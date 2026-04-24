# Backend-Projektstruktur

Solution Layout für ASP.NET Core 10 mit Vertical Slice Architecture.

## Grundsatzentscheidung: Ein Projekt

**Keine 4-Schichten-Solution** (`.Domain`, `.Application`, `.Infrastructure`, `.Api`). Stattdessen ein einziges Projekt für die API plus Test-Projekt.

**Begründung:** Eine Vertical Slice enthält Endpoint + Handler + Validator + Request + (optionale) Regeln in einem Ordner. Bei 4-Schichten-Split zerfleddert jedes Feature über alle Projekte — kämpft gegen das Pattern.

## Vollständige Struktur

```
TennisClub/
├── src/
│   └── TennisClub.Api/
│       ├── Features/                   ← Vertical Slices
│       │   ├── Auth/
│       │   │   ├── Login/
│       │   │   │   ├── LoginEndpoint.cs
│       │   │   │   ├── LoginHandler.cs
│       │   │   │   ├── LoginRequest.cs
│       │   │   │   └── LoginValidator.cs
│       │   │   ├── Refresh/
│       │   │   ├── Logout/
│       │   │   ├── ForgotPassword/
│       │   │   ├── ResetPassword/
│       │   │   └── Shared/
│       │   │       ├── AuthResponse.cs
│       │   │       └── JwtSettings.cs
│       │   │
│       │   ├── Reservations/
│       │   │   ├── Create/
│       │   │   │   ├── CreateReservationEndpoint.cs
│       │   │   │   ├── CreateReservationHandler.cs
│       │   │   │   ├── CreateReservationRequest.cs
│       │   │   │   └── CreateReservationValidator.cs
│       │   │   ├── Cancel/
│       │   │   ├── ListMine/
│       │   │   ├── ListForWeek/
│       │   │   └── Rules/               ← gehört in Reservations-Domäne
│       │   │       ├── IBookingRule.cs
│       │   │       ├── BookingAttempt.cs
│       │   │       ├── RuleResult.cs
│       │   │       ├── BookingRuleEngine.cs
│       │   │       ├── SlotBoundsAreValidRule.cs
│       │   │       ├── SlotIsNotInPastRule.cs
│       │   │       ├── SlotIsWithinSeasonRule.cs
│       │   │       ├── SlotIsWithinOpeningHoursRule.cs
│       │   │       ├── CourtIsActiveRule.cs
│       │   │       ├── NoCourtBlockExistsRule.cs
│       │   │       ├── NoOverlappingReservationRule.cs
│       │   │       ├── MaxAdvanceBookingRule.cs
│       │   │       └── MaxOpenReservationsRule.cs
│       │   │
│       │   ├── Courts/
│       │   │   ├── List/
│       │   │   ├── BlockOnce/
│       │   │   └── BlockWeekly/         ← Trainer-Workaround
│       │   │
│       │   ├── Members/
│       │   │   ├── Create/
│       │   │   ├── List/
│       │   │   ├── Update/
│       │   │   └── Deactivate/
│       │   │
│       │   ├── GuestPlayers/
│       │   │   ├── Create/
│       │   │   ├── ListForMember/
│       │   │   └── ListAll/
│       │   │
│       │   └── Admin/
│       │       ├── UpdateSettings/
│       │       ├── UpdateSeason/
│       │       └── GuestPlayerBilling/
│       │
│       ├── Domain/
│       │   ├── Entities/
│       │   │   ├── Member.cs
│       │   │   ├── Court.cs
│       │   │   ├── Reservation.cs
│       │   │   ├── GuestPlayer.cs
│       │   │   ├── CourtBlock.cs
│       │   │   ├── Season.cs
│       │   │   ├── SystemSettings.cs
│       │   │   └── RefreshToken.cs
│       │   └── Enums/
│       │       └── ReservationStatus.cs
│       │
│       ├── Infrastructure/
│       │   ├── Persistence/
│       │   │   ├── AppDbContext.cs
│       │   │   ├── AppDbContextFactory.cs   ← für dotnet ef
│       │   │   ├── Configurations/          ← IEntityTypeConfiguration<T>
│       │   │   │   ├── MemberConfiguration.cs
│       │   │   │   ├── CourtConfiguration.cs
│       │   │   │   ├── ReservationConfiguration.cs
│       │   │   │   ├── GuestPlayerConfiguration.cs
│       │   │   │   ├── CourtBlockConfiguration.cs
│       │   │   │   ├── SeasonConfiguration.cs
│       │   │   │   ├── SystemSettingsConfiguration.cs
│       │   │   │   └── RefreshTokenConfiguration.cs
│       │   │   └── Seed/
│       │   │       └── SeedData.cs
│       │   ├── Email/
│       │   │   ├── IEmailSender.cs
│       │   │   ├── SmtpEmailSender.cs
│       │   │   ├── EmailMessage.cs
│       │   │   ├── EmailQueue.cs
│       │   │   ├── EmailDispatcher.cs
│       │   │   ├── EmailTemplateRenderer.cs
│       │   │   └── Templates/
│       │   │       ├── booking-confirmation.sbn
│       │   │       ├── booking-cancellation.sbn
│       │   │       ├── welcome.sbn
│       │   │       └── password-reset.sbn
│       │   └── Auth/
│       │       └── JwtTokenService.cs
│       │
│       ├── Common/
│       │   ├── Results/
│       │   │   └── Result.cs
│       │   ├── Endpoints/
│       │   │   ├── IEndpoint.cs
│       │   │   └── EndpointExtensions.cs
│       │   └── Exceptions/
│       │       └── DbUpdateExceptionExtensions.cs
│       │
│       ├── Migrations/                      ← EF Core generiert
│       ├── Properties/
│       │   └── launchSettings.json
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Program.cs
│       ├── TennisClub.Api.csproj
│       └── Dockerfile
│
├── tests/
│   └── TennisClub.Api.Tests/
│       ├── Features/
│       │   ├── Reservations/
│       │   │   ├── CreateReservationHandlerTests.cs
│       │   │   ├── CancelReservationHandlerTests.cs
│       │   │   └── Rules/
│       │   │       ├── SlotIsNotInPastRuleTests.cs
│       │   │       ├── NoOverlappingReservationRuleTests.cs
│       │   │       └── MaxAdvanceBookingRuleTests.cs
│       │   └── Auth/
│       │       ├── LoginHandlerTests.cs
│       │       └── RefreshHandlerTests.cs
│       ├── Integration/
│       │   ├── TestBase.cs                  ← WebApplicationFactory
│       │   ├── ReservationEndpointTests.cs
│       │   └── AuthEndpointTests.cs
│       └── TennisClub.Api.Tests.csproj
│
├── .editorconfig
├── .gitignore
├── docker-compose.yml                       ← lokales SQL für Development
├── TennisClub.sln
└── README.md
```

