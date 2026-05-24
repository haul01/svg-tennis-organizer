using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TennisClub.Api.Domain.Entities;
using TennisClub.Api.Features.Auth.Shared;
using TennisClub.Api.Features.Members.Create;
using TennisClub.Api.Features.Members.ImportCsv;
using TennisClub.Api.Infrastructure.Email;
using TennisClub.Api.Infrastructure.Persistence;
using TennisClub.Api.Infrastructure.Persistence.Seed;
using TennisClub.Api.Tests.TestInfrastructure;

namespace TennisClub.Api.Tests.Features.Members;

public class ImportCsvHandlerTests : IAsyncLifetime
{
    private AuthTestHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = new AuthTestHost();
        await _host.EnsureAllRolesAsync();
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    private ImportCsvHandler BuildHandler(IServiceScope scope)
    {
        var sp = scope.ServiceProvider;
        var createHandler = new CreateMemberHandler(
            sp.GetRequiredService<UserManager<Member>>(),
            sp.GetRequiredService<EmailQueue>(),
            sp.GetRequiredService<EmailTemplateRenderer>(),
            Options.Create(new FrontendSettings { BaseUrl = "http://localhost:4200" }),
            _host.Time);

        return new ImportCsvHandler(
            sp.GetRequiredService<UserManager<Member>>(),
            createHandler);
    }

    private static Stream Csv(string text) =>
        new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public async Task ImportsRows_AndSkipsHeader()
    {
        using var scope = _host.Services.CreateScope();
        var handler = BuildHandler(scope);

        var csv = """
            Vorname;Nachname;Email
            Anna;Müller;anna@example.com
            Ben;Huber;ben@example.com
            """;

        var summary = await handler.HandleAsync(Csv(csv), CancellationToken.None);

        summary.TotalRows.Should().Be(2);
        summary.Created.Should().Be(2);
        summary.SkippedEmails.Should().BeEmpty();
        summary.Failed.Should().BeEmpty();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Users.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task SkipsExistingEmails()
    {
        await _host.SeedMemberAsync("anna@example.com", "Password1!");

        using var scope = _host.Services.CreateScope();
        var handler = BuildHandler(scope);

        var csv = """
            Vorname;Nachname;Email
            Anna;Müller;anna@example.com
            Ben;Huber;ben@example.com
            """;

        var summary = await handler.HandleAsync(Csv(csv), CancellationToken.None);

        summary.Created.Should().Be(1);
        summary.SkippedEmails.Should().BeEquivalentTo(["anna@example.com"]);
        summary.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task ReportsMalformedRows()
    {
        using var scope = _host.Services.CreateScope();
        var handler = BuildHandler(scope);

        var csv = """
            Vorname;Nachname;Email
            Anna;Müller;anna@example.com
            BrokenRow
            ;Huber;ben@example.com
            """;

        var summary = await handler.HandleAsync(Csv(csv), CancellationToken.None);

        summary.Created.Should().Be(1);
        summary.Failed.Should().HaveCount(2);
        summary.Failed.Should().Contain(e => e.Message.Contains("weniger als drei"));
        summary.Failed.Should().Contain(e => e.Message.Contains("leer"));
    }

    [Fact]
    public async Task AssignsMemberRole()
    {
        using var scope = _host.Services.CreateScope();
        var handler = BuildHandler(scope);

        var csv = """
            Vorname;Nachname;Email
            Anna;Müller;anna@example.com
            """;

        await handler.HandleAsync(Csv(csv), CancellationToken.None);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<Member>>();
        var anna = await users.FindByEmailAsync("anna@example.com");
        anna.Should().NotBeNull();
        var roles = await users.GetRolesAsync(anna!);
        roles.Should().ContainSingle().Which.Should().Be(SeedData.MemberRole);
    }

    [Fact]
    public async Task SupportsCommaSeparator()
    {
        using var scope = _host.Services.CreateScope();
        var handler = BuildHandler(scope);

        var csv = """
            Vorname,Nachname,Email
            Anna,Müller,anna@example.com
            """;

        var summary = await handler.HandleAsync(Csv(csv), CancellationToken.None);

        summary.Created.Should().Be(1);
        summary.Failed.Should().BeEmpty();
    }
}
