using System.Net;
using System.Net.Sockets;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.Core;

public sealed class DnsServer : BackgroundService
{
    private readonly ILogger<DnsServer> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly DnsForwarderService _forwarder;
    private UdpClient? _udp;

    public DnsServer(
        ILogger<DnsServer> logger,
        DnsForwarderOptions options,
        DnsForwarderService forwarder)
    {
        _logger = logger;
        _options = options;
        _forwarder = forwarder;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listenAddress = IPAddress.Parse(_options.Listen.Address);
        var endpoint = new IPEndPoint(listenAddress, _options.Listen.Port);

        _udp = new UdpClient(endpoint);

        _logger.LogInformation(
            "DNS forwarder listening on {Address}:{Port}",
            _options.Listen.Address,
            _options.Listen.Port);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(stoppingToken);

                // Fire-and-forget request handler
                _ = HandleRequestAsync(result, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving DNS packet");
            }
        }
    }

    private async Task HandleRequestAsync(UdpReceiveResult result, CancellationToken ct)
    {
        try
        {
            // Pass remote endpoint into forwarder for logging
            var responseBytes = await _forwarder.ProcessAsync(
                result.Buffer,
                result.RemoteEndPoint,
                ct);

            if (responseBytes is not null)
            {
                await _udp!.SendAsync(
                    responseBytes,
                    responseBytes.Length,
                    result.RemoteEndPoint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing DNS request from {Remote}",
                result.RemoteEndPoint);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _udp?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing UDP listener");
        }

        return base.StopAsync(cancellationToken);
    }
}
