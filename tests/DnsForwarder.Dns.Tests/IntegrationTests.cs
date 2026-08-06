using System.Net;

using DnsForwarder.Dns;
using DnsForwarder.Dns.Core;

using Xunit;

namespace DnsForwarder.Dns.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task Real_Dns_Query_Works()
    {
        var client = new UdpDnsClient(new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53));

        // Query: example.com A
        byte[] query =
        {
            0x12, 0x34,             // ID
            0x01, 0x00,             // Flags
            0x00, 0x01,             // QDCOUNT
            0x00, 0x00,             // ANCOUNT
            0x00, 0x00,             // NSCOUNT
            0x00, 0x00,             // ARCOUNT

            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,
            0x00, 0x01,
            0x00, 0x01
        };

        var response = await client.QueryAsync(query, default);

        Assert.NotNull(response);
        Assert.True(response.Length > 12);

        var msg = DnsParser.Parse(response);

        Assert.True(msg.IsResponse);
        Assert.Single(msg.Questions);
        Assert.True(msg.Answers.Count > 0);
    }
}
