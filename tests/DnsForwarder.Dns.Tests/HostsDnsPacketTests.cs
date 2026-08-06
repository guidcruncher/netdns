using System.Net;

using DnsForwarder.Dns;
using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Dns.RuleEngine;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DnsForwarder.Dns.Tests;

public class HostsDnsPacketTests
{
    private RuleEngine.RuleEngine CreateEngine(string ip)
    {
        var opts = new DnsForwarderOptions
        {
            DefaultResolver = new UpstreamResolverOptions
            {
                Name = "default",
                Address = "1.1.1.1",
                Port = 53
            }
        };

        var logger = NullLogger<DnsForwarder.Dns.RuleEngine.RuleEngine>.Instance;
        var engine = new RuleEngine.RuleEngine(opts, logger);

        // Inject hosts entry directly
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, $"{ip} test.local");

        var source = new HostsFileSource(new[] { tmp });
        engine.AddHostsAsync(source).Wait();

        return engine;
    }

    private static byte[] BuildDnsQuery(string domain)
    {
        var parts = domain.Split('.');
        var bytes = new List<byte>();

        bytes.Add(0x12);
        bytes.Add(0x34);

        bytes.Add(0x01);
        bytes.Add(0x00);

        bytes.Add(0x00);
        bytes.Add(0x01);

        bytes.AddRange(new byte[] { 0, 0, 0, 0, 0, 0 });

        foreach (var p in parts)
        {
            bytes.Add((byte)p.Length);
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(p));
        }
        bytes.Add(0x00);

        bytes.Add(0x00);
        bytes.Add(0x01);

        bytes.Add(0x00);
        bytes.Add(0x01);

        return bytes.ToArray();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    public async Task HostsOverride_Should_Return_Valid_A_Record(string ip)
    {
        var engine = CreateEngine(ip);

        var result = engine.Match("test.local", "-");

        Assert.False(result.Block);

        // NEW API: Upstreams[0] is the active resolver
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);

        var query = BuildDnsQuery("test.local");
        var response = await result.Upstreams[0].Client.QueryAsync(query, default);

        int offset = 0;

        ushort id = (ushort)((response[0] << 8) | response[1]);
        Assert.Equal(0x1234, id);

        Assert.Equal(0x81, response[2]);
        Assert.Equal(0x80, response[3]);

        Assert.Equal(0x00, response[4]);
        Assert.Equal(0x01, response[5]);

        Assert.Equal(0x00, response[6]);
        Assert.Equal(0x01, response[7]);

        offset = 12;

        while (response[offset] != 0)
            offset += response[offset] + 1;

        offset++;
        offset += 4;

        Assert.Equal(0xC0, response[offset]);
        Assert.Equal(0x0C, response[offset + 1]);
        offset += 2;

        Assert.Equal(0x00, response[offset]);
        Assert.Equal(0x01, response[offset + 1]);
        offset += 2;

        Assert.Equal(0x00, response[offset]);
        Assert.Equal(0x01, response[offset + 1]);
        offset += 2;

        offset += 4;

        Assert.Equal(0x00, response[offset]);
        Assert.Equal(0x04, response[offset + 1]);
        offset += 2;

        var ipBytes = IPAddress.Parse(ip).GetAddressBytes();
        Assert.Equal(ipBytes[0], response[offset]);
        Assert.Equal(ipBytes[1], response[offset + 1]);
        Assert.Equal(ipBytes[2], response[offset + 2]);
        Assert.Equal(ipBytes[3], response[offset + 3]);
    }
}
