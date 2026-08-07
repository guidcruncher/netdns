using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Events;

public sealed class EventDispatcherService : BackgroundService
{
    private readonly ILogger<EventDispatcherService> _logger;
    private readonly EventBus _bus;
    private readonly IEnumerable<IEventConsumer> _consumers;

    public EventDispatcherService(
        ILogger<EventDispatcherService> logger,
        EventBus bus,
        IEnumerable<IEventConsumer> consumers)
    {
        _logger = logger;
        _bus = bus;
        _consumers = consumers;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event dispatcher started.");

        await foreach (var evt in _bus.ConsumeAsync(stoppingToken))
        {
            foreach (var consumer in _consumers)
            {
                try
                {
                    consumer.Consume(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consumer failed processing event {Event}", evt);
                }
            }
        }
    }
}
