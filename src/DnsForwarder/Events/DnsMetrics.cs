namespace DnsForwarder.Events;

public sealed class DnsMetrics : IDnsMetrics
{
    private readonly EventBus _bus;

    public DnsMetrics(EventBus bus)
    {
        _bus = bus;
    }

    public void Query(DnsQueryEvent evt)
        => _bus.Publish(evt);

    public void Response(DnsResponseEvent evt)
        => _bus.Publish(evt);

    public void UpstreamLatency(DnsUpstreamLatencyEvent evt)
        => _bus.Publish(evt);
}
