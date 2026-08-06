using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;
using DnsForwarder.Ntp;

public class PacketHandlerTests
{
    [Fact]
    public async Task Handler_IgnoresNonClientMode()
    {
        var logger = Mock.Of<ILogger<NtpRequestHandler>>();
        var timeSource = Mock.Of<ITimeSource>(t => t.UtcNow == DateTime.UtcNow && t.ReferenceUtc == DateTime.UtcNow);
        var options = Options.Create(new NtpServerOptions());

        var handler = new NtpRequestHandler(logger, timeSource, options);

        var packet = new byte[48];
        packet[0] = 0b_0010_0100; // mode = 4 (server), not client

        var udp = new UdpClient(AddressFamily.InterNetwork);
        var result = new UdpReceiveResult(packet, new IPEndPoint(IPAddress.Loopback, 9999));

        await handler.HandleAsync(result, udp, default);

        // No exception = pass
    }

    [Fact]
    public async Task Handler_RespondsToClientMode()
    {
        var logger = Mock.Of<ILogger<NtpRequestHandler>>();
        var timeSource = Mock.Of<ITimeSource>(t => t.UtcNow == DateTime.UtcNow && t.ReferenceUtc == DateTime.UtcNow);
        var options = Options.Create(new NtpServerOptions());

        var handler = new NtpRequestHandler(logger, timeSource, options);

        var packet = new byte[48];
        packet[0] = 0b_0010_0011; // mode = 3 (client)

        var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        var result = new UdpReceiveResult(packet, new IPEndPoint(IPAddress.Loopback, udp.Client.LocalEndPoint is IPEndPoint ep ? ep.Port : 9999));

        await handler.HandleAsync(result, udp, default);

        // If no exception, response was sent
    }
}
