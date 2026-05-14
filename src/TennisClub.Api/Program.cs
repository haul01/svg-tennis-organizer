using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TennisClub.Api.Common.Endpoints;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Admin.Diag.SendTestEmail;
using TennisClub.Api.Features.Admin.Reports.ListReservations;
using TennisClub.Api.Features.Auth.ForgotPassword;
using TennisClub.Api.Features.Auth.Login;
using TennisClub.Api.Features.Auth.Register;
using TennisClub.Api.Features.Auth.Logout;
using TennisClub.Api.Features.Auth.Refresh;
using TennisClub.Api.Features.Auth.ResetPassword;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Features.Courts.Create;
using TennisClub.Api.Features.Courts.List;
using TennisClub.Api.Features.Courts.Update;
using TennisClub.Api.Features.CourtBlocks.CreateOnce;
using TennisClub.Api.Features.CourtBlocks.CreateSeries;
using TennisClub.Api.Features.CourtBlocks.Delete;
using TennisClub.Api.Features.CourtBlocks.List;
using TennisClub.Api.Features.CourtBlocks.ListForWeek;
using TennisClub.Api.Features.CourtBlocks.Shared;
using TennisClub.Api.Features.GuestPlayers.Create;
using TennisClub.Api.Features.GuestPlayers.ListForMember;
using TennisClub.Api.Features.Members.ChangeRole;
using TennisClub.Api.Features.Members.Create;
using TennisClub.Api.Features.Members.Get;
using TennisClub.Api.Features.Members.List;
using TennisClub.Api.Features.Members.SetActive;
using TennisClub.Api.Features.Members.TriggerPasswordReset;
using TennisClub.Api.Features.Members.Update;
using TennisClub.Api.Features.Profile.ChangePassword;
using TennisClub.Api.Features.Profile.Get;
using TennisClub.Api.Features.Profile.Update;
using TennisClub.Api.Features.Reservations.Cancel;
using TennisClub.Api.Features.Reservations.Create;
using TennisClub.Api.Features.Reservations.ListForWeek;
using TennisClub.Api.Features.Reservations.ListMine;
using TennisClub.Api.Features.Reservations.Rules;
using TennisClub.Api.Features.Seasons.Current;
using TennisClub.Api.Features.Seasons.Update;
using TennisClub.Api.Features.Settings.Admin;
using TennisClub.Api.Features.Settings.Public;
using TennisClub.Api.Infrastructure.Auth;
using TennisClub.Api.Infrastructure.Email;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' not configured.");

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(connectionString));

// Identity (core only - we use JWT, not cookies).
// DataProtection is required for default token providers (password reset).
// In production with multiple replicas, persist keys (e.g. to Azure Blob).
builder.Services.AddDataProtection();

builder.Services.AddIdentityCore<Member>(opts =>
    {
        opts.Password.RequiredLength = 8;
        opts.User.RequireUniqueEmail = true;
        opts.SignIn.RequireConfirmedEmail = false;
        opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opts.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// JWT configuration.
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Jwt configuration section is missing.");

if (jwtSettings.SigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be at least 32 characters long.");
}
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        // Preserve original short claim names (sub, email, role) instead of
        // mapping them to legacy WS-* schema URIs.
        opts.MapInboundClaims = false;

        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "email",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Admin", p => p.RequireRole(SeedData.AdminRole));
    opts.AddPolicy("TrainerOrAdmin",
        p => p.RequireRole(SeedData.TrainerRole, SeedData.AdminRole));
});

