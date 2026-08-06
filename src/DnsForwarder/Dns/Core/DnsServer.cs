using System.Net;
using System.Net.Sockets;

using DnsForwarder.Events;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Dns.Core;

public sealed class DnsServer : BackgroundService
{
    private readonly ILogger<DnsServer> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly DnsForwarderService _forwarder;
    private readonly IDnsMetrics _metrics;

    private UdpClient? _udp;

    public DnsServer(
        ILogger<DnsServer> logger,
        DnsForwarderOptions options,
        DnsForwarderService forwarder,
        IDnsMetrics metrics)
    {
        _logger = logger;
        _options = options;
        _forwarder = forwarder;
        _metrics = metrics;
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
            // Parse DNS query for logging
            var parsed = DnsMessage.TryParse(result.Buffer);

            if (parsed is not null)
            {
                _metrics.Query(new DnsQueryEvent(
                    Timestamp: DateTime.UtcNow,
                    ClientIp: result.RemoteEndPoint.Address,
                    ClientName: null, // DHCP hostname integration optional
                    QueryName: parsed.QuestionName,
                    QueryType: parsed.QuestionType));
            }

            // Forward to upstream resolver
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

                // Parse response for logging
                var resp = DnsMessage.TryParse(responseBytes);

                if (resp is not null)
                {
                    _metrics.Response(new DnsResponseEvent(
                        Timestamp: DateTime.UtcNow,
                        ClientIp: result.RemoteEndPoint.Address,
                        ClientName: null,
                        QueryName: resp.QuestionName,
                        QueryType: resp.QuestionType,
                        Status: resp.ResponseCode.ToString(),
                        ResponseIp: resp.AnswerAddress));
                }
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
