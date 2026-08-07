using DnsForwarder.Events;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Metrics;

public sealed class MetricsEventConsumer : IEventConsumer
{
    private readonly MetricsRegistry _registry;
    private readonly ILogger<MetricsEventConsumer> _logger;

    public MetricsEventConsumer(
        MetricsRegistry registry,
        ILogger<MetricsEventConsumer> logger)
    {
        _registry = registry;
        _logger = logger;

        _logger.LogInformation(
            "MetricsEventConsumer created. Registry hash: {Hash}",
            _registry.GetHashCode());
    }

    public void Consume(EventRecord evt)
    {
        if (evt == null)
        {
            _logger.LogWarning("Consume called with null event");
            return;
        }

        _logger.LogInformation("Consume received event: {EventType}", evt.GetType().Name);

        switch (evt)
        {
            case DnsQueryEvent q:
                _logger.LogInformation("Recording DNS Query event: {Event}", q);
                _registry.RecordDnsQuery(q);
                break;

            case DnsResponseEvent r:
                _logger.LogInformation("Recording DNS Response event: {Event}", r);
                _registry.RecordDnsResponse(r);
                break;

            case DnsCacheHitEvent h:
                _logger.LogInformation("Recording DNS Cache Hit event");
                _registry.RecordDnsCacheHit();
                break;

            case DnsLatencyEvent l:
                _logger.LogInformation("Recording DNS Latency event: {Seconds}", l.Seconds);
                _registry.RecordDnsLatency(l.Seconds);
                break;

            case DhcpLeaseAllocatedEvent d1:
                _logger.LogInformation("Recording DHCP Lease Allocated event: {Event}", d1);
                _registry.RecordDhcpLeaseAllocated(d1);
                break;

            case DhcpLeaseReleasedEvent d2:
                _logger.LogInformation("Recording DHCP Lease Released event: {Event}", d2);
                _registry.RecordDhcpLeaseReleased(d2);
                break;

            case NtpSyncEvent n:
                _logger.LogInformation(
                    "Recording NTP Sync event: Success={Success}, OffsetMs={OffsetMs}",
                    n.Success,
                    n.Offset.TotalMilliseconds);
                _registry.RecordNtpSync(n);
                break;

            default:
                _logger.LogWarning("Unknown event type received: {EventType}", evt.GetType().Name);
                break;
        }
    }
}
