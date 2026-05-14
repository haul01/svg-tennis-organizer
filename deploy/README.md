# Pi-Deployment

Self-hosted Setup auf einem Raspberry Pi (4B, 8 GB RAM, SSD). Stack:

- `postgres:16-alpine` — Daten + Identity-Tabellen
- `tennisclub-api` — ASP.NET Core 10, Migrations + Seed laufen beim Start
- `caddy:2-alpine` — Reverse Proxy + statisches Angular-Bundle, hört auf `127.0.0.1:80`

TLS macht der Cloudflare Tunnel davor (oder ein anderer Proxy deiner Wahl) — Caddy selbst hat hier kein TLS.

## Erst-Setup auf der Pi

Voraussetzungen:
- Docker + Docker Compose v2 (`curl -fsSL https://get.docker.com | sh`)
- Node.js 22+ und npm (`sudo apt install nodejs npm` oder via nvm)
- Git

```bash
sudo mkdir -p /opt/tennisclub
sudo chown $USER /opt/tennisclub
git clone https://github.com/haul01/svg-tennis-organizer.git /opt/tennisclub
cd /opt/tennisclub/deploy

cp .env.example .env
$EDITOR .env   # alle change_me-Werte ersetzen

./update.sh
```

`update.sh` zieht git → baut Frontend → baut API-Image → bringt den Stack hoch. Erste Migration + Seed laufen automatisch beim API-Container-Start.

## Tägliche Updates

```bash
ssh pi@<host> 'cd /opt/tennisclub/deploy && ./update.sh'
```

Downtime ~10–20 s während der API-Container neu startet. Migrations werden idempotent angewandt; Seed-Helfer schreiben nichts wenn ihre Zeilen schon existieren.

## API-URL für das Frontend setzen

`config.js` liegt nach dem Build in `frontend/dist/frontend/browser/config.js` und wird von Caddy unhashed ausgeliefert. Default ist `apiUrl: 'http://localhost:5555/api'` für lokale Entwicklung — auf der Pi läuft alles unter einer Origin, deshalb reicht `'/api'`:

```bash
cat > /opt/tennisclub/frontend/dist/frontend/browser/config.js <<'EOF'
window.TC_CONFIG = { apiUrl: '/api' };
EOF
```

Browser einmal hart neu laden (Strg+Shift+R), kein Container-Restart nötig.

## Mail-Versand (Brevo)

Solange `SMTP_HOST` in `deploy/.env` leer ist, schreibt die API jeden
Mail-Versand nur in den Container-Log (`LoggingEmailSender`). Für echten
Versand brauchst du einen SMTP-Relay — wir empfehlen Brevo (300
Mails/Tag gratis, EU-gehostet, GDPR-konform).

### 1. Brevo-Account + Domain verifizieren

1. Account anlegen auf https://www.brevo.com (kostenlos)
2. **Senders, Domains & Dedicated IPs → Domains → Add a domain**: deine
   Vereinsdomain eintragen (`tennisverein.at`)
3. Brevo zeigt drei DNS-Einträge:
   - `brevo-code._domainkey.<domain>` (DKIM, CNAME)
   - SPF-TXT — `v=spf1 include:spf.brevo.com ~all`
   - DMARC-TXT — `v=DMARC1; p=none; rua=mailto:dmarc@<domain>` (Start)
4. DNS-Einträge bei deinem Provider setzen, in Brevo auf **Verify**
   warten (kann 5 min bis 1 h dauern)
5. **Settings → SMTP & API → SMTP**: SMTP-Login + Master-Key generieren.
   Beides notieren — wir tragen es gleich in `.env` ein.

### 2. `.env` auf der Pi befüllen

```ini
SMTP_HOST=smtp-relay.brevo.com
SMTP_PORT=587
SMTP_USERNAME=<brevo-smtp-login>
SMTP_PASSWORD=<brevo-master-key>
SMTP_FROM_NAME=TennisClub
SMTP_FROM_ADDRESS=reservierung@tennisverein.at
```

Die `FROM_ADDRESS` muss zur in Brevo verifizierten Domain gehören —
sonst lehnt der Relay die Mail ab (`550 5.7.1`).

### 3. Stack neu starten

```bash
ssh pi@<host> 'cd /opt/tennisclub/deploy && ./update.sh'
```

Die API erkennt `SMTP_HOST` und schaltet automatisch von
`LoggingEmailSender` auf `SmtpEmailSender` um.

### 4. Smoke-Test

Admin-Token besorgen (aus Browser nach Login: `localStorage.getItem('accessToken')`)
und dann:

```bash
curl -X POST https://tennis.deinedomain.at/api/admin/diag/test-email \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"to": "selbsttest@dein-postfach.at"}'
```

- `204 No Content` → Versand hat funktioniert, Inbox prüfen (auch Spam!)
- `400` mit `SMTP-Versand fehlgeschlagen: ...` → SMTP-Fehler steht im
  Response-Body. Häufige Ursachen: falsches Passwort, FROM-Adresse
  nicht in Brevo verifiziert, Port 587 von der Pi-IP nicht erreichbar.

Erstmail an verschiedene Provider testen (Gmail, GMX, Outlook,
selbst-gehostet), Spam-Ordner checken, Deliverability bewerten.

## Cloudflare Tunnel (TLS + Public Access)

Tunnel-Daemon installieren und mit dem Token aus dem Cloudflare-Zero-Trust-Dashboard registrieren:

```bash
sudo apt install cloudflared
sudo cloudflared service install <token-aus-cf-dashboard>
```

Im Cloudflare Zero Trust Dashboard → Networks → Tunnels → deinen Tunnel → "Public Hostname" anlegen:
- Hostname: `tennis.deinedomain.at`
- Service: `http://127.0.0.1:80`

Quick-Test ohne eigene Domain:
```bash
cloudflared tunnel --url http://localhost:80
```
Liefert eine `*.trycloudflare.com`-URL die bis zum Beenden gilt.

## Backups

Nicht in `update.sh` enthalten — eigenes Skript unter `deploy/backup.sh` (Phase 6) oder per Cron:

```bash
docker exec tennisclub-postgres pg_dump -U tennis tennisclub | gzip > /opt/tennisclub/data/backup-$(date +%F).sql.gz
```

Volumes auf der Pi:
- `deploy/data/postgres/` — DB-Daten
- `deploy/data/dataprotection-keys/` — Reset-Token-Keys (verlierst du das, sind alle ausstehenden Reset-Links ungültig)
- `deploy/data/caddy*/` — Caddy-Cache

## Rollback

```bash
git -C /opt/tennisclub log --oneline | head
git -C /opt/tennisclub checkout <vorheriger-sha>
cd /opt/tennisclub/deploy && ./update.sh
```

DB-Migrations sind nicht automatisch rückwärtskompatibel — vor riskanten Migrations vorher `pg_dump` machen.
