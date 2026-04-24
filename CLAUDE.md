# TennisClub — Projekt-Kontext

## Was ist das?

Reservierungssystem für einen Tennisverein in Österreich. 50–200 Mitglieder, 3–5 Outdoor-Plätze, Saisonbetrieb. Single-Tenant, solide implementiert. MVP-Scope und vollständiger Hintergrund: siehe @docs/architecture.md.

## Stack

- **Backend:** ASP.NET Core 10, EF Core, Azure SQL (Free Tier)
- **Frontend:** Angular (aktuelle Version), Angular Material, Signals, Standalone Components
- **Hosting:** Azure Container Apps (API) + Static Web Apps (Angular) — alles im Azure-Free-Tier
- **Auth:** ASP.NET Identity + JWT + rotierende Refresh Tokens
- **Email:** Brevo via SMTP
- **CI/CD:** GitHub Actions mit OIDC-Federated-Identity

## Code-Organisation

- **Backend:** Vertical Slice Architecture, ein Projekt (`src/TennisClub.Api/`), Features in `Features/<Domain>/<Action>/` — Details: @docs/project-structure.md
- **Frontend:** Feature-Folders in `frontend/src/app/features/`, plus `core/`, `shared/` — Details: @docs/angular-structure.md

## Konventionen — nicht verhandelbar

- Zeitwerte: immer `DateTimeOffset`, nie `DateTime`
- Reservation-Concurrency: `RowVersion` + Filtered Unique Index auf `(CourtId, StartsAt)` mit Filter `Status = Active`
- Soft-Cancel für Reservierungen (Status-Flag, kein DELETE)
- Result-Pattern statt Exceptions für Business-Fehler
- Keine MediatR, kein NgRx, kein AutoMapper — bewusste Entscheidung, nicht aus Unwissen
- Minimal APIs mit `IEndpoint`-Pattern statt Controller
- `TimeProvider` injecten, nie `DateTimeOffset.UtcNow` direkt verwenden
- Im Frontend: Standalone Components, Signals für UI-State, Reactive Forms für Formulare, OnPush-Change-Detection
- State-Mutation nur über Service-Methoden; öffentliche Signals sind `asReadonly()`

## DSGVO & Security

- UI zeigt NIE Namen anderer Mitglieder (nur „Belegt")
- Auth-Fehlermeldungen generisch (Enumeration-Schutz)
- Refresh Tokens werden gehasht (SHA-256) gespeichert, nie im Klartext
- Secrets niemals in appsettings.json, sondern als Container-App-Secrets / GitHub-Secrets

## Sprache & Stil

- Code-Kommentare auf Englisch
- UI-Texte und Error-Messages auf Deutsch (de-AT), Datumsformat „Mo, 15.04.2026 18:00"
- Commit-Messages: Conventional Commits (`feat:`, `fix:`, `refactor:` etc.)
- Fehlermeldungen handlungsorientiert, nicht technisch

## Verifikation

Nach jedem Feature vor dem Commit ausführen:

- Backend-Tests: `dotnet test` (im Repo-Root)
- Frontend-Tests: `cd frontend && npm run test -- --watch=false`
- Frontend-Lint: `cd frontend && npm run lint`
- Backend-Build: `dotnet build --configuration Release`

## Phasen-Plan

Vorgehen in Reihenfolge, jede Phase vollständig abschließen und commiten bevor die nächste beginnt:

1. **Scaffold & Datenmodell** — Solution-Layout + Entities + erste Migration — siehe @docs/project-structure.md und @docs/data-model.md
2. **Auth end-to-end** — Login, Refresh, Passwort-Reset — siehe @docs/auth-flow.md
3. **Buchungs-Kern** — CreateReservation + CancelReservation + ListWeek mit allen Regeln — siehe @docs/booking-rules.md
4. **Angular-Shell** — Projekt-Setup, Routing, Auth-Integration, Login-Screen
5. **Wochengrid + Buchungs-Dialog** — das Hero-Feature
6. **Meine Buchungen + Profil** — restliche Mitglieder-Features
7. **Admin-Bereich** — Mitglieder, Platzsperren, Saison, Regeln, Gastspieler
8. **E-Mail-Integration** — siehe @docs/email.md
9. **Deployment** — GitHub Actions aufsetzen — siehe @docs/deployment.md

## Arbeitsweise mit Claude Code

- Bei größeren Features zuerst Plan erstellen (Plan Mode), Plan gemeinsam reviewen, dann umsetzen
- Nach jedem Feature Tests schreiben und ausführen
- Unsicherheit bei Entscheidungen → fragen, nicht raten

## Designs

Designs werden in **Google Stitch** gepflegt und sind über den Stitch-MCP-Server direkt abrufbar — **nicht** per Datei-Export.

- **Design-System:** `@DESIGN.md` im Repo-Root (von Stitch generiert). Enthält Color Tokens, Typography, Spacing, Komponentenregeln. Wird bei jeder Session automatisch mitgeladen und ist verbindlich für alle UI-Arbeiten.
- **Screen-Inhalte:** `@docs/screen-briefings.md` — was inhaltlich auf jedem Screen sein muss (Felder, Zustände, Fehlerbehandlung). Ergänzt den visuellen Design-Part.
- **Screen-Designs abrufen:** via Stitch-MCP-Tools. Beispiel-Prompt: „Hole das Login-Screen-Design aus Stitch und generiere daraus die Angular-Component in `frontend/src/app/features/auth/login/`. Halte dich an `@DESIGN.md` und `@docs/angular-structure.md`."
- **Konsistenz zwischen Screens:** Wenn ein neuer Screen stilistisch zu existierenden passen soll, zuerst das „Design Context" / die „Design DNA" eines Referenz-Screens extrahieren, dann daraus den neuen generieren. Die Stitch-MCP-Tools bieten genau dafür explizite Actions.
- **Fallback:** Falls der MCP-Server mal nicht verfügbar ist, können Screenshots manuell in `design/screenshots/` abgelegt werden und Claude Code diese lesen.

## Detail-Dokumente

- Architektur & MVP-Scope: @docs/architecture.md
- Datenmodell: @docs/data-model.md
- Auth-Flow: @docs/auth-flow.md
- Buchungsregeln & Concurrency: @docs/booking-rules.md
- Backend-Projektstruktur: @docs/project-structure.md
- Angular-Projektstruktur: @docs/angular-structure.md
- E-Mail-Versand: @docs/email.md
- Deployment: @docs/deployment.md
- Screen-Inhalte: @docs/screen-briefings.md
- Design-System (von Stitch gepflegt): @DESIGN.md
