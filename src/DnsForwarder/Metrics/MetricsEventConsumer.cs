using DnsForwarder.Events;

namespace DnsForwarder.Metrics;

public sealed class MetricsEventConsumer : IEventConsumer
{
    private readonly MetricsRegistry _registry;

    public MetricsEventConsumer(MetricsRegistry registry)
    {
        _registry = registry;
    }

    public void Consume(EventRecord evt)
    {
        switch (evt)
        {
            // DNS Query
            case DnsQueryEvent q:
                _registry.RecordDnsQuery(q);
                break;

            // DNS Response
            case DnsResponseEvent r:
                _registry.RecordDnsResponse(r);
                break;

            // DNS Cache Hit
            case DnsCacheHitEvent:
                _registry.RecordDnsCacheHit();
                break;

            // DNS Latency
            case DnsLatencyEvent l:
                _registry.RecordDnsLatency(l.Seconds);
                break;

            // DHCP Lease Allocated
            case DhcpLeaseAllocatedEvent d1:
                _registry.RecordDhcpLeaseAllocated(d1);
                break;

            // DHCP Lease Released
            case DhcpLeaseReleasedEvent d2:
                _registry.RecordDhcpLeaseReleased(d2);
                break;

            // NTP Sync
            case NtpSyncEvent n:
                _registry.RecordNtpSync(n);
                break;
        }
    }
}