// Rate limiting: protect login endpoint from brute-force.
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opts.AddPolicy("auth-login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Guest self-registration: each new account triggers a welcome mail
    // through Brevo's 300-mails/day quota, so cap per IP to keep a single
    // source from draining the quota.
    opts.AddPolicy("auth-register", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// Feature slice handlers.
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshHandler>();
builder.Services.AddScoped<LogoutHandler>();
builder.Services.AddScoped<ForgotPasswordHandler>();
builder.Services.AddScoped<RegisterHandler>();
builder.Services.AddScoped<ResetPasswordHandler>();
builder.Services.AddScoped<CreateReservationHandler>();
builder.Services.AddScoped<CancelReservationHandler>();
builder.Services.AddScoped<ListForWeekHandler>();
builder.Services.AddScoped<ListMineHandler>();
builder.Services.AddScoped<ListCourtsHandler>();
builder.Services.AddScoped<GetCurrentSeasonHandler>();
builder.Services.AddScoped<CreateGuestPlayerHandler>();
builder.Services.AddScoped<ListMyGuestsHandler>();
builder.Services.AddScoped<GetPublicSettingsHandler>();
builder.Services.AddScoped<GetProfileHandler>();
builder.Services.AddScoped<UpdateProfileHandler>();
builder.Services.AddScoped<ChangePasswordHandler>();
builder.Services.AddScoped<ListMembersHandler>();
builder.Services.AddScoped<GetMemberHandler>();
builder.Services.AddScoped<CreateMemberHandler>();
builder.Services.AddScoped<UpdateMemberHandler>();
builder.Services.AddScoped<SetActiveHandler>();
builder.Services.AddScoped<TriggerPasswordResetHandler>();
builder.Services.AddScoped<ChangeRoleHandler>();
builder.Services.AddScoped<UpdateSettingsHandler>();
builder.Services.AddScoped<UpdateSeasonHandler>();
builder.Services.AddScoped<CreateCourtHandler>();
builder.Services.AddScoped<UpdateCourtHandler>();
builder.Services.AddScoped<BlockConflictChecker>();
builder.Services.AddScoped<BlockCancellationNotifier>();
builder.Services.AddScoped<CreateOnceHandler>();
builder.Services.AddScoped<CreateSeriesHandler>();
builder.Services.AddScoped<ListBlocksHandler>();
builder.Services.AddScoped<ListBlocksForWeekHandler>();
builder.Services.AddScoped<DeleteBlockHandler>();
builder.Services.AddScoped<SendTestEmailHandler>();
builder.Services.AddScoped<ListReservationsHandler>();

// Booking rule engine + all nine rules.
builder.Services.AddBookingRules();

// FluentValidation - pick up all validators in this assembly.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Email pipeline.
// - Templates rendered via Scriban once per process (singleton).
// - Outbound mail flows through an in-memory queue + BackgroundService
//   so request handlers don't block on SMTP.
// - When Smtp:Host is configured, the production sender wins; otherwise
//   the dev logger writes the rendered HTML to the console so the
//   set-password / reset / confirmation links remain reachable.
builder.Services.AddSingleton<EmailTemplateRenderer>();
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService<EmailDispatcher>();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));

var smtpConfigured = builder.Configuration
    .GetSection(SmtpSettings.SectionName)
    .Get<SmtpSettings>()?.IsConfigured ?? false;
if (smtpConfigured)
{
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();
}

// Settings binding.
builder.Services.Configure<FrontendSettings>(
    builder.Configuration.GetSection(FrontendSettings.SectionName));
builder.Services.Configure<SeedOptions>(
    builder.Configuration.GetSection(SeedOptions.SectionName));

builder.Services.AddSingleton(TimeProvider.System);

// CORS for the Angular dev server. Production origins go into Cors:AllowedOrigins
// via container-app env vars.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Location"));
});

builder.Services.AddEndpoints();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Migrations + seed run in every environment except Testing. Single
// replica on the Pi makes Migrate-on-startup safe (no concurrency race),
// and the Ensure*-helpers in SeedData early-return when rows exist so
// re-running them on every boot is idempotent. Integration tests apply
// migrations + custom seed via ApiTestEnvironment themselves.
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
    await SeedData.RunAsync(app.Services);
}

app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.Run();

public partial class Program;
