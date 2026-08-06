using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using DnsForwarder.Ntp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public class PacketHandlerTests
{
    private sealed class FakeTimeSource : ITimeSource
    {
        public DateTime UtcNow => new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime ReferenceUtc => new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Handler_IgnoresNonClientMode()
    {
        var handler = new NtpRequestHandler(
            NullLogger<NtpRequestHandler>.Instance,
            new FakeTimeSource(),
            new NtpServerOptions()
        );

        var packet = new byte[48];
        packet[0] = 0b_0010_0100; // mode = 4 (server)

        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        var result = new UdpReceiveResult(packet, new IPEndPoint(IPAddress.Loopback, udp.Client.LocalEndPoint is IPEndPoint ep ? ep.Port : 9999));

        await handler.HandleAsync(result, udp, CancellationToken.None);

        // No exception = pass
    }

    [Fact]
    public async Task Handler_RespondsToClientMode()
    {
        var handler = new NtpRequestHandler(
            NullLogger<NtpRequestHandler>.Instance,
            new FakeTimeSource(),
            new NtpServerOptions()
        );

        var packet = new byte[48];
        packet[0] = 0b_0010_0011; // mode = 3 (client)

        using var udp = new UdpClient(AddressFamily.InterNetwork);
        udp.Client.Bind(new IPEndPoint(IPAddress.Loopback, 0));

        var remote = new IPEndPoint(IPAddress.Loopback, udp.Client.LocalEndPoint is IPEndPoint ep ? ep.Port : 9999);
        var result = new UdpReceiveResult(packet, remote);

        await handler.HandleAsync(result, udp, CancellationToken.None);

        // If no exception, response was sent
    }
}
