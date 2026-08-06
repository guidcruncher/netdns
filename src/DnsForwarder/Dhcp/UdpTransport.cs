using System.Net;
using System.Net.Sockets;

namespace DnsForwarder.Dhcp;

public sealed class UdpTransport : IUdpTransport, IDisposable
{
    private readonly UdpClient _client;

    public UdpTransport(IPAddress address, int port)
    {
        _client = new UdpClient(new IPEndPoint(address, port));
    }


    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken ct)
    {
        return await _client.ReceiveAsync(ct);
    }

    public Task SendAsync(byte[] buffer, int length, IPEndPoint endpoint)
        => _client.SendAsync(buffer, length, endpoint);

    public void Dispose()
    {
        _client.Dispose();
    }
}
