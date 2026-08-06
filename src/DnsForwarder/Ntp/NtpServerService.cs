using System.Net;
using System.Net.Sockets;

using DnsForwarder;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsForwarder.Ntp;

public sealed class NtpServerService : BackgroundService
{
    private readonly ILogger<NtpServerService> _logger;
    private readonly INtpRequestHandler _handler;
    private readonly NtpServerOptions _options;
    private UdpClient? _udp;

    public NtpServerService(
        ILogger<NtpServerService> logger,
        INtpRequestHandler handler,
        IOptions<NtpServerOptions> options)
    {
        _logger = logger;
        _handler = handler;
        _options = options.Value;
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
                result = await _udp.ReceiveAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            _ = Task.Run(() => _handler.HandleAsync(result, _udp, stoppingToken), stoppingToken);
        }
    }

    public override void Dispose()
    {
        _udp?.Dispose();
        base.Dispose();
    }
}

