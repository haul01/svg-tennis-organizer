# TennisClub

Reservierungssystem für einen Tennisverein in Österreich. ASP.NET Core API + Angular-Frontend, gehostet auf Azure Free Tier.

## Schnellstart

1. **Voraussetzungen**
   - .NET 10 SDK
   - Node.js (aktuell LTS)
   - Docker Desktop (für lokalen SQL Server)
   - Claude Code: `npm install -g @anthropic-ai/claude-code`

2. **Repo klonen und lokal starten**
   ```bash
   git clone <repo-url>
   cd tennisclub

   # Lokalen SQL Server starten
   docker compose up -d

   # Backend
   cd src/TennisClub.Api
   dotnet ef database update
   dotnet run

   # Frontend (zweites Terminal)
   cd frontend
   npm install
   ng serve
   ```

3. **Mit Claude Code weiterentwickeln**
   ```bash
   claude
   ```
   Die `CLAUDE.md` im Repo-Root wird automatisch geladen und enthält alle Projektkonventionen.

## Dokumentation

Alle Design-Entscheidungen und Implementierungs-Guides in `docs/`:

- `architecture.md` — MVP-Scope, Stack, Grundsatzentscheidungen
- `data-model.md` — Entities, Relationen, EF-Konfiguration
- `auth-flow.md` — Login, JWT, Refresh-Token-Rotation
- `booking-rules.md` — die 9 Buchungsregeln und Concurrency-Strategie
- `project-structure.md` — Backend Solution Layout (Vertical Slice)
- `angular-structure.md` — Frontend-Layout mit Signals und Standalone Components
- `email.md` — E-Mail-Versand via Brevo
- `deployment.md` — GitHub Actions + Azure Container Apps
- `screen-briefings.md` — Screen-Spezifikationen für UI-Generierung

## Stack

- **Backend:** ASP.NET Core 10, EF Core 10, Azure SQL
- **Frontend:** Angular 19+, Angular Material, Signals, Standalone Components
- **Auth:** ASP.NET Identity + JWT mit rotierenden Refresh Tokens
- **Hosting:** Azure Container Apps + Static Web Apps (Free Tier)
- **Email:** Brevo SMTP
- **CI/CD:** GitHub Actions mit OIDC Federated Identity

## Entwicklung

```bash
# Backend
dotnet build
dotnet test
dotnet run --project src/TennisClub.Api

# Frontend
cd frontend
npm run test -- --watch=false
npm run lint
ng build --configuration production
```

## Projektstatus

MVP in Entwicklung. Phasen-Plan: siehe `CLAUDE.md`.

## Lizenz

[TBD]
