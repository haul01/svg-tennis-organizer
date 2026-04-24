# Datenmodell

Vollständiges Entity-Modell des MVPs. EF Core Code-First mit Azure SQL.

## Übersicht

```
Member (IdentityUser<Guid>) ──< Reservation >── Court
                    │              │
                    │              └──? GuestPlayer
                    │
                    └──< GuestPlayer (als InvitedBy)
                    │
                    └──< RefreshToken

Court ──< CourtBlock

Season (standalone, aktiv per Datum)
SystemSettings (Single-Row)
```

## Entities

### Member

Extends `IdentityUser<Guid>`. Gibt Identity alle Auth-Felder (Email, PasswordHash, etc.) automatisch.

```csharp
public class Member : IdentityUser<Guid>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = [];
}
```

**Rollen:** via ASP.NET Identity Roles — `Member`, `Trainer`, `Admin`. Mindestens ein Admin wird im Seed angelegt.

### Court

```csharp
public class Court
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;   // z.B. "Platz 1"
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
```

Seed: 3–5 Plätze vorinitialisieren, aber konfigurierbar.

### Season

```csharp
public class Season
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;   // "Sommersaison 2026"
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public TimeOnly OpeningTime { get; set; }
    public TimeOnly ClosingTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 60;
}
```

Die aktive Saison wird per `StartDate <= today <= EndDate` ermittelt. Mehrere Saisons können koexistieren (für die nächste Saison vorbereiten, ohne die aktuelle zu ändern).

### Reservation

Die Kern-Entity. Enthält `RowVersion` für optimistisches Locking und `Status` als Soft-Cancel-Flag.

```csharp
public class Reservation
{
    public Guid Id { get; set; }

    public int CourtId { get; set; }
    public Court Court { get; set; } = null!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public Guid? GuestPlayerId { get; set; }
    public GuestPlayer? GuestPlayer { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

public enum ReservationStatus { Active, Cancelled }
```

**Wichtige Constraints (in `IEntityTypeConfiguration<Reservation>`):**

```csharp
// Filtered Unique Index - Hauptverteidigung gegen Doppelbuchungen
builder.HasIndex(r => new { r.CourtId, r.StartsAt })
    .HasFilter("[Status] = 0")   // 0 = Active
    .IsUnique();

// Index für Query "Reservations for week"
builder.HasIndex(r => new { r.StartsAt, r.CourtId })
    .HasFilter("[Status] = 0");

// Index für "My reservations"
builder.HasIndex(r => new { r.MemberId, r.Status, r.StartsAt });
```

### GuestPlayer

Nicht-Identity-Entity. Wird vom einladenden Mitglied angelegt, hat keinen Login.

```csharp
public class GuestPlayer
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }

    public Guid InvitedByMemberId { get; set; }
    public Member InvitedBy { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
```

UI zeigt bei Buchung eine Autocomplete-Liste bisheriger Gäste des jeweiligen Mitglieds.

### CourtBlock

Platzsperren — einmalig oder als wöchentliche Serie.

```csharp
public class CourtBlock
{
    public Guid Id { get; set; }

    public int CourtId { get; set; }
    public Court Court { get; set; } = null!;

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Reason { get; set; } = null!;   // z.B. "Training Jugend", "Regen"

    public Guid? SeriesId { get; set; }   // null = einmalig, gleicher Wert = Serie
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedByMemberId { get; set; }
}
```

**Serien-Handling:** Wenn Admin „wöchentlich Mo 18–19 Uhr für die ganze Saison" anlegt, werden direkt N einzelne `CourtBlock`-Datensätze mit derselben `SeriesId` materialisiert. Das vereinfacht Queries und erlaubt einzelne Ausnahmen durch Löschen eines Blocks.

### SystemSettings

Single-Row-Tabelle für konfigurierbare Buchungsregeln.

```csharp
public class SystemSettings
{
    public int Id { get; set; }
    public int MaxAdvanceBookingDays { get; set; } = 7;
    public int MinCancellationHours { get; set; } = 2;
    public int MaxOpenReservationsPerMember { get; set; } = 2;
}
```

Nur eine Zeile. Seed-Logik legt diese beim ersten Start an. Im Admin-UI editierbar, in der Rule Engine pro Request frisch gelesen (oder gecacht mit Invalidierung bei Update).

### RefreshToken

Für rotierende Refresh Tokens.

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public string TokenHash { get; set; } = null!;   // SHA-256, nie Klartext
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }     // Rotation-Chain
}
```

**Index:**
```csharp
builder.HasIndex(t => t.TokenHash).IsUnique();
builder.HasIndex(t => new { t.MemberId, t.RevokedAt });
```

## EF Core Konfiguration

- Eine Klasse `IEntityTypeConfiguration<T>` pro Entity im Ordner `Infrastructure/Persistence/Configurations/`
- `AppDbContext` erbt von `IdentityDbContext<Member, IdentityRole<Guid>, Guid>`
- `OnModelCreating` ruft `base.OnModelCreating` auf und lädt alle Configurations per `ApplyConfigurationsFromAssembly`
- Migrations-Ordner: `src/TennisClub.Api/Migrations/`

## Initiale Seed-Daten

Im ersten Run angelegt:

- 1 Admin-Account (Credentials aus Config)
- 1 aktuelle Saison (aus Config)
- 3–5 Courts (aus Config)
- `SystemSettings` mit Default-Werten

Seed-Code in `Infrastructure/Persistence/Seed/SeedData.cs`, aufgerufen aus `Program.cs` hinter einer Environment-Check.

## Design-Begründungen

- **`DateTimeOffset` statt `DateTime`** — Österreich hat Sommerzeit, `DateTimeOffset` speichert den Offset und verhindert Timezone-Bugs. Azure SQL mapped es auf `datetimeoffset`.
- **Soft-Cancel via Status** — History bleibt, Filtered Unique Index funktioniert, Gastspieler-Liste rückwirkend auswertbar
- **`RowVersion` auf Reservation** — schützt Updates (Stornierung), nicht Inserts. Für Inserts ist der Filtered Unique Index zuständig.
- **CourtBlock materialisiert statt Template** — simpler Queries, einzelne Ausnahmen durch Löschen eines Records statt Expand-Logic
- **`SystemSettings` als Single-Row** — simpler als Key-Value-EAV, ein Formular im Admin-UI
- **GuestPlayer kein IdentityUser** — Gäste wollen sich nicht einloggen, unnötiger Overhead vermieden
- **Kein Mitspieler-Table** — Einzel-only im MVP, Doppel kommt in V3

## Wichtig für die Umsetzung

- **Concurrency:** `RowVersion` auf `Reservation` ist Pflicht
- **Index:** Filtered Unique Index auf `(CourtId, StartsAt)` ist die Hauptverteidigung gegen Doppelbuchungen
- **Cascading:** Bei Member-Delete sollen `Reservation` NICHT gelöscht werden (Historie), stattdessen `OnDelete(DeleteBehavior.Restrict)`. Bei Court-Delete analog.
- **GuestPlayer löschen:** `OnDelete(DeleteBehavior.SetNull)` auf `Reservation.GuestPlayerId` — Buchung bleibt, Gast-Referenz wird `null`

## Migration-Strategie

- Migrations werden in der Pipeline ausgeführt, nicht beim App-Startup (siehe @docs/deployment.md)
- `dotnet ef migrations script --idempotent` für sichere Wiederanwendbarkeit
- Entwicklung lokal: `dotnet ef database update` gegen lokalen SQL Server (Docker oder SQL Server Express)
