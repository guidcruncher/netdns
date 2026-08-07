using System.Threading.Channels;

namespace DnsForwarder.Events;

public sealed class EventBus
{
    private readonly Channel<EventRecord> _channel;

    public EventBus(int capacity = 4096)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.DropWrite
        };

        _channel = Channel.CreateBounded<EventRecord>(options);
    }

    public bool Publish(EventRecord evt)
    {
        return _channel.Writer.TryWrite(evt);
    }

    public IAsyncEnumerable<EventRecord> ConsumeAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
