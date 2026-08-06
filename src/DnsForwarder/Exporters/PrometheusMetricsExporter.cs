using DnsForwarder.Events;
using DnsForwarder.Metrics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Exporters;

public sealed class PrometheusMetricsExporter : BackgroundService
{
    private readonly ILogger<PrometheusMetricsExporter> _logger;
    private readonly EventBus _bus;
    private readonly MetricsRegistry _metrics;

    public PrometheusMetricsExporter(
        ILogger<PrometheusMetricsExporter> logger,
        EventBus bus,
        MetricsRegistry metrics)
    {
        _logger = logger;
        _bus = bus;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Prometheus metrics exporter started.");

        await foreach (var evt in _bus.ConsumeAsync(stoppingToken))
        {
            try
            {
                Record(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record metrics for event");
            }
        }
    }

    private void Record(EventRecord evt)
    {
        switch (evt)
        {
            case DnsQueryEvent q:
                _metrics.RecordDnsQuery(q);
                break;

            case DnsResponseEvent r:
                _metrics.RecordDnsResponse(r);
                break;

            case DhcpLeaseAllocatedEvent d:
                _metrics.RecordDhcpLeaseAllocated(d);
                break;

            case DhcpLeaseReleasedEvent rel:
                _metrics.RecordDhcpLeaseReleased(rel);
                break;

            case NtpSyncEvent n:
                _metrics.RecordNtpSync(n);
                break;
        }
    }
}
