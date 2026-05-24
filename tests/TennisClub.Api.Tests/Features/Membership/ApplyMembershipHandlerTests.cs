using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TennisClub.Api.Features.Membership.Apply;
using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Tests.Features.Membership;

public class ApplyMembershipHandlerTests
{
    private static (ApplyMembershipHandler Handler, EmailQueue Queue) Build(
        string adminEmail = "admin@club.test",
        string smtpFrom = "noreply@club.test")
    {
        var queue = new EmailQueue();
        var renderer = new EmailTemplateRenderer();
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-05-20T10:00:00Z"));

        var handler = new ApplyMembershipHandler(
            queue,
            renderer,
            Options.Create(new MembershipApplicationSettings { NotificationEmail = adminEmail }),
            Options.Create(new SmtpSettings { FromAddress = smtpFrom }),
            time,
            NullLogger<ApplyMembershipHandler>.Instance);
        return (handler, queue);
    }

    private static ApplyMembershipRequest SampleRequest() =>
        new(
            FirstName: "Anna",
            LastName: "Beispiel",
            Street: "Hauptstraße 1",
            PostalCode: "4201",
            City: "Gramastetten",
            BirthDate: new DateOnly(1990, 5, 1),
            Phone: "+43 660 1234567",
            Email: "anna@example.com",
            FeeTier: MembershipFeeTiers.AdultE100,
            Comment: "Freue mich auf die Saison.");

    [Fact]
    public async Task SendsBothMails()
    {
        var (handler, queue) = Build();
        await handler.HandleAsync(SampleRequest(), CancellationToken.None);

        var collected = new List<EmailMessage>();
        await foreach (var msg in queue.ReadAllAsync(CancellationToken.None))
        {
            collected.Add(msg);
            if (collected.Count == 2) break;
        }

        collected.Should().HaveCount(2);
        collected.Should().Contain(m => m.To == "admin@club.test"
            && m.Subject.Contains("Anna Beispiel"));
        collected.Should().Contain(m => m.To == "anna@example.com"
            && m.Subject.Contains("Beitrittserklärung"));
    }

    [Fact]
    public async Task FallsBackToSmtpFromAddress_WhenAdminConfigEmpty()
    {
        var (handler, queue) = Build(adminEmail: "", smtpFrom: "fallback@club.test");
        await handler.HandleAsync(SampleRequest(), CancellationToken.None);

        var collected = new List<EmailMessage>();
        await foreach (var msg in queue.ReadAllAsync(CancellationToken.None))
        {
            collected.Add(msg);
            if (collected.Count == 2) break;
        }

        collected.Should().Contain(m => m.To == "fallback@club.test");
    }

    [Fact]
    public async Task IncludesFeeTierLabel_InAdminMail()
    {
        var (handler, queue) = Build();
        var req = SampleRequest() with { FeeTier = MembershipFeeTiers.CoupleE190 };
        await handler.HandleAsync(req, CancellationToken.None);

        var collected = new List<EmailMessage>();
        await foreach (var msg in queue.ReadAllAsync(CancellationToken.None))
        {
            collected.Add(msg);
            if (collected.Count == 2) break;
        }

        var admin = collected.Single(m => m.To == "admin@club.test");
        admin.HtmlBody.Should().Contain("Kombi Ehepaare");
        admin.PlainTextBody.Should().NotBeNull();
        admin.PlainTextBody!.Should().Contain("Kombi Ehepaare");
    }

    [Fact]
    public async Task EmitsCommentBlock_OnlyWhenCommentPresent()
    {
        var (handler, queue) = Build();
        var noComment = SampleRequest() with { Comment = null };
        await handler.HandleAsync(noComment, CancellationToken.None);

        var collected = new List<EmailMessage>();
        await foreach (var msg in queue.ReadAllAsync(CancellationToken.None))
        {
            collected.Add(msg);
            if (collected.Count == 2) break;
        }

        var admin = collected.Single(m => m.To == "admin@club.test");
        admin.HtmlBody.Should().NotContain("Anmerkung der/des Antragstellenden");
    }
}
