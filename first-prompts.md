# Erste Prompts für Claude Code

Konkrete Prompts, die du Claude Code für die ersten Phasen geben kannst. Passe sie an deine Situation an.

## Voraussetzung: Stitch-MCP einrichten

Bevor du die UI-Phasen (4+) startest, solltest du den Stitch-MCP-Server in Claude Code konfigurieren. Dann können die Prompts Designs direkt aus Stitch ziehen, statt auf Datei-Exporte zurückzugreifen. Setup-Schritte in kurz:

1. Google Cloud Project mit aktivierter Stitch-API
2. `gcloud auth application-default login`
3. MCP-Server in `~/.claude/mcp.json` oder `.mcp.json` im Projekt eintragen (siehe Stitch-MCP-Docs)
4. Claude Code neu starten, MCP-Tools sollten im Tool-Inventar erscheinen

Die Backend-Phasen (1–3) gehen auch ohne, du kannst den MCP-Setup also verschieben bis vor Phase 4.

## Phase 1: Backend-Fundament

**Prompt 1: Solution scaffolden**

```
Lies die CLAUDE.md und die Dateien in docs/. Scaffolde die Solution
gemäß docs/project-structure.md:

1. Lege die .sln und die beiden .csproj-Dateien an (Api + Tests)
2. Erstelle die Ordnerstruktur mit allen geplanten Features-Ordnern
3. Erstelle das leere Program.cs mit der Minimal-API-Basis
4. Erstelle Common/Endpoints/IEndpoint.cs und EndpointExtensions.cs
5. Erstelle docker-compose.yml für lokalen SQL Server
6. Füge alle in docs/project-structure.md genannten NuGet-Pakete hinzu

Verifiziere mit `dotnet build`, dass alles kompiliert.
```

**Prompt 2: Datenmodell**

```
Implementiere das Datenmodell gemäß docs/data-model.md:

1. Alle Entity-Klassen in Domain/Entities/
2. ReservationStatus-Enum in Domain/Enums/
3. AppDbContext in Infrastructure/Persistence/
4. IEntityTypeConfiguration<T>-Klassen pro Entity in
   Infrastructure/Persistence/Configurations/ — inkl. Filtered Unique
   Index, RowVersion, alle Indexes aus der Spec
5. AppDbContextFactory für dotnet ef
6. SeedData in Infrastructure/Persistence/Seed/ mit:
   - Admin-User aus appsettings
   - aktuelle Saison
   - 3 Courts (konfigurierbar)
   - SystemSettings mit Defaults

Dann erste Migration erstellen und gegen lokalen SQL (docker compose)
ausführen. Verifizieren dass alle Tables und Indexes korrekt angelegt
wurden.
```

## Phase 2: Auth end-to-end

**Prompt 3: Auth Backend**

```
Implementiere den kompletten Auth-Flow gemäß docs/auth-flow.md:

1. JwtSettings-Klasse + Konfiguration
2. JwtTokenService in Infrastructure/Auth/
3. Alle Auth-Features als Vertical Slices in Features/Auth/:
   - Login, Refresh, Logout, ForgotPassword, ResetPassword
   - Jeweils Endpoint + Handler + Request + Validator
4. Program.cs um Identity, JWT, Authorization erweitern
5. Unit-Tests für Login- und RefreshHandler (happy path + typische Fehler)

Verifiziere mit manuellem Test via curl oder Bruno, dass Login → Refresh
→ geschützter Endpoint funktioniert.
```

## Phase 3: Kern-Feature Buchungen

**Prompt 4: Buchungsregeln**

```
Implementiere die Rule Engine gemäß docs/booking-rules.md:

1. IBookingRule, BookingAttempt, RuleResult in Features/Reservations/Rules/
2. BookingRuleEngine-Orchestrator
3. Alle 9 Rule-Klassen (siehe docs/booking-rules.md)
4. DbUpdateExceptionExtensions.IsUniqueConstraintViolation()
5. Unit-Test pro Rule mit FakeTimeProvider

Extension-Methode AddBookingRules() für die Registrierung aller Rules
in Program.cs.
```

**Prompt 5: CreateReservation-Slice**

```
Implementiere die CreateReservation-Slice in Features/Reservations/Create/:
- Request, Validator, Handler, Endpoint
- Handler ruft Rule Engine auf und behandelt UniqueConstraintViolation
  (siehe docs/booking-rules.md)
- Integration-Test: zwei parallele Create-Requests auf denselben Slot —
  einer erfolgreich, einer mit Conflict
```

**Prompt 6: Restliche Reservation-Slices**

