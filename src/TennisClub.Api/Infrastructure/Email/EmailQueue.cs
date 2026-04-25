using System.Threading.Channels;

namespace TennisClub.Api.Infrastructure.Email;

/// <summary>
/// In-process queue for outbound mail. Bounded so a runaway producer
/// can't blow up memory; full → wait. Survival across app restart is
/// out of scope for the MVP — for password resets we'd send synchronously
/// instead if zero-loss were a hard requirement.
/// </summary>
public sealed class EmailQueue
{
    private readonly Channel<EmailMessage> _channel = Channel.CreateBounded<EmailMessage>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(message, ct);

    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
