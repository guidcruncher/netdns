using System.Net;
using System.Net.Sockets;

namespace DnsForwarder.Dhcp.Tests;

public sealed class RealDhcpClient
{
    private readonly UdpClient _udp;

    /// <summary>
    /// Bind the client to a specific local port.
    /// Your tests call: new RealDhcpClient(6868)
    /// </summary>
    public RealDhcpClient(int localPort)
    {
        // Bind to the exact port the test expects to receive unicast replies on
        _udp = new UdpClient(new IPEndPoint(IPAddress.Loopback, localPort));
    }

    /// <summary>
    /// Send a DHCP packet to the server (always port 6767 in tests).
    /// </summary>
    public Task SendAsync(byte[] packet)
    {
        return _udp.SendAsync(packet, packet.Length,
            new IPEndPoint(IPAddress.Loopback, 6767));
    }

    /// <summary>
    /// Receive a DHCP packet. UdpClient.ReceiveAsync returns ValueTask.
    /// Convert to Task to match test signatures.
    /// </summary>
    public Task<UdpReceiveResult> ReceiveAsync(CancellationToken ct)
    {
        return _udp.ReceiveAsync(ct).AsTask();
    }
}
