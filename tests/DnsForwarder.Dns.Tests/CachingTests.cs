using DnsForwarder.Dns;
using DnsForwarder.Dns.Core;

using Xunit;

namespace DnsForwarder.Dns.Tests;

public class CachingTests
{
    private sealed class FakeDnsClient : IDnsClient
    {
        public int Calls = 0;
        public byte[] Response;

        public FakeDnsClient(byte[] response)
        {
            Response = response;
        }

        public Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Response);
        }
    }

    [Fact]
    public async Task Cache_Returns_Cached_Response()
    {
        // Fake DNS response with TTL = 60
        byte[] response =
        {
            0x12, 0x34, 0x81, 0x80, // header
            0x00, 0x01, 0x00, 0x01, // QD=1 AN=1
            0x00, 0x00, 0x00, 0x00, // NS AR

            // Question: example.com
            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,
            0x00, 0x01,
            0x00, 0x01,

            // Answer: TTL = 60
            0xC0, 0x0C,             // pointer to name
            0x00, 0x01,             // type A
            0x00, 0x01,             // class IN
            0x00, 0x00, 0x00, 0x3C, // TTL = 60
            0x00, 0x04,             // RDLENGTH
            0x7F, 0x00, 0x00, 0x01  // 127.0.0.1
        };

        var fake = new FakeDnsClient(response);
        var cache = new CachingDnsClientDecorator(fake, 100);

        // Minimal query for example.com
        byte[] query =
        {
            0x12, 0x34, 0x01, 0x00,
            0x00, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,

            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,
            0x00, 0x01,
            0x00, 0x01
        };

        await cache.QueryAsync(query, default);
        await cache.QueryAsync(query, default);

        Assert.Equal(1, fake.Calls); // second call was cached
    }
}
