using System.Text.Json;

using DnsForwarder.Events;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Exporters;

public sealed class NullEventExporter : BackgroundService
{
    private readonly EventBus _bus;
    private readonly ILogger<NullEventExporter> _logger;

    public NullEventExporter(EventBus bus, ILogger<NullEventExporter> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NullEventExporter started");
        await foreach (var evt in _bus.ConsumeAsync(stoppingToken))
        {
            // Do nothing just flush the bus
        }

        _logger.LogInformation("NullEventExporter stopped");
    }
}
