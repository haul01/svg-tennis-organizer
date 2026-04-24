# Architektur & MVP-Scope

## Projekt-Kontext

- **Verein:** 50–200 Mitglieder, Tennisverein in Österreich
- **Anlage:** 3–5 Outdoor-Plätze, Saisonbetrieb (kein Winter)
- **Ziel:** Solide Webapplikation zum Reservieren des Tennisplatzes
- **Nicht-Ziel:** Mehrere Vereine (Multi-Tenant), keine Produktabsichten

## Nutzer-Rollen

- **Member** — reguläres Mitglied, kann buchen und eigene Buchungen verwalten
- **Trainer** — im MVP funktional identisch zu Member (kein eigenes UI)
- **Admin** — Vollzugriff auf Mitgliederverwaltung, Platzsperren, Saison, Regeln, Gastspieler-Abrechnung

## MVP-Scope (Version 1)

**Inklusive:**
- Login + Passwort-Reset
- Platzübersicht als Wochengrid
- Einzel-Slot buchen
- Eigene Buchungen ansehen und stornieren
- Gastspieler beim Buchen eintragen (strukturierte Daten, kein Login für Gäste)
- Konfigurierbare Buchungsregeln (MaxAdvance, MinCancel, MaxOpen)
- Admin: Mitglieder-CRUD
- Admin: Plätze sperren (einmalig oder wöchentlich wiederkehrend — Trainer-Workaround)
- Admin: Saisonverwaltung (Start, Ende, Öffnungszeiten)
- Admin: Gastspieler-Nutzungsliste für Offline-Abrechnung
- E-Mail-Bestätigung bei Buchung und Stornierung

**V2 (später):**
- Erinnerungs-Mails 24h vorher
- Partnersuche („Ich suche Mitspieler für Sa 10 Uhr")
- Statistiken / Reports

**V3+ (optional):**
- Dauerbuchungen (echte, mit Mitspieler-Eintrag)
- Doppel-Buchungen
- Warteliste mit Benachrichtigung
- Wetter-Anzeige
- Gastspieler zu vollen Accounts aufwerten

**Dauerhaft draußen:**
- Online-Zahlungen (Gastspielergebühren werden offline im Vereinsheim abgewickelt)
- Import aus bestehender Mitgliederverwaltung (wird manuell gepflegt)
- Indoor/Halle (nur Outdoor-Betrieb)

## Technische Grundsatzentscheidungen

### Backend

- **ASP.NET Core 10** mit Minimal APIs (Endpoint-Pattern statt Controller)
- **Vertical Slice Architecture** in einem Projekt, nicht 4-Layer Clean Architecture
- **EF Core 10** mit Code-First-Migrations
- **Azure SQL Database** statt PostgreSQL — PostgreSQL ist auf Azure nur 12 Monate gratis, Azure SQL dauerhaft
- **ASP.NET Identity** mit JWT + rotierenden Refresh Tokens für Auth
- **Result-Pattern** für Business-Fehler, Exceptions nur für Unerwartetes
- **FluentValidation** pro Feature-Slice
- **Serilog** für strukturiertes Logging
- **Scriban** für E-Mail-Templates (leichtgewichtig)

### Frontend

- **Angular (aktuelle Version)** mit Standalone Components
- **Signals** für UI-State statt BehaviorSubject/NgRx
- **Angular Material** für UI-Komponenten
- **Reactive Forms** für komplexe Formulare
- **Functional Interceptors & Guards** statt class-based
- **OnPush Change Detection** (automatisch kompatibel mit Signals)
- **date-fns** für Datums-Operationen (kompakter als Moment/Luxon)

### Infrastruktur

- **Azure Container Apps** mit Consumption Plan (Scale-to-Zero) für die API
- **Azure Static Web Apps** (Free Tier) für Angular
- **Azure SQL Free Offer** (100k vCore-Sekunden/Monat, 32 GB, Auto-pause)
- **GitHub Container Registry** (ghcr.io) statt Azure Container Registry (spart ~5€/Monat)
- **GitHub Actions** mit **OIDC Federated Identity** (keine Service-Principal-Secrets)
- **Brevo** als SMTP-Provider (300 Mails/Tag dauerhaft gratis, EU-basiert, GDPR-konform)

### Concurrency-Strategie

Drei-Schichten-Verteidigung für Doppelbuchungs-Verhinderung:

1. Application-Level-Validation via Rule Engine (UX-freundliche Fehlermeldungen)
2. Filtered Unique Index in der DB (physische Verhinderung)
3. `RowVersion` (optimistisches Locking, primär für Updates/Stornos)

Details: @docs/booking-rules.md

## Nicht-funktionale Anforderungen

- **Locale:** `de-AT`, Datumsformat „Mo, 15.04.2026 18:00"
- **Mobile-First** für Mitglieder-Screens, Desktop-optimiert für Admin
- **DSGVO-konform:** keine Namen anderer Mitglieder in der UI, generische Auth-Fehler, Recht auf Löschung via Admin
- **Saisonbetrieb:** System kennt Zustand „Saison offen/geschlossen"
- **Cold-Start-Verträglichkeit:** Azure SQL pausiert bei Inaktivität, erster Request kann 10–30 Sekunden dauern → UX muss das kommunizieren

## Ein-/Aus-Kriterien (wann ist der MVP fertig?)

Das MVP ist fertig, wenn:
- Ein Admin Mitglieder anlegen kann und diese per E-Mail ihr Passwort setzen
- Ein Mitglied sich einloggen, ein Wochengrid sehen und einen Slot buchen kann
- Buchungsregeln konfigurierbar und wirksam sind
- Doppelbuchungen technisch unmöglich sind
- Admin Platzsperren (einmalig + wöchentlich) anlegen kann
- Gastspieler-Nutzungen für Offline-Abrechnung exportierbar sind
- Bestätigungs-E-Mails zuverlässig ankommen
- Die App in Azure deployed ist und über HTTPS unter einer Custom Domain erreichbar ist
