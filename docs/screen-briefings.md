# Tennis-Reservierungssystem — Screen-Briefings

Stand: April 2026. Dokument dient als Eingabematerial für UI-Generierung (z.B. Google Stitch). Jedes Screen-Briefing ist so formuliert, dass es eigenständig in einen Prompt kopiert werden kann.

## Leitgedanke

**Mobile-First für Mitglieder-Screens** (ab 380px Breite), Desktop-optimiert für den Admin-Bereich.

Die App hat zwei Nutzergruppen mit unterschiedlichen Kontexten:

- **Mitglieder** checken Verfügbarkeiten am Handy, oft unterwegs oder am Platz
- **Admins** arbeiten hauptsächlich am Desktop mit strukturellen Aufgaben

## Rollen

- **Mitglied** — kann buchen, eigene Buchungen sehen und stornieren
- **Trainer** — identisch zu Mitglied (im MVP kein eigenes UI)
- **Admin** — Vollzugriff auf Verwaltung

---

# User Stories

## Als Mitglied möchte ich…
- mich mit E-Mail und Passwort einloggen
- beim ersten Login mein Passwort setzen (Einladungs-Mail)
- mein Passwort zurücksetzen können
- die Platzbelegung einer Woche sehen
- zwischen Wochen/Tagen vor- und zurückblättern oder zu einem Datum springen
- einen freien Slot buchen
- bei der Buchung einen Gastspieler eintragen (inkl. neuen Gast registrieren)
- meine eigenen Buchungen sehen
- eine eigene Buchung stornieren (innerhalb der Stornofrist)
- meine Stammdaten sehen und ändern

## Als Admin möchte ich…
- neue Mitglieder anlegen (sie erhalten automatisch die Einladungs-Mail)
- Mitglieder deaktivieren, ohne sie zu löschen
- alle Buchungen sehen und bei Bedarf stornieren
- Plätze einmalig sperren (Regen, Turnier, Platzpflege)
- Plätze als wöchentliche Serie sperren (Trainer-Workaround)
- die aktive Saison konfigurieren (Start, Ende, Öffnungszeiten)
- die Buchungsregeln konfigurieren
- die Gastspieler-Nutzungsliste für die Offline-Abrechnung verwalten

## System-getriggerte E-Mails
- Nach Buchung: Bestätigung an Mitglied
- Nach Stornierung: Bestätigung an Mitglied
- Passwort-Reset angefordert: Mail mit Link
- Mitglied vom Admin angelegt: Willkommens-Mail mit Passwort-Setzen-Link

---

# Screen-Inventar

| #  | Screen                   | Rolle              | Priorität |
|----|--------------------------|--------------------|-----------|
| 1  | Login                    | public             | Kritisch  |
| 2  | Passwort vergessen       | public             | Hoch      |
| 3  | Passwort setzen          | public (via Token) | Hoch      |
| 4  | Wochengrid (Hauptansicht)| Mitglied           | Kritisch  |
| 5  | Buchungsdialog (Modal)   | Mitglied           | Kritisch  |
| 6  | Meine Buchungen          | Mitglied           | Hoch      |
| 7  | Profil                   | Mitglied           | Niedrig   |
| 8  | Admin-Dashboard          | Admin              | Mittel    |
| 9  | Mitglieder-Liste         | Admin              | Hoch      |
| 10 | Mitglied bearbeiten      | Admin              | Hoch      |
| 11 | Platz sperren            | Admin              | Hoch      |
| 12 | Saison-Einstellungen     | Admin              | Mittel    |
| 13 | Buchungsregeln           | Admin              | Mittel    |
| 14 | Gastspieler-Abrechnung   | Admin              | Mittel    |

---

# Globale Layout-Bausteine

**Top-Bar (eingeloggt):**
- Mobile: Logo links, Hamburger-Menü rechts, Titel zentriert
- Desktop: Logo links, horizontale Navigation mittig, Profil-Dropdown rechts mit Logout

**Bottom-Tab-Bar (nur Mobile, Mitglied-Bereich):** „Platzbelegung" | „Meine Buchungen" | „Profil"

**Admin-Sub-Navigation (Desktop):** Side-Nav mit Icons + Label für: Dashboard, Mitglieder, Platzsperren, Saison, Regeln, Abrechnung

**Snackbars:** für nicht-blockierende Erfolgs- und Info-Meldungen (Auto-Hide nach 4 Sekunden)

