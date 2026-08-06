using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dhcp;

public sealed class DhcpServerEngine
{
    private readonly ILogger<DhcpServerEngine> _logger;
    private readonly DhcpOptions _config;

    public DhcpServerEngine(ILogger<DhcpServerEngine> logger, DhcpOptions config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("DHCP server listening on {Address}:{Port}",
            _config.ListenAddress, _config.ListenPort);

        // TODO: implement DHCP packet loop
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct);
        }
    }
}
