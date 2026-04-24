# Buchungsregeln & Concurrency

Der Kern der Applikation. Hier entscheidet sich Korrektheit unter Last.

## Die 9 Regeln

Eine Buchung muss alle neun Regeln erfüllen:

**Strukturelle Regeln (hardcoded):**
1. `StartsAt < EndsAt`, Dauer entspricht `Season.SlotDurationMinutes`
2. Slot liegt innerhalb der aktiven Saison (`StartDate..EndDate`)
3. Slot liegt innerhalb der Öffnungszeiten (`OpeningTime..ClosingTime`)
4. Court ist aktiv (`IsActive`)
5. Kein aktiver `CourtBlock` überlappt den Slot
6. Keine andere aktive `Reservation` überlappt den Slot
7. Slot liegt nicht in der Vergangenheit

**Konfigurierbare Regeln (aus `SystemSettings`):**
8. Nicht weiter als `MaxAdvanceBookingDays` in der Zukunft
9. Mitglied hat weniger als `MaxOpenReservationsPerMember` aktive Buchungen

## Rule Engine Pattern

Jede Regel ist eine eigene Klasse, damit sie einzeln testbar und erweiterbar ist.

### Interfaces

```csharp
public record BookingAttempt(
    Guid MemberId,
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

public record RuleResult(bool IsValid, string? Code = null, string? Message = null)
{
    public static RuleResult Ok() => new(true);
    public static RuleResult Fail(string code, string msg) => new(false, code, msg);
}

public interface IBookingRule
{
    Task<RuleResult> CheckAsync(BookingAttempt attempt, CancellationToken ct);
}
```

### Beispiel-Regel

```csharp
public sealed class NoOverlappingReservationRule(AppDbContext db) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var exists = await db.Reservations.AnyAsync(r =>
            r.CourtId == a.CourtId
            && r.Status == ReservationStatus.Active
            && r.StartsAt < a.EndsAt
            && r.EndsAt > a.StartsAt, ct);

        return exists
            ? RuleResult.Fail("OVERLAP", "Der Platz ist zu dieser Zeit bereits gebucht.")
            : RuleResult.Ok();
    }
}

public sealed class SlotIsNotInPastRule(TimeProvider time) : IBookingRule
{
    public Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct) =>
        Task.FromResult(a.StartsAt > time.GetUtcNow()
            ? RuleResult.Ok()
            : RuleResult.Fail("IN_PAST", "Der Slot liegt in der Vergangenheit."));
}

public sealed class MaxAdvanceBookingRule(
    AppDbContext db, TimeProvider time) : IBookingRule
{
    public async Task<RuleResult> CheckAsync(BookingAttempt a, CancellationToken ct)
    {
        var settings = await db.SystemSettings.FirstAsync(ct);
        var maxDate = time.GetUtcNow().AddDays(settings.MaxAdvanceBookingDays);

        return a.StartsAt <= maxDate
            ? RuleResult.Ok()
            : RuleResult.Fail("TOO_FAR", $"Buchungen sind nur bis {settings.MaxAdvanceBookingDays} Tage im Voraus möglich.");
    }
}
```

### Alle Regeln zu implementieren

Zu implementierende Rule-Klassen in `Features/Reservations/Rules/`:

- `SlotBoundsAreValidRule` — StartsAt < EndsAt, Dauer passt
- `SlotIsWithinSeasonRule` — innerhalb aktiver Saison
- `SlotIsWithinOpeningHoursRule` — innerhalb Öffnungszeiten
- `CourtIsActiveRule` — Court.IsActive == true
- `NoCourtBlockExistsRule` — kein überlappender CourtBlock
- `NoOverlappingReservationRule` — keine überlappende Active Reservation
- `SlotIsNotInPastRule` — StartsAt > now
- `MaxAdvanceBookingRule` — StartsAt <= now + MaxAdvanceBookingDays
- `MaxOpenReservationsRule` — Count(Active for Member) < MaxOpenReservationsPerMember

### Orchestrator

```csharp
public sealed class BookingRuleEngine(IEnumerable<IBookingRule> rules)
{
    public async Task<IReadOnlyList<RuleResult>> CheckAsync(
        BookingAttempt attempt, CancellationToken ct)
    {
        var failures = new List<RuleResult>();
        foreach (var rule in rules)
        {
            var result = await rule.CheckAsync(attempt, ct);
            if (!result.IsValid) failures.Add(result);
        }
        return failures;
    }
}
```

Registrierung via `AddScoped` für jede `IBookingRule`-Implementierung. Ein `AddBookingRules()`-Extension im Program.cs bündelt das.

**Wichtig: alle Regeln laufen (kein Fail-Fast).** User soll alle Fehler auf einmal sehen, nicht iterativ eine nach der anderen beheben.

## Concurrency — drei Verteidigungsschichten

### Schicht 1: Rule Engine (UX)

Fängt die meisten Fälle mit schönen Fehlermeldungen. Aber zwischen Check und Insert liegen Millisekunden — reicht nicht allein.

### Schicht 2: Filtered Unique Index

Die eigentliche Wahrheit in der DB. In `IEntityTypeConfiguration<Reservation>`:

```csharp
builder.HasIndex(r => new { r.CourtId, r.StartsAt })
    .HasFilter("[Status] = 0")   // 0 = Active
    .IsUnique();
```

Der `HasFilter` ist essenziell — sonst würden stornierte Buchungen neue Buchungen auf denselben Slot blockieren.

