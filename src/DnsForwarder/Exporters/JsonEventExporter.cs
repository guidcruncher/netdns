using System.Text.Json;

using DnsForwarder;
using DnsForwarder.Events;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Exporters;

public sealed class JsonEventExporter : BackgroundService
{
    private readonly EventBus _bus;
    private readonly ILogger<JsonEventExporter> _logger;
    private readonly string _filePath;

    public JsonEventExporter(EventBus bus, ILogger<JsonEventExporter> logger, ServerOptions config)
    {
        _bus = bus;
        _logger = logger;
        _filePath = Path.Combine(config.Metrics.Location, "events.log");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JsonEventExporter started, writing to {File}", _filePath);

        await foreach (var evt in _bus.ConsumeAsync(stoppingToken))
        {
            try
            {
                var json = JsonSerializer.Serialize(evt);
                await File.AppendAllTextAsync(_filePath, json + Environment.NewLine, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write event");
            }
        }

        _logger.LogInformation("JsonEventExporter stopped");
    }
}
