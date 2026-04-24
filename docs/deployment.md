# Deployment

CI/CD über GitHub Actions. Frontend → Static Web Apps (auto-konfiguriert), Backend → Container Apps (manuelle Pipeline).

## Azure-Ressourcen (einmalig anlegen)

Alles in einer Resource Group `tennisclub-rg`:

1. **Azure SQL Database** — Free Offer aktivieren, Auto-pause einschalten (32 GB, 100k vCore-Sekunden/Monat)
2. **Container Apps Environment** + **Container App** für die API (Consumption Plan, Scale-to-Zero)
3. **Static Web App** — beim Erstellen GitHub-Repo verbinden (legt Frontend-Pipeline automatisch an)
4. **Keine Azure Container Registry** — wir nutzen GitHub Container Registry (`ghcr.io`), kostenlos

## Frontend-Pipeline (von Azure generiert)

Beim Erstellen der Static Web App im Portal → GitHub-Repo verbinden → Azure:

- Legt `.github/workflows/azure-static-web-apps-<random>.yml` automatisch an
- Speichert Deployment-Token als GitHub-Secret
- Pushes auf `main` → Produktions-Deploy
- Pull Requests → Preview-Environment mit eigener URL

Typische Anpassungen in der generierten YAML:

```yaml
- name: Build And Deploy
  uses: Azure/static-web-apps-deploy@v1
  with:
    azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
    repo_token: ${{ secrets.GITHUB_TOKEN }}
    action: "upload"
    app_location: "/frontend"
    api_location: ""
    output_location: "dist/tennisclub-frontend/browser"
```

**Wichtig:** Die API-URL wird in `environment.prod.ts` hardgecoded (nicht als Secret), weil sie im Browser sowieso sichtbar ist.

## Backend-Pipeline (manuell aufsetzen)

### Schritt 1: OIDC-Federated-Identity einrichten

Moderne Authentifizierung von GitHub Actions zu Azure. Keine Secrets, keine Rotation.

```bash
# 1. App Registration anlegen
az ad app create --display-name "github-tennisclub-deploy"
# → notiere die appId aus der Response

# 2. Service Principal erstellen
az ad sp create --id <appId>

# 3. Contributor-Rolle zuweisen (Scope: Resource Group)
az role assignment create \
  --role "Contributor" \
  --subscription <subscription-id> \
  --assignee-object-id <sp-object-id> \
  --assignee-principal-type ServicePrincipal \
  --scope "/subscriptions/<subscription-id>/resourceGroups/tennisclub-rg"

# 4. Federated Credential für den main-Branch
az ad app federated-credential create \
  --id <appId> \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:<YourGitHubUser>/<Repo>:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### Schritt 2: GitHub Secrets anlegen

Repo → Settings → Secrets and variables → Actions:

- `AZURE_CLIENT_ID` — die `appId` aus Schritt 1
- `AZURE_TENANT_ID` — deine Azure-Tenant-ID
- `AZURE_SUBSCRIPTION_ID` — Azure Subscription-ID
- `AZURE_SQL_CONNECTION_STRING` — für Migration (mit einem dedizierten migrate-User, DDL-Rechte)

### Schritt 3: Dockerfile

`src/TennisClub.Api/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TennisClub.Api.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TennisClub.Api.dll"]
```

### Schritt 4: Workflow

`.github/workflows/backend.yml`:

```yaml
name: Deploy API

on:
  push:
    branches: [main]
    paths:
      - 'src/TennisClub.Api/**'
      - '.github/workflows/backend.yml'
  workflow_dispatch:

permissions:
  id-token: write    # für OIDC-Login
  contents: read
  packages: write    # für ghcr.io push

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}/tennisclub-api
  RESOURCE_GROUP: tennisclub-rg
  CONTAINER_APP_NAME: tennisclub-api

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      - run: dotnet restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release

  migrate:
    needs: test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.x'
      - name: Generate idempotent migration script
        run: |
          dotnet tool install --global dotnet-ef
          dotnet ef migrations script --idempotent \
            --project src/TennisClub.Api \
            --output migrate.sql
      - uses: azure/sql-action@v2
        with:
          connection-string: ${{ secrets.AZURE_SQL_CONNECTION_STRING }}
          path: migrate.sql

  deploy:
    needs: migrate
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v5
        with:
          context: src/TennisClub.Api
          push: true
          tags: |
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
            ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest
      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
      - name: Update Container App image
        run: |
          az containerapp update \
            --name ${{ env.CONTAINER_APP_NAME }} \
            --resource-group ${{ env.RESOURCE_GROUP }} \
            --image ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
