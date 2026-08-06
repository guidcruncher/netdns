using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace DnsForwarder.Dhcp.Tests;

public sealed class FakeUdpClient
{
    private readonly Channel<byte[]> _incoming = Channel.CreateUnbounded<byte[]>();
    public List<byte[]> SentPackets { get; } = new();

    public CancellationToken CancellationToken => new CancellationTokenSource().Token;

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
}

