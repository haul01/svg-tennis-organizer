using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Features.Auth.Refresh;
using TennisClub.Api.Infrastructure.Auth;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Auth;

public class RefreshHandlerTests : IAsyncLifetime
{
    private AuthTestHost _host = null!;

    public Task InitializeAsync()
    {
        _host = new AuthTestHost();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Refresh_WithValidToken_RotatesTokenAndReturnsNewPair()
    {
        var member = await _host.SeedMemberAsync();
        const string originalToken = "valid-refresh-token-abcd";
        var persistedOld = await _host.SeedRefreshTokenAsync(member.Id, originalToken);

        var handler = _host.Services.GetRequiredService<RefreshHandler>();

        var result = await handler.HandleAsync(
            new RefreshRequest(originalToken),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBe(originalToken);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var oldToken = await db.RefreshTokens.FindAsync(persistedOld.Id);
        oldToken!.RevokedAt.Should().NotBeNull("old token must be revoked on rotation");
        oldToken.ReplacedByTokenId.Should().NotBeNull("rotation chain must be linked");

        var replacement = await db.RefreshTokens
            .SingleAsync(t => t.Id == oldToken.ReplacedByTokenId);
        replacement.TokenHash.Should().Be(JwtTokenService.Hash(result.Value.RefreshToken!));
        replacement.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_ReturnsUnauthorized()
    {
        var member = await _host.SeedMemberAsync();
        const string token = "already-revoked-token";
        await _host.SeedRefreshTokenAsync(
            member.Id, token,
            revokedAt: _host.Time.GetUtcNow().AddMinutes(-1));

        var handler = _host.Services.GetRequiredService<RefreshHandler>();

        var result = await handler.HandleAsync(
            new RefreshRequest(token),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        var member = await _host.SeedMemberAsync();
        const string token = "expired-token";
        await _host.SeedRefreshTokenAsync(
            member.Id, token,
            expiresAt: _host.Time.GetUtcNow().AddMinutes(-1));

        var handler = _host.Services.GetRequiredService<RefreshHandler>();

        var result = await handler.HandleAsync(
            new RefreshRequest(token),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        var handler = _host.Services.GetRequiredService<RefreshHandler>();

        var result = await handler.HandleAsync(
            new RefreshRequest("never-issued-token"),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithInactiveMember_ReturnsUnauthorized()
    {
        var member = await _host.SeedMemberAsync(isActive: false);
        const string token = "for-inactive-user";
        await _host.SeedRefreshTokenAsync(member.Id, token);

        var handler = _host.Services.GetRequiredService<RefreshHandler>();

        var result = await handler.HandleAsync(
            new RefreshRequest(token),
            CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Unauthorized);
    }
}
