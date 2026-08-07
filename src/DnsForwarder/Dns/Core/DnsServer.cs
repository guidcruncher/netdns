using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

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
    private Channel<UdpReceiveResult>? _channel;

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

        // Create a bounded channel and worker pool to decouple receive from processing
        _channel = Channel.CreateBounded<UdpReceiveResult>(new BoundedChannelOptions(4096)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

        var workers = new List<Task>();
        int workerCount = Math.Max(1, Environment.ProcessorCount);
        for (int i = 0; i < workerCount; i++)
        {
            workers.Add(Task.Run(async () =>
            {
                var reader = _channel.Reader;
                while (await reader.WaitToReadAsync(stoppingToken))
                {
                    UdpReceiveResult item;
                    try
                    {
                        item = await reader.ReadAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    try
                    {
                        await HandleRequestAsync(item, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling DNS request in worker");
                    }
                }
            }, stoppingToken));
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(stoppingToken);

                // Enqueue for processing by worker pool
                await _channel.Writer.WriteAsync(result, stoppingToken);
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

        // Shutdown
        _channel.Writer.Complete();
        await Task.WhenAll(workers);
    }

    private async Task HandleRequestAsync(UdpReceiveResult result, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Parse DNS query for logging
            var parsed = DnsMessage.TryParse(result.Buffer);

            if (parsed is not null)
            {
                _metrics.RecordDnsQuery(new DnsQueryEvent(
                    Timestamp: DateTime.UtcNow,
                    ClientIp: result.RemoteEndPoint.Address,
                    ClientName: null, // DHCP hostname integration optional
                    QueryName: parsed.QuestionName,
                    QueryType: parsed.QuestionType));
            }

            // Forward to upstream resolver
            var response = await _forwarder.ProcessAsync(
                result.Buffer,
                result.RemoteEndPoint,
                ct);

            if (response is not null)
            {
                // Send the received buffer (may be pooled)
                await _udp!.SendAsync(
                    response.Buffer,
                    response.Length,
                    result.RemoteEndPoint);

                // Parse response for logging
                var resp = DnsMessage.TryParse(response.Buffer);

                if (resp is not null)
                {
                    _metrics.RecordDnsResponse(new DnsResponseEvent(
                        Timestamp: DateTime.UtcNow,
                        ClientIp: result.RemoteEndPoint.Address,
                        ClientName: null,
                        QueryName: resp.QuestionName,
                        QueryType: resp.QuestionType,
                        Status: resp.ResponseCode.ToString(),
                        ResponseIp: resp.AnswerAddress));
                }

                // Return pooled buffer if applicable
                response.Return();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing DNS request from {Remote}",
                result.RemoteEndPoint);
        }
        finally
        {
            sw.Stop();

            _metrics.RecordDnsLatency(sw.Elapsed.TotalSeconds);
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
