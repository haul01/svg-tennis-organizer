using TennisClub.Api.Infrastructure.Email;

namespace TennisClub.Api.Tests.TestInfrastructure;

public sealed class CollectingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
