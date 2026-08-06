using System.Net;
using System.Net.Sockets;

namespace DnsForwarder.Dhcp;

public sealed class UdpTransport : IUdpTransport, IDisposable
{
    private readonly UdpClient _udp;

    public UdpTransport(IPAddress address, int port)
    {
        _udp = new UdpClient(new IPEndPoint(address, port));
    }

    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken ct)
    {
        return await _udp.ReceiveAsync(ct);
    }

    public async Task SendAsync(byte[] buffer, int length, IPEndPoint endpoint)
    {
        await _udp.SendAsync(buffer, length, endpoint);
    }

    public void Dispose()
    {
        _udp.Dispose();
    }
}
