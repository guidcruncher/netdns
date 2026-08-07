using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsForwarder.Events;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Ntp;

public sealed class NtpServerService : BackgroundService
{
    private readonly ILogger<NtpServerService> _logger;
    private readonly INtpRequestHandler _handler;
    private readonly NtpServerOptions _options;
    private readonly INtpMetrics _metrics;

    private UdpClient? _udp;

    public NtpServerService(
        ILogger<NtpServerService> logger,
        INtpRequestHandler handler,
        NtpServerOptions options,
        INtpMetrics metrics)
    {
        _logger = logger;
        _handler = handler;
        _options = options;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _udp = new UdpClient(AddressFamily.InterNetwork);
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.Client.ReceiveBufferSize = _options.BufferSize;
        _udp.Client.SendBufferSize = _options.BufferSize;

        var ep = new IPEndPoint(_options.ListenAddress, _options.Port);
        _udp.Client.Bind(ep);

        _logger.LogInformation("NTP server listening on {Endpoint}", ep);

        while (!stoppingToken.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await _udp.ReceiveAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(result, stoppingToken), stoppingToken);
        }
    }

    private async Task HandleRequestAsync(UdpReceiveResult result, CancellationToken ct)
    {
        try
        {
            var response = await _handler.HandleAsync(result, _udp!, ct).ConfigureAwait(false);

            _metrics.Sync(new NtpSyncEvent(
                Timestamp: DateTime.UtcNow,
                ClientIp: result.RemoteEndPoint.Address,
                ClientName: null,
                Offset: response.Offset,
                Success: response.Success));

            if (response.Bytes is not null)
            {
                await _udp!.SendAsync(
                    response.Bytes,
                    response.Bytes.Length,
                    result.RemoteEndPoint).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing NTP request from {Remote}",
                result.RemoteEndPoint);

            _metrics.Sync(new NtpSyncEvent(
                Timestamp: DateTime.UtcNow,
                ClientIp: result.RemoteEndPoint.Address,
                ClientName: null,
                Offset: TimeSpan.Zero,
                Success: false));
        }
    }

    public override void Dispose()
    {
        try
        {
            _udp?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing UDP listener");
        }

        base.Dispose();
    }
}