**Bei Violation:** `DbUpdateException` wird geworfen. Muss im Handler abgefangen werden.

### Schicht 3: RowVersion (Updates)

Für Updates (typisch Stornierung). `[Timestamp] byte[] RowVersion` auf der Entity. Bei konkurrierender Änderung wirft EF Core `DbUpdateConcurrencyException`.

Beim reinen Insert ist RowVersion irrelevant — der Filtered Unique Index übernimmt.

## Handler-Pattern

Der komplette `CreateReservationHandler`:

```csharp
public sealed class CreateReservationHandler(
    AppDbContext db,
    BookingRuleEngine rules,
    TimeProvider time)
{
    public async Task<Result<Guid>> HandleAsync(
        CreateReservationRequest req, Guid memberId, CancellationToken ct)
    {
        var attempt = new BookingAttempt(memberId, req.CourtId, req.StartsAt, req.EndsAt);

        // Schicht 1
        var failures = await rules.CheckAsync(attempt, ct);
        if (failures.Count > 0) return Result.Invalid(failures);

        // Schicht 2
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CourtId = req.CourtId,
            MemberId = memberId,
            GuestPlayerId = req.GuestPlayerId,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            Status = ReservationStatus.Active,
            CreatedAt = time.GetUtcNow()
        };

        db.Reservations.Add(reservation);

        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success(reservation.Id);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            return Result.Conflict("Der Slot wurde gerade von jemand anderem gebucht.");
        }
    }
}
```

### DbUpdateException-Extension

```csharp
public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException ex)
    {
        if (ex.InnerException is not SqlException sql) return false;
        // 2627 = Unique Constraint, 2601 = Unique Index
        return sql.Number is 2627 or 2601;
    }
}
```

## CancelReservationHandler (mit RowVersion)

```csharp
public sealed class CancelReservationHandler(
    AppDbContext db, TimeProvider time)
{
    public async Task<Result> HandleAsync(
        Guid reservationId, Guid memberId, byte[] rowVersion, CancellationToken ct)
    {
        var settings = await db.SystemSettings.FirstAsync(ct);
        var reservation = await db.Reservations.FindAsync([reservationId], ct);

        if (reservation is null || reservation.MemberId != memberId)
            return Result.NotFound();

        if (reservation.Status != ReservationStatus.Active)
            return Result.Invalid("Diese Buchung ist bereits storniert.");

        var hoursUntilStart = (reservation.StartsAt - time.GetUtcNow()).TotalHours;
        if (hoursUntilStart < settings.MinCancellationHours)
            return Result.Invalid($"Stornierung nur bis {settings.MinCancellationHours}h vor Beginn möglich.");

        // RowVersion-basiertes optimistisches Locking
        db.Entry(reservation).Property(r => r.RowVersion).OriginalValue = rowVersion;
        reservation.Status = ReservationStatus.Cancelled;
        reservation.CancelledAt = time.GetUtcNow();

        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Conflict("Die Buchung wurde inzwischen geändert, bitte neu laden.");
        }
    }
}
```

## Edge-Case: MaxOpenReservations-Race

Szenario: User hat 1 offene Buchung, öffnet zwei Browser-Tabs, klickt gleichzeitig auf „Buchen". Beide Requests lesen `count=1`, beide inserten → User hat 3 offene Buchungen.

**Entscheidung für den MVP: akzeptieren und im Admin-Dashboard einen Alert anzeigen, wenn ein Mitglied das Limit überschreitet.** Bei 50–200 Mitgliedern extrem selten, Lösungen dafür wären überdimensioniert:

- Serializable Transaction würde alle Buchungen verlangsamen
- Pessimistisches Locking kompliziert den Handler
- Nach-Prüfung mit Rollback verdoppelt den DB-Roundtrip

Alle drei Varianten sind dokumentiert — falls sich der Edge-Case in Produktion häufen sollte, ist die Entscheidung später rückgängig machbar.

## Testing

**Unit-Tests pro Rule-Klasse** mit `FakeTimeProvider` und In-Memory oder SQLite-EF-Context:

```csharp
[Fact]
public async Task SlotInPast_ReturnsInvalid()
{
    var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-01T10:00:00Z"));
    var rule = new SlotIsNotInPastRule(time);

    var result = await rule.CheckAsync(
        new BookingAttempt(Guid.NewGuid(), 1,
            DateTimeOffset.Parse("2026-04-30T18:00:00Z"),
            DateTimeOffset.Parse("2026-04-30T19:00:00Z")),
        CancellationToken.None);

    result.IsValid.Should().BeFalse();
    result.Code.Should().Be("IN_PAST");
}
```

**Integration-Tests für den Handler** mit `WebApplicationFactory` und Testcontainers MsSql (für realistische Unique-Constraint-Tests).

**Besonders wichtig zu testen:**
- Concurrency: zwei parallele Create-Requests auf denselben Slot — einer erfolgreich, einer mit Conflict
- Cancel nach Frist vs. vor Frist
- Cancel durch anderen User als Owner (403)
- RowVersion-Mismatch bei Cancel

## Was wir bewusst NICHT bauen

- **Keine `UPDLOCK`/`HOLDLOCK` Hints** — EF Core verschleiert diese, und der Unique Index löst das Kernproblem besser
- **Kein Redis-Lock** über die Buchungsoperation — unnötige Komplexität, Unique Index reicht
- **Keine Retry-Logic bei Unique-Violation** — wenn der Slot weg ist, ist er weg. User wählt einen anderen.
