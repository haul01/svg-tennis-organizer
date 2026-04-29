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
