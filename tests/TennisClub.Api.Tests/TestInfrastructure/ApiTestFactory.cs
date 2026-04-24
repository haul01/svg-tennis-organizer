using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace TennisClub.Api.Tests.TestInfrastructure;

/// <summary>
/// Boots the real API pipeline against a supplied connection string.
/// Uses the "Testing" environment so Program.cs skips the dev-time seed —
/// tests control seeding themselves for full determinism.
/// </summary>
public sealed class ApiTestFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:Issuer", "TennisClub.Api.Tests");
        builder.UseSetting("Jwt:Audience", "TennisClub.Api.Tests");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-chars-long!");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "7");

        builder.ConfigureLogging(log => log.ClearProviders());
    }
}
