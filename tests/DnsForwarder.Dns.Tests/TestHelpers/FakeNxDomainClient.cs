using DnsForwarder.Dns;
using DnsForwarder.Dns.Core;

namespace DnsForwarder.Dns.Tests;

public sealed class FakeNxDomainClient : IDnsClient
{
    public Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        var response = new List<byte>();

        // Copy transaction ID
        response.Add(request[0]);
        response.Add(request[1]);

        // Flags: QR=1, RD=1, RA=1, RCODE=3 (NXDOMAIN)
        response.Add(0x81);
        response.Add(0x83);

        // QDCOUNT = 1
        response.Add(0x00);
        response.Add(0x01);

        // ANCOUNT = 0
        response.Add(0x00);
        response.Add(0x00);

        // NSCOUNT = 0
        response.Add(0x00);
        response.Add(0x00);

        // ARCOUNT = 0
        response.Add(0x00);
        response.Add(0x00);

        // Copy question section
        response.AddRange(request.Skip(12));

        return Task.FromResult(response.ToArray());
    }
}
