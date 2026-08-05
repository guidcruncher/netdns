using System.Net;
using System.Net.Sockets;

namespace DnsForwarder;

public sealed class UdpDnsClient : IDnsClient
{
    private readonly IPEndPoint _endpoint;

    public UdpDnsClient(IPEndPoint endpoint)
    {
        _endpoint = endpoint;
    }

    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Connect(_endpoint);

        await udp.SendAsync(request, request.Length);

        var result = await udp.ReceiveAsync(ct);
        return result.Buffer;
    }
}
