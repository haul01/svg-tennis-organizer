# Design-System „Advantage Point"

Quelle: Stitch-Projekt `12333706382444206285`, Design-System Asset
`07a52528a12545089a1481faaeda4545`. Diese Datei ist die Arbeitskopie —
bei Änderungen am Stitch-System nachziehen.

**Positionierung:** Corporate Modern mit athletischem Akzent. Viel Weißraum,
klare Flächen, hohe Kontraste nur bei Kernaktionen — der Reservierungsgrid
soll der Fokus bleiben.

## Farb-Tokens

### Marken-Akzente (Hero-Farben)

| Token | Hex | Einsatz |
|-------|-----|---------|
| `--tc-deep-navy` | `#0A192F` | Primary-Aktion, Toolbar, starke Aktionen |
| `--tc-tennis-green` | `#C1FF22` | „Meine Buchung", Primary-CTAs in Flächen |
| `--tc-slate` | `#64748B` | Text sekundär, Icons, Outlines |

### Surface-Palette (Material-3-Tokens aus Stitch)

| Token | Hex | Einsatz |
|-------|-----|---------|
| `--tc-surface` | `#f8f9ff` | App-Hintergrund (Slate-50) |
| `--tc-surface-dim` | `#cbdbf5` | Gedimmte Fläche |
| `--tc-surface-bright` | `#f8f9ff` | Helle Fläche |
| `--tc-surface-container-lowest` | `#ffffff` | Karten, Modale |
| `--tc-surface-container-low` | `#eff4ff` | Subtile Gruppen |
| `--tc-surface-container` | `#e5eeff` | Standard-Container |
| `--tc-surface-container-high` | `#dce9ff` | Hervorgehobene Flächen |
| `--tc-surface-container-highest` | `#d3e4fe` | Maximale Tonalität ohne Primary |
| `--tc-on-surface` | `#0b1c30` | Primärer Text auf Surface |
| `--tc-on-surface-variant` | `#44474d` | Sekundärer Text |
| `--tc-outline` | `#75777e` | Standard-Outline |
| `--tc-outline-variant` | `#c5c6cd` | Leise Outline (Karten, Zellen) |

### Zell-Zustände im Wochengrid

| Zustand | Farbe | Zusätzlich |
|---------|-------|------------|
| Frei | `--tc-surface-container-lowest` | 1px `--tc-outline-variant` Border |
| Meine Buchung | `--tc-tennis-green` | `--tc-deep-navy` Text, Haken-Icon |
| Belegt (fremd) | `--tc-surface-dim` | Keine Namen anzeigen |
| Gesperrt | `--tc-deep-navy` | Schraffur-Overlay, Reason als Label |

Wichtig: nicht nur Farbe differenzieren — immer auch Form/Pattern nutzen.

### Status / Feedback

| Token | Hex | Einsatz |
|-------|-----|---------|
| `--tc-error` | `#ba1a1a` | Fehler-Text, zerstörerische Aktionen |
| `--tc-on-error` | `#ffffff` | Text auf Error-Fläche |
| `--tc-error-container` | `#ffdad6` | Dezente Fehler-Banner |
| `--tc-on-error-container` | `#93000a` | Text auf Error-Container |

## Typografie

Paarung **Lexend** (Headlines, rhythmisch) + **Inter** (Body, Utility).

| Rolle | Font | Größe | Weight | Line-Height | Letter-Spacing |
|-------|------|-------|--------|-------------|----------------|
| `headline-xl` | Lexend | 40px | 700 | 1.2 | -0.02em |
| `headline-lg` | Lexend | 32px | 600 | 1.2 | 0 |
| `headline-md` | Lexend | 24px | 600 | 1.3 | 0 |
| `body-lg` | Inter | 18px | 400 | 1.6 | 0 |
| `body-md` | Inter | 16px | 400 | 1.6 | 0 |
| `label-bold` | Inter | 14px | 600 | 1.2 | 0.05em |
| `label-sm` | Inter | 12px | 500 | 1.2 | 0 |

## Spacing

4-er Raster:

| Token | Wert |
|-------|------|
| `--tc-space-base` | 4px |
| `--tc-space-xs` | 8px |
| `--tc-space-sm` | 16px |
| `--tc-space-md` | 24px |
| `--tc-space-lg` | 40px |
| `--tc-space-xl` | 64px |
| `--tc-space-grid-gutter` | 12px |

Seitenmargins: 24–40px. Grid-Gutter 12px.

## Radius

| Token | Wert |
|-------|------|
| `--tc-radius-sm` | 4px (`0.25rem`) |
| `--tc-radius` | 8px (`0.5rem`, Default für Buttons/Inputs) |
| `--tc-radius-md` | 12px (`0.75rem`) |
| `--tc-radius-lg` | 16px (`1rem`) |
| `--tc-radius-xl` | 24px (`1.5rem`, Karten/Modale) |
| `--tc-radius-full` | `9999px` |

## Elevation

Bevorzugt Tonalität statt Schatten:

- **Level 0 (Background):** Slate-50 flat.
- **Level 1 (Cards):** Weiß mit 1px `--tc-outline-variant` Border, kein Schatten.
- **Level 2 (Active/Hover):** `0 2px 4px rgba(10, 25, 47, 0.08)`.
- **Overlays (Modal, Drawer):** `0 8px 24px rgba(10, 25, 47, 0.16)`.

## Buttons

- **Primary:** `--tc-deep-navy` Hintergrund, weißer Text. Für „Anmelden",
  „Buchung bestätigen" etc.
- **Secondary:** weißer Hintergrund, 1px `--tc-deep-navy` Border.
- **Accent:** `--tc-tennis-green` Hintergrund, `--tc-deep-navy` Text.
  Sparsam einsetzen („Book Now", „Check-In").

## Material-3-Mapping

Das Stitch-Export enthält die vollen Material-3-Token-Namen
(`surface`, `primary`, `outline`, …). In Angular-Material kann man das
Custom-Theme über `@use '@angular/material' as mat;` und die eigenen
Farb-Tokens zusammensetzen. Für einfache Screens genügt es, die
CSS-Variablen aus der Tabelle oben zu verwenden.

## Verbindliche Referenz

Alle UI-Arbeit zieht die Tokens aus dieser Datei. Bei Konflikten zwischen
Stitch-Screen und DESIGN.md entscheidet DESIGN.md, bis die Datei
aktualisiert wird.
