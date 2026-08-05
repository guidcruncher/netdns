using System.Net;

using DnsForwarder;

namespace DnsForwarder.Tests;

public sealed class FakeSuccessClient : IDnsClient
{
    private readonly IPAddress _ip;

    public FakeSuccessClient(IPAddress ip)
    {
        _ip = ip;
    }

    public Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        var response = new List<byte>();

        response.Add(request[0]);
        response.Add(request[1]);

        response.Add(0x81);
        response.Add(0x80);

        response.Add(0x00);
        response.Add(0x01);

        response.Add(0x00);
        response.Add(0x01);

        response.Add(0x00);
        response.Add(0x00);

        response.Add(0x00);
        response.Add(0x00);

        response.AddRange(request.Skip(12));

        response.Add(0xC0);
        response.Add(0x0C);

        response.Add(0x00);
        response.Add(0x01);

        response.Add(0x00);
        response.Add(0x01);

        response.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x3C });

        response.Add(0x00);
        response.Add(0x04);

        response.AddRange(_ip.GetAddressBytes());

        return Task.FromResult(response.ToArray());
    }
}
