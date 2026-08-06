using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dhcp;

public sealed class DhcpHostedService : IHostedService
{
    private readonly ILogger<DhcpHostedService> _logger;
    private readonly DhcpServerEngine _engine;
    private CancellationTokenSource? _cts;

    public DhcpHostedService(ILogger<DhcpHostedService> logger, DhcpServerEngine engine)
    {
        _logger = logger;
        _engine = engine;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting DHCP server...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = _engine.RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping DHCP server...");
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
