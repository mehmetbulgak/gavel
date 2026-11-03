using System.Threading.Channels;

namespace Gavel.Messaging.Outbox;

public sealed class OutboxSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
    });

    public void Notify() => _channel.Writer.TryWrite(true);

    public async Task WaitAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutSource.CancelAfter(timeout);
        try
        {
            await _channel.Reader.ReadAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
        }
    }
}
