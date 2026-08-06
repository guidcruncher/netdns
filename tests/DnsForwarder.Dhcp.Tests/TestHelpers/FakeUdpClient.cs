using System.Threading.Channels;
using System.Net;
using System.Net.Sockets;

using DnsForwarder.Dhcp;


namespace DnsForwarder.Dhcp.Tests;

public sealed class FakeUdpClient : IUdpTransport
{
    private readonly Channel<byte[]> _incoming = Channel.CreateUnbounded<byte[]>();
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken CancellationToken => _cts.Token;

    public void CancelAfter(int ms) => _cts.CancelAfter(ms);

    public async Task InjectReceive(byte[] packet)
    {
        await _incoming.Writer.WriteAsync(packet);
    }

    public async Task<UdpReceiveResult> ReceiveAsync(CancellationToken ct)
    {
        var data = await _incoming.Reader.ReadAsync(ct);
        return new UdpReceiveResult(data, new IPEndPoint(IPAddress.Loopback, 68));
    }

    public Task SendAsync(byte[] buffer, int length, IPEndPoint endpoint)
    {
        SentPackets.Add(buffer.Take(length).ToArray());
        return Task.CompletedTask;
    }

    public List<byte[]> SentPackets { get; } = new();
}