```

Drei Jobs: `test` → `migrate` → `deploy`. Tests rot → nichts deployed. Migration failed → Code nicht ausgerollt (wichtig, weil neuer Code auf altem Schema crasht).

## Migration-Strategie: Pipeline statt Startup

**Nicht** `db.Database.MigrateAsync()` im Program.cs. Bei mehreren Replicas wäre das eine Race. Stattdessen:

```bash
dotnet ef migrations script --idempotent --output migrate.sql
# → Azure SQL Action applied das Script
```

`--idempotent` sorgt dafür, dass bereits angewandte Migrationen übersprungen werden. Das Script kann beliebig oft ausgeführt werden.

**Migrate-User:** Separater DB-User mit DDL-Rechten, nur für Pipeline. Der API-User (Runtime) hat nur DML. Principle of Least Privilege.

## Container App Konfiguration

Runtime-Config (DB-Connection, SMTP, JWT) gehören **nicht** in `appsettings.json`, sondern als Environment Variables in die Container App.

Sensible Werte als **Secrets**:

```bash
# Secrets anlegen
az containerapp secret set \
  --name tennisclub-api \
  --resource-group tennisclub-rg \
  --secrets \
    "db-connection=Server=...;Database=...;User Id=...;Password=..." \
    "jwt-key=<min. 32 Zeichen random>" \
    "smtp-password=<brevo-smtp-key>"

# Env-Variables referenzieren die Secrets
az containerapp update \
  --name tennisclub-api \
  --resource-group tennisclub-rg \
  --set-env-vars \
    "ConnectionStrings__Default=secretref:db-connection" \
    "Jwt__SigningKey=secretref:jwt-key" \
    "Smtp__Password=secretref:smtp-password" \
    "Smtp__Host=smtp-relay.brevo.com" \
    "Smtp__Port=587" \
    "Smtp__Username=<brevo-smtp-user>" \
    "Smtp__FromName=TennisClub" \
    "Smtp__FromAddress=reservierung@tennisverein.at"
```

## Branch Protection

GitHub → Settings → Branches → Add rule für `main`:

- [x] Require a pull request before merging
- [x] Require status checks to pass before merging (choose `test` job)
- [x] Require branches to be up to date before merging

Schützt dich davor, kaputte Migrationen direkt nach `main` zu pushen.

## Lokale Entwicklung

**Docker Compose für lokalen SQL Server:**

```yaml
# docker-compose.yml im Repo-Root
services:
  sql:
    image: mcr.microsoft.com/azure-sql-edge:latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=DevPassword123!
    ports:
      - "1433:1433"
```

Dev-Connection-String in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost,1433;Database=TennisClub;User Id=sa;Password=DevPassword123!;TrustServerCertificate=true;"
  }
}
```

## Deploy-Ablauf im Alltag

1. Feature in einem Branch entwickeln
2. Lokal testen (`dotnet test`, manuell verifizieren)
3. Pull Request öffnen — `test`-Job läuft
4. PR merged → `backend.yml` + `frontend.yml` triggern
5. `test` → `migrate` → `deploy` (~3–5 Minuten)
6. Neue Version ist online
7. Frontend Preview-URL ist bei PRs automatisch verfügbar

## Monitoring (für später)

- **Application Insights** an Container App anbinden (Telemetry, Exceptions, Performance)
- **Log Analytics-Kostenfalle beachten:** 5 GB/Monat gratis, darüber ~2€/GB. Sampling auf 10–20% stellen
- **Budget-Alert** unter Cost Management → Budgets bei 1€ Ausgaben einrichten (Mail bei Überschreitung)

## Rollback

Wenn ein Deploy schief geht:

```bash
# Letzten funktionierenden SHA finden
git log --oneline

# Container App auf alten Image-Tag zurücksetzen
az containerapp update \
  --name tennisclub-api \
  --resource-group tennisclub-rg \
  --image ghcr.io/<repo>/tennisclub-api:<old-sha>
```

Bei DB-Migrations die nicht rückwärts-kompatibel sind: vor solchen Migrations ein Backup in Azure SQL anlegen (Point-in-Time Restore ist bei Free-Tier 7 Tage verfügbar).
