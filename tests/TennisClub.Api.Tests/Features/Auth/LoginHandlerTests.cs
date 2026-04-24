using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Features.Auth.Login;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Auth;

public class LoginHandlerTests : IAsyncLifetime
{
    private AuthTestHost _host = null!;

    public Task InitializeAsync()
    {
        _host = new AuthTestHost();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndPersistsRefreshToken()
    {
        await _host.SeedMemberAsync("alice@club.test", "Alice123!");
        var handler = _host.Services.GetRequiredService<LoginHandler>();

        var result = await handler.HandleAsync(
            new LoginRequest("alice@club.test", "Alice123!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.RefreshTokens.SingleAsync();
        persisted.RevokedAt.Should().BeNull();
        persisted.TokenHash.Should().NotBe(result.Value.RefreshToken, "raw token must never be stored");
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _host.SeedMemberAsync("bob@club.test", "Bob12345!");
        var handler = _host.Services.GetRequiredService<LoginHandler>();

        var result = await handler.HandleAsync(
            new LoginRequest("bob@club.test", "wrong-password"),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Error.Should().Be("Login fehlgeschlagen.");
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsSameGenericError()
    {
        var handler = _host.Services.GetRequiredService<LoginHandler>();

        var result = await handler.HandleAsync(
            new LoginRequest("nobody@club.test", "whatever-123"),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Error.Should().Be("Login fehlgeschlagen.",
            "because enumeration protection requires identical responses for unknown vs. wrong-password");
    }

    [Fact]
    public async Task Login_InactiveMember_IsRejectedEvenWithCorrectPassword()
    {
        await _host.SeedMemberAsync("carol@club.test", "Carol123!", isActive: false);
        var handler = _host.Services.GetRequiredService<LoginHandler>();

        var result = await handler.HandleAsync(
            new LoginRequest("carol@club.test", "Carol123!"),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tokenCount = await db.RefreshTokens.CountAsync();
        tokenCount.Should().Be(0, "no refresh token should be issued for inactive users");
    }
}