```
Implementiere:
- CancelReservation mit RowVersion-Check und MinCancellationHours-Regel
- ListForWeek (Query für das Wochengrid)
- ListMine (eigene Buchungen mit Status-Filter)

Inklusive Tests.
```

## Phase 4: Angular-Shell

**Prompt 7: Angular scaffolden**

```
Erstelle das Angular-Projekt gemäß docs/angular-structure.md:

1. `ng new frontend --standalone --routing --style scss`
2. Angular Material und date-fns hinzufügen
3. Core/, Features/, Shared/ Ordner anlegen
4. app.config.ts mit allen Providers (inkl. de-AT Locale)
5. Top-Level-Routing mit Lazy Loading für alle Features
6. environment.ts / environment.prod.ts mit apiUrl
7. AuthService, AuthInterceptor, AuthGuards, AdminGuard in Core/Auth/
8. Result- und CurrentUser-Modelle in Core/Models/

Verifiziere mit `ng build` dass alles kompiliert.
```

**Prompt 8: Login-Screen**

```
Hole das Login-Screen-Design aus Stitch via MCP und implementiere den
Screen als Angular Standalone Component in
frontend/src/app/features/auth/login/.

- Reactive Form (Email + Passwort)
- Verwendung von Angular Material (MatFormField, MatInput, MatButton)
- Integration mit AuthService
- Fehlerbehandlung gemäß docs/screen-briefings.md Screen 1
- OnPush Change Detection
- Halte dich an DESIGN.md (Color Tokens, Typography)

Nach Erfolg: Redirect auf /reservations
```

## Phase 5: Hero-Feature Wochengrid

**Prompt 9: Reservations Service + API Layer**

```
Implementiere in frontend/src/app/features/reservations/:
- reservation.model.ts (Interfaces für Reservation, CreateReservationRequest)
- reservations.api.ts (HttpClient-Wrapper)
- reservations.service.ts (State via Signals, siehe docs/angular-structure.md)
- reservations.routes.ts mit Lazy-Loading
```

**Prompt 10: Wochengrid**

```
Hole das Week-Grid-Design aus Stitch via MCP. Kombiniere es mit
docs/screen-briefings.md Screen 4 für die inhaltlichen Anforderungen.

Implementiere die WeekGridComponent:
- Tag-Tabs mit Swipe (Mobile) / Wochen-Navigation
- Grid: Plätze × Zeit-Slots
- Vier Zellzustände farblich + per Muster kodiert
- Loading-State mit Skeleton (wegen Azure SQL Auto-Pause)
- Leerzustand für Saison-aus
- Effect lädt Daten bei Änderung der ausgewählten Woche
- Design-Tokens aus DESIGN.md verwenden

Nach Tap auf freie Zelle: BookingDialog öffnen.
```

**Prompt 11: BookingDialog**

```
Hole das Booking-Dialog-Design aus Stitch via MCP und kombiniere es mit
docs/screen-briefings.md Screen 5.

Implementiere den BookingDialog als MatDialog:
- Reactive Form
- Gastspieler-Toggle mit Autocomplete (aus reservations.api getGuests)
- Option "Neuen Gast anlegen" inkl. Sub-Formular
- Gastspielergebühr-Hinweis bei aktivem Gast
- Loading-State während Submit
- Inline-Fehlermeldung bei Conflict-Response
- Design-Tokens aus DESIGN.md
```

## Weitere Phasen analog

Pattern: pro Feature je ein Prompt. Immer mit Verweis auf die relevanten
Docs, immer mit expliziter Verifikations-Anforderung am Ende.

## Nützliche Meta-Prompts

```
Review deinen zuletzt geschriebenen Code gegen die Konventionen in
CLAUDE.md. Was würdest du einem erfahrenen Kollegen bei der Code-Review
kritisieren?
```

```
Schreibe Tests für alle public Methoden in <Datei>, inklusive
Edge-Cases. Nutze FakeTimeProvider für Zeitlogik.
```

```
Führe alle Tests aus. Falls welche rot sind, fixe sie, ohne die
Test-Logik zu ändern. Falls die Tests den Code-Contract falsch
widerspiegeln, erkläre den Konflikt bevor du was änderst.
```

```
# Benutze von jetzt an konsequent async/await in Streaming-Handlern,
nie .Result oder .Wait(). Das in CLAUDE.md eintragen wenn es sich
bewährt.
```

## Nicht vergessen nach jeder Phase

- [ ] `dotnet test` (Backend) und `ng test` (Frontend) grün
- [ ] `dotnet build --configuration Release` ohne Warnings
- [ ] `ng lint` clean
- [ ] git commit mit Conventional-Commit-Message
- [ ] bei größeren Änderungen: CLAUDE.md auf Aktualität prüfen
