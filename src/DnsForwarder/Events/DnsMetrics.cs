namespace DnsForwarder.Events;

public sealed class DnsMetrics : IDnsMetrics
{
    private readonly EventBus _bus;

    public DnsMetrics(EventBus bus)
    {
        _bus = bus;
    }

    public void RecordDnsQuery(DnsQueryEvent evt)
        => _bus.Publish(evt);

    public void RecordDnsResponse(DnsResponseEvent evt)
        => _bus.Publish(evt);

    public void RecordDnsCacheHit()
        => _bus.Publish(new DnsCacheHitEvent());

    public void RecordDnsLatency(double seconds)
        => _bus.Publish(new DnsLatencyEvent(seconds));
}