**Confirm-Dialog:** für alle destruktiven Aktionen (Stornierung, Deaktivierung, Reset)

---

# Screen-Briefings

## Screen 1: Login

**Zweck:** Mitglied authentifizieren.

**Rolle & Gerät:** Public, Mobile + Desktop, identisches Layout.

**Elemente:**
- Vereinslogo + Vereinsname oben zentriert
- Feld: E-Mail (`type="email"`, `autocomplete="username"`)
- Feld: Passwort (`autocomplete="current-password"`, Sichtbar-schalten-Icon)
- Link: „Passwort vergessen?" unter dem Passwort-Feld
- Primär-Button: „Anmelden"
- **Kein** Registrieren-Link — Selbst-Registrierung ist ausgeschlossen

**Zustände:**
- Leer / ruhend
- Während Request: Button disabled + Loading-Indicator
- Fehler: Inline-Meldung „E-Mail oder Passwort falsch" (generisch, keine Info ob Email existiert)

**Details:**
- Nach erfolgreichem Login: Redirect zum Wochengrid
- Login-Feld soll Browser-Passwort-Manager triggern

---

## Screen 2: Passwort vergessen

**Zweck:** Reset-Mail anfordern.

**Rolle & Gerät:** Public, Mobile + Desktop.

**Elemente:**
- Titel „Passwort zurücksetzen"
- Erklärungs-Text: „Gib deine E-Mail-Adresse ein. Wir schicken dir einen Link zum Passwort-Zurücksetzen."
- E-Mail-Feld
- Primär-Button: „Link anfordern"
- Sekundär-Link: „Zurück zum Login"

**Zustände:**
- Leer
- Submit läuft
- Erfolg: Bestätigungsseite „Falls die Adresse in unserem System registriert ist, haben wir dir einen Link geschickt." (Enumeration-Schutz — immer gleiche Antwort)

---

## Screen 3: Passwort setzen

**Zweck:** Passwort nach Einladung oder Reset setzen.

**Rolle & Gerät:** Public via Token-Link aus Mail, Mobile + Desktop.

**Elemente:**
- Titel „Passwort setzen" oder „Willkommen bei [Vereinsname]"
- Feld: Neues Passwort (mit Sichtbar-schalten-Icon und Stärke-Indikator)
- Feld: Passwort wiederholen
- Passwort-Anforderungen als Checkliste (mindestens 8 Zeichen etc.)
- Primär-Button: „Passwort setzen"

**Zustände:**
- Leer
- Validierungs-Fehler: Felder markieren + Hinweis
- Token ungültig/abgelaufen: Fehlerseite mit „Neuen Link anfordern"
- Erfolg: Auto-Login + Redirect oder Login mit Erfolgs-Snackbar

---

## Screen 4: Wochengrid (Hauptansicht) — HERO-SCREEN

**Zweck:** Mitglied sieht Verfügbarkeit und bucht. Das ist die Startseite nach Login — 80% der Nutzung.

**Rolle & Gerät:** Mitglied. Mobile ist der Haupt-Fokus.

**Design-Entscheidung:** Tagesansicht mit Tag-Tabs — keine echte Wochen-Matrix. Grund: 7 Tage × 5 Plätze × 15 Slots ist auf Mobile nicht sinnvoll darstellbar. Auf Desktop kann optional eine breitere Wochenansicht angeboten werden.

**Elemente (Mobile):**
- Top-Bar mit Logo + Hamburger
- Datum-Header: „Heute" / „Morgen" / formatiertes Datum mit kleinem Kalender-Icon zum Springen
- Horizontaler Swipe-Tab-Strip: 7 Tage der aktuellen Woche (Mo–So), aktueller Tag hervorgehoben
- Pfeile Prev/Next-Woche seitlich, oder Swipe-Geste über gesamten Grid
- Tabelle/Grid:
  - Spalten: Plätze („Platz 1", „Platz 2", …)
  - Zeilen: Zeit-Slots von Öffnungszeit bis Schlusszeit (z.B. 08:00 bis 22:00 in 1h-Schritten)
  - Zellen in vier Zuständen, farblich UND per Muster/Icon unterschieden:
    - **Frei** — heller/neutraler Hintergrund, tap → Buchungsdialog
    - **Meine Buchung** — Primärfarbe, Haken-Icon, tap → „Meine Buchung, stornieren?"
    - **Belegt** — Grau, „Belegt"-Label, nicht klickbar (kein Mitgliedsname aus DSGVO)
    - **Gesperrt** — Schraffur, Grund-Label (z.B. „Training", „Regen"), nicht klickbar
