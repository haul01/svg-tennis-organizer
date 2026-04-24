# Design-Assets

Designs werden primär über den **Stitch-MCP-Server** direkt in Claude Code integriert — siehe `CLAUDE.md` im Repo-Root. Dieser Ordner ist nur als **Fallback** für den seltenen Fall gedacht, dass der MCP-Server nicht verfügbar ist oder du Screenshots manuell ablegen willst.

## MCP-Workflow (der Normalfall)

1. Designs in Google Stitch pflegen
2. `@DESIGN.md` im Repo-Root wird von Stitch generiert und enthält das Design-System (Color Tokens, Typography, Spacing)
3. Claude Code ruft einzelne Screens via Stitch-MCP ab:
   > „Hole das Login-Screen-Design aus Stitch und generiere daraus die Angular-Component in `frontend/src/app/features/auth/login/`."

Setup siehe Stitch-MCP-Docs oder die Community-Wrapper (`stitch-mcp`, `@_davideast/stitch-mcp`).

## Fallback-Struktur (optional)

Falls doch mal manuell abgelegt werden muss:

```
design/
├── html/                ← HTML/CSS-Exports aus Stitch
│   └── <screen>.html
└── screenshots/         ← PNG-Exports, falls HTML nicht verfügbar
    └── <screen>.png
```

## Screen-Spezifikationen

Was inhaltlich auf jedem Screen sein muss (Felder, Zustände, Fehlerbehandlung), steht in `../docs/screen-briefings.md`. Das ist unabhängig vom visuellen Design und bleibt für Claude Code die Wahrheit für den Screen-Inhalt.
