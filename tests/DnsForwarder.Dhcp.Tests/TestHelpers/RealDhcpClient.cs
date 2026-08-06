using System.Net;
using System.Net.Sockets;

namespace DnsForwarder.Dhcp.Tests.Integration;

public sealed class RealDhcpClient
{
    private readonly UdpClient _udp;

    public RealDhcpClient(int port)
    {
        _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
    }

    public async Task SendAsync(byte[] packet)
    {
        await _udp.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Loopback, 6767));
    }

    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken ct)
    {
        return await _udp.ReceiveAsync(ct);
    }
}