- Bottom-Tab-Bar: Platzbelegung (active) | Meine Buchungen | Profil

**Zustände:**
- Lädt (Cold-Start bis 30s möglich): Skeleton-Cells mit Pulse-Animation. Nach 3 Sekunden Text-Hinweis „Verbindung wird hergestellt…"
- Saison inaktiv: Hero-Card statt Grid mit „Saison startet am [Datum]"
- Fehler (Netzwerk): Retry-Button + Fehlertext
- Leere Öffnungszeit (unplausibel, aber möglich): „Für diesen Tag sind keine Zeiten konfiguriert"

**Desktop-Variante:**
- Breiterer Grid möglich, statt Tag-Tabs kann eine Wochen-Übersicht als Alternative angeboten werden
- Hover-State über Zellen zeigt Vorschau / Details

---

## Screen 5: Buchungsdialog (Modal) — HERO-SCREEN

**Zweck:** Slot-Details bestätigen, Gastspieler optional eintragen, Buchung auslösen.

**Rolle & Gerät:** Mitglied. Modal auf Mobile bedeutet Bottom-Sheet oder Fullscreen-Modal.

**Elemente:**
- Header: „Platz 3 buchen" (mit Schließen-Icon oben rechts)
- Info-Block: Datum, Uhrzeit-Slot, Dauer (aus Saison-Konfig)
- Toggle/Checkbox: „Mit Gastspieler spielen"
  - Wenn aktiviert: Autocomplete-Feld „Gast auswählen oder hinzufügen"
  - Autocomplete zeigt bisherige Gäste des Mitglieds (mit Datum des letzten Spiels als Kontext)
  - Option am Ende der Liste: „+ Neuen Gast hinzufügen"
  - Bei „Neuen Gast hinzufügen": Sub-Formular mit Vorname, Nachname, E-Mail (optional)
- Hinweis-Text (wenn Gast aktiv): „Gastspielergebühr bitte im Vereinsheim entrichten."
- Footer: Button „Abbrechen" (sekundär) links, Button „Buchen" (primär) rechts

