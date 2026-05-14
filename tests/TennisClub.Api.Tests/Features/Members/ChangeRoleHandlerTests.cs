using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TennisClub.Api.Common.Results;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Members.ChangeRole;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Infrastructure.Persistence.Seed;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Members;

public class ChangeRoleHandlerTests : IAsyncLifetime
{
    private AuthTestHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = new AuthTestHost();
        // ChangeRoleHandler may target any of the four roles; pre-seed
        // them all so AddToRoleAsync finds a normalized row.
        await _host.EnsureAllRolesAsync();
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private async Task<ChangeRoleHandler> HandlerAsync()
    {
        // Build the handler with services from a single scope so the
        // UserManager + AppDbContext share a unit-of-work view.
        var scope = _host.Services.CreateAsyncScope();
        return new ChangeRoleHandler(
            scope.ServiceProvider.GetRequiredService<UserManager<Member>>(),
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            _host.Time);
    }

    [Fact]
    public async Task DemoteToGuest_Succeeds_AndRevokesRefreshTokens()
    {
        var member = await _host.SeedMemberAsync(
            "u@club.test", "Password1!", roles: [SeedData.MemberRole]);
        await _host.SeedRefreshTokenAsync(member.Id, "raw-refresh-token");

        var handler = await HandlerAsync();
        var result = await handler.HandleAsync(
            member.Id, new ChangeRoleRequest(SeedData.GuestRole), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(SeedData.GuestRole);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var token = await db.RefreshTokens.SingleAsync(t => t.MemberId == member.Id);
        token.RevokedAt.Should().NotBeNull(
            "the next login must mint a JWT carrying the new role");
    }

    [Fact]
    public async Task DemoteLastAdmin_Fails()
    {
        var soloAdmin = await _host.SeedMemberAsync(
            "admin@club.test", "Password1!", roles: [SeedData.AdminRole]);

        var handler = await HandlerAsync();
        var result = await handler.HandleAsync(
            soloAdmin.Id, new ChangeRoleRequest(SeedData.MemberRole), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("letzten Admin");
    }

    [Fact]
    public async Task DemoteAdmin_WhenAnotherExists_Succeeds()
    {
        await _host.SeedMemberAsync("a1@club.test", "Password1!", roles: [SeedData.AdminRole]);
        var demotee = await _host.SeedMemberAsync(
            "a2@club.test", "Password1!", roles: [SeedData.AdminRole]);

        var handler = await HandlerAsync();
        var result = await handler.HandleAsync(
            demotee.Id, new ChangeRoleRequest(SeedData.MemberRole), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(SeedData.MemberRole);
    }

    [Fact]
    public async Task PromoteGuestToMember_Succeeds()
    {
        var guest = await _host.SeedMemberAsync(
            "g@club.test", "Password1!", roles: [SeedData.GuestRole]);

        var handler = await HandlerAsync();
        var result = await handler.HandleAsync(
            guest.Id, new ChangeRoleRequest(SeedData.MemberRole), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(SeedData.MemberRole);
    }

    [Fact]
    public async Task UnknownMember_Returns_NotFound()
    {
        var handler = await HandlerAsync();
        var result = await handler.HandleAsync(
            Guid.NewGuid(), new ChangeRoleRequest(SeedData.MemberRole), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