## IEndpoint-Pattern

Jedes Feature registriert sein Endpoint selbst. Kein zentraler Controller, kein Boilerplate.

### Common/Endpoints/IEndpoint.cs

```csharp
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

### Common/Endpoints/EndpointExtensions.cs

```csharp
public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        var endpointType = typeof(IEndpoint);
        var types = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => endpointType.IsAssignableFrom(t)
                && !t.IsInterface
                && !t.IsAbstract);

        foreach (var type in types)
        {
            services.AddScoped(endpointType, type);
        }
        return services;
    }

    public static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        var scope = app.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetServices<IEndpoint>();
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }
        return app;
    }
}
```

## Beispiel einer Vertical Slice

`Features/Reservations/Create/`:

```csharp
// CreateReservationRequest.cs
public sealed record CreateReservationRequest(
    int CourtId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid? GuestPlayerId);

// CreateReservationValidator.cs
public sealed class CreateReservationValidator
    : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationValidator()
    {
        RuleFor(x => x.CourtId).GreaterThan(0);
        RuleFor(x => x.StartsAt).LessThan(x => x.EndsAt);
    }
}

// CreateReservationHandler.cs - siehe @docs/booking-rules.md

// CreateReservationEndpoint.cs
public sealed class CreateReservationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost("/api/reservations", async (
            CreateReservationRequest req,
            IValidator<CreateReservationRequest> validator,
            CreateReservationHandler handler,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var memberId = user.GetMemberId();
            var result = await handler.HandleAsync(req, memberId, ct);

            return result.Match(
                id => Results.Created($"/api/reservations/{id}", new { id }),
                invalid => Results.BadRequest(invalid),
                conflict => Results.Conflict(conflict));
        })
        .RequireAuthorization();
}
```

## NuGet-Pakete (API-Projekt)

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.*" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.*" />
  <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.*" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.*" />
  <PackageReference Include="FluentValidation.AspNetCore" Version="11.*" />
  <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="12.*" />
  <PackageReference Include="Serilog.AspNetCore" Version="10.*" />
  <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="3.*" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="10.*" />
  <PackageReference Include="MailKit" Version="4.*" />
  <PackageReference Include="Scriban" Version="7.*" />
</ItemGroup>
```

## NuGet-Pakete (Test-Projekt)

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.*" />
  <!-- FluentAssertions 8.x wechselte auf kommerzielle Lizenz (Jan 2025).
       7.2.0 ist die letzte kostenfreie Version. Pinnen bis wir wissen, wohin. -->
  <PackageReference Include="FluentAssertions" Version="7.2.0" />
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
  <PackageReference Include="Testcontainers.MsSql" Version="4.*" />
  <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.*" />
</ItemGroup>
```

## Designentscheidungen

- **Ein Projekt statt vier.** Vertical Slice + 4-Layer kämpfen gegeneinander.
- **Entities im selben Projekt** (`Domain/Entities/`). Keine separate `.Domain.dll`, solange kein anderer Consumer existiert.
- **Minimal APIs mit IEndpoint.** Kein Controller-Boilerplate, Endpoint lebt im Slice.
- **Plain Handler statt MediatR.** Weniger Magie, expliziter, schnellerer Build. MediatR hat zudem kürzliche Lizenzänderungen für kommerzielle Nutzung.
- **FluentValidation per Feature.** Validator lebt neben Request, nicht zentral.
- **Kein AutoMapper.** Manuelles Mapping im Handler — bei Projekt-Größe spart AutoMapper keine echte Zeit und macht Refactoring schwerer.
- **IEntityTypeConfiguration<T> pro Entity** statt alles im OnModelCreating — skaliert sauber.
- **Result-Pattern statt Exceptions für Business-Logik.** Exceptions nur für Unerwartetes.

## Commands für die Entwicklung

```bash
# Solution erstellen
dotnet new sln -n TennisClub
dotnet new webapi -n TennisClub.Api -o src/TennisClub.Api --use-minimal-apis
dotnet new xunit -n TennisClub.Api.Tests -o tests/TennisClub.Api.Tests
dotnet sln add src/TennisClub.Api tests/TennisClub.Api.Tests
dotnet add tests/TennisClub.Api.Tests reference src/TennisClub.Api

# EF Core Tools
dotnet tool install --global dotnet-ef

# Migration erstellen
dotnet ef migrations add InitialCreate --project src/TennisClub.Api

# DB aktualisieren
dotnet ef database update --project src/TennisClub.Api

# Migration-Script generieren (für Pipeline)
dotnet ef migrations script --idempotent -o migrate.sql --project src/TennisClub.Api

# Tests
dotnet test
```