**Zustände:**
- Leer / ruhend
- Submit läuft: „Buchen"-Button disabled + Spinner
- Erfolg: Modal schließt, Snackbar „Buchung bestätigt. Bestätigungsmail unterwegs."
- Fehler: Inline-Fehlermeldung im Modal (z.B. „Dieser Slot wurde gerade von einem anderen Mitglied gebucht.")

---

## Screen 6: Meine Buchungen

**Zweck:** Mitglied sieht eigene Buchungen, kann stornieren.

**Rolle & Gerät:** Mitglied. Mobile-optimiert.

**Elemente:**
- Titel „Meine Buchungen"
- Section 1: „Kommende" (default aufgeklappt)
  - Liste als Karten mit: Datum + Uhrzeit groß, Platz, Gast (falls vorhanden)
  - Karte hat „Stornieren"-Button wenn Stornofrist noch offen
  - Karte hat Info-Text „Stornierung nicht mehr möglich" wenn Frist abgelaufen
- Section 2: „Vergangene" (collapsible, standardmäßig eingeklappt)
  - Read-only, Historie der letzten X Wochen

**Zustände:**
- Keine Buchungen: Leerzustand mit Illustration + CTA „Jetzt Platz buchen" → Wochengrid
- Storno-Button tap: Confirm-Dialog „Buchung wirklich stornieren? Der Slot wird wieder freigegeben."
- Storno-Erfolg: Karte verschwindet, Snackbar „Buchung storniert."

---

## Screen 7: Profil

**Zweck:** Mitglied kann Stammdaten und Passwort ändern.

**Rolle & Gerät:** Mitglied. Mobile + Desktop.

**Elemente:**
- Titel „Profil"
- Sektion „Stammdaten":
  - Vorname (editierbar)
  - Nachname (editierbar)
  - E-Mail (read-only, mit Info-Icon „E-Mail-Änderung bitte beim Admin anfragen")
  - Button „Änderungen speichern"
- Sektion „Passwort":
  - Button „Passwort ändern" → öffnet Dialog mit: aktuelles Passwort, neues, wiederholen
- Sektion „Abmeldung":
  - Button „Abmelden" (sekundär, Warnfarbe)

**Details:**
- E-Mail ist read-only weil sie auch Login-ID ist; Änderungen würden den Auth-Flow verkomplizieren

---

## Screen 8: Admin-Dashboard

**Zweck:** Einstiegspunkt für Admin-Bereich.

**Rolle & Gerät:** Admin. Desktop-optimiert.

**Elemente:**
- Titel „Administration"
- KPI-Karten in Zeile (optional, nice-to-have):
  - „Heutige Buchungen: X"
  - „Aktive Mitglieder: X"
  - „Offene Gastspieler-Gebühren: X"
- Kachel-Grid mit Links zu den Admin-Unterseiten:
  - Mitglieder, Platzsperren, Saison, Regeln, Gastspieler-Abrechnung
- Optional: Kurz-Liste „Letzte Buchungen" (5 neueste)

---

## Screen 9: Mitglieder-Liste (Admin)

**Zweck:** Mitglieder verwalten (Liste + Suche + Neuanlage).

**Rolle & Gerät:** Admin. Desktop-fokussiert.

**Elemente:**
- Titel „Mitglieder" + primärer Button „+ Neues Mitglied" oben rechts
- Suchfeld (Name oder E-Mail)
- Filter-Chips: „Alle" | „Aktiv" | „Inaktiv" und Rollen-Filter („Member", „Trainer", „Admin")
- Tabelle mit Spalten: Name, E-Mail, Rolle, Status (Badge: Aktiv/Inaktiv), Aktionen (Icons: Bearbeiten, Deaktivieren)
- Pagination oder virtualisiertes Scrollen bei > 100 Einträgen

**„Neues Mitglied"-Dialog:**
- Felder: Vorname, Nachname, E-Mail, Rolle-Select
- Hinweis: „Nach dem Anlegen erhält das Mitglied automatisch eine Mail mit dem Passwort-Setzen-Link."
- Buttons: „Abbrechen" / „Mitglied anlegen"

---

## Screen 10: Mitglied bearbeiten (Admin)

**Zweck:** Einzelnes Mitglied anpassen.

**Rolle & Gerät:** Admin.

**Elemente:**
- Titel „[Vorname Nachname]"
- Sektion „Stammdaten": Vorname, Nachname, E-Mail (hier editierbar), Rolle-Select
- Sektion „Status": Toggle „Aktiv" mit Erklärungstext zum Effekt der Deaktivierung
- Sektion „Buchungen": Kurz-Liste der letzten 10 Buchungen dieses Mitglieds (read-only, für Kontext)
- Sektion „Gefährliche Aktionen" (klar abgegrenzt, rot):
  - Button „Passwort zurücksetzen" (triggert Reset-Mail)
  - Button „Deaktivieren" (nach Confirm)
- Footer: Buttons „Zurück" / „Speichern"

---

## Screen 11: Platz sperren (Admin)

**Zweck:** Einmalige oder wiederkehrende Platzsperren einrichten (Trainer-Workaround, Platzpflege, Turniere).

**Rolle & Gerät:** Admin. Ein Screen für beide Sperrarten.

**Elemente:**
- Titel „Plätze sperren"
- Liste der bestehenden Sperren oben, Filterbar nach Datum/Platz
  - Jede Zeile: Datum/Zeitraum, Platz, Grund, „Einmalig" oder „Serie" Badge, Entfernen-Icon
  - Bei Serien: Klick zeigt alle Vorkommnisse + Option „Ganze Serie löschen" oder „Nur diesen Termin"
- Primär-Button: „+ Neue Sperre"

**„Neue Sperre"-Formular:**
- Radio-Buttons: „Einmalig" / „Wöchentlich wiederkehrend"
- Platz-Select: „Platz 1", „Platz 2", … oder „Alle Plätze"
- Einmalig: Datum + Von/Bis Zeit
- Wöchentlich: Wochentag-Select + Von/Bis Zeit + Gültig ab Datum + Gültig bis Datum (Default: Saisonende)
- Grund-Textfeld (z.B. „Training Jugend", „Platzpflege")
- Buttons: „Abbrechen" / „Sperre anlegen"

**Zustände:**
- Konflikt mit bestehenden Buchungen: Dialog fragt nach „Bestehende Buchungen stornieren?" mit Liste der betroffenen Buchungen

---

## Screen 12: Saison-Einstellungen (Admin)

**Zweck:** Saison-Lifecycle konfigurieren.

**Rolle & Gerät:** Admin.

**Elemente:**
- Titel „Saison"
- Card „Aktuelle Saison":
  - Name (z.B. „Sommersaison 2026")
  - Datepicker: Start-Datum
  - Datepicker: Ende-Datum
  - Timepicker: Öffnungszeit
  - Timepicker: Schlusszeit
  - Number-Input: Slot-Dauer in Minuten (Default 60)
  - Button „Speichern"
- Warnhinweis: „Änderungen können bestehende Buchungen betreffen. Buchungen außerhalb der neuen Saisonzeiten bleiben bestehen, neue Buchungen sind jedoch nur innerhalb möglich."
- Sektion „Neue Saison anlegen" (für Jahreswechsel)

---

## Screen 13: Buchungsregeln (Admin)

**Zweck:** Konfigurierbare Regeln setzen.

**Rolle & Gerät:** Admin.

**Elemente:**
- Titel „Buchungsregeln"
- Drei Karten, jeweils mit Number-Input, Beschreibung und Beispiel:
  - „Maximale Tage im Voraus" — z.B. „7" bedeutet: Mitglieder können bis zu 7 Tage im Voraus buchen
  - „Mindeststornofrist (Stunden)" — z.B. „2" bedeutet: Stornierung bis spätestens 2 Stunden vor Spielbeginn möglich
  - „Max. gleichzeitige Buchungen pro Mitglied" — z.B. „2" bedeutet: jedes Mitglied kann maximal 2 offene Buchungen haben
- Gemeinsamer Button „Speichern"
- Info-Text: „Änderungen wirken ab sofort für neue Buchungen. Bestehende Buchungen bleiben unberührt."

---

## Screen 14: Gastspieler-Abrechnung (Admin)

**Zweck:** Offline-Abrechnung der Gastspieler-Gebühren unterstützen.

**Rolle & Gerät:** Admin.

**Elemente:**
- Titel „Gastspieler-Abrechnung"
- Filter-Leiste:
  - Monat-Select (Default: aktueller Monat)
  - Filter: „Alle" | „Offen" | „Bezahlt"
  - Freitext-Suche (Gastname oder Mitgliedsname)
- Tabelle mit Spalten:
  - Datum / Uhrzeit
  - Gastspieler (Name)
  - Einladendes Mitglied
  - Platz
  - Status (Checkbox zum direkten Umschalten „Bezahlt")
- Summen-Zeile unten: „X offene Gäste, Y bezahlte Gäste"
- Export-Button „Als CSV exportieren" (für Buchhaltung)

---

# Übergeordnete UX-Prinzipien

**Farbsystem für Slot-Zustände:** Grün = frei, Primärfarbe (Blau) = meine Buchung, Grau = belegt, Schraffur = gesperrt. Wichtig: nicht nur Farbe, auch Form/Pattern nutzen — Farbenblindheit betrifft ca. 8% der Männer.

**Destruktive Aktionen immer mit Confirm-Dialog:** Stornierung, Mitglied deaktivieren, Saison ändern, Serien-Sperre löschen — immer „Sind Sie sicher?"

**Loading-States ehrlich kommunizieren:** Azure-SQL-Auto-Pause kann beim ersten Request 10–30 Sekunden dauern. Nach 3 Sekunden Loading: Text „Verbindung wird hergestellt…". Nach 15 Sekunden: „Das dauert länger als gewöhnlich. System startet gerade auf."

**Zeitangaben lokalisieren:** `de-AT`, Format „Mo, 15.04.2026 18:00". Nie ISO, nie US-Format.

**Touch-Targets mindestens 44×44 Pixel** (Apple HIG) bzw. 48×48 (Material). Für Slot-Zellen im Grid: bei 5 Plätzen auf 380px schon knapp, horizontales Scrollen einkalkulieren.

**Leerzustände mit Orientierung:** Jede Liste braucht einen sinnvollen „Leer"-Zustand mit nächstem Schritt (z.B. „Keine Buchungen — jetzt Platz buchen").

**Fehlermeldungen handlungsorientiert:** Nicht „Fehler 500", sondern „Die Buchung konnte nicht angelegt werden. Bitte versuche es in einem Moment erneut."

**Enumeration-Schutz bei Auth:** Login- und Passwort-Reset-Meldungen verraten nie, ob eine E-Mail existiert.

**DSGVO-konform:** Im Wochengrid nie Namen anderer Mitglieder anzeigen (nur „Belegt"). In der Gastspieler-Liste nur Personen mit Einverständnis.
