using System.Net;

using DnsForwarder.Dns.Core;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DnsForwarder.Dns.Tests;

public sealed class SequentialFallbackTests
{
    private DnsForwarder.Dns.RuleEngine.RuleEngine CreateEngine()
    {
        var options = new DnsForwarderOptions
        {
            DefaultResolvers =
            {
                new UpstreamResolverOptions
                {
                    Name = "primary",
                    Address = "127.0.0.1",
                    Port = 5301
                },
                new UpstreamResolverOptions
                {
                    Name = "secondary",
                    Address = "127.0.0.1",
                    Port = 5302
                },
                new UpstreamResolverOptions
                {
                    Name = "tertiary",
                    Address = "127.0.0.1",
                    Port = 5303
                }
            }
        };

        var logger = NullLogger<DnsForwarder.Dns.RuleEngine.RuleEngine>.Instance;
        return new DnsForwarder.Dns.RuleEngine.RuleEngine(options, logger);
    }

    private static byte[] BuildQuery(string domain)
    {
        var parts = domain.Split('.');
        var q = new List<byte> { 0x12, 0x34, 0x01, 0x00, 0x00, 0x01, 0, 0, 0, 0, 0, 0 };

        foreach (var p in parts)
        {
            q.Add((byte)p.Length);
            q.AddRange(System.Text.Encoding.ASCII.GetBytes(p));
        }

        q.Add(0);
        q.AddRange(new byte[] { 0x00, 0x01, 0x00, 0x01 });
        return q.ToArray();
    }

    [Fact]
    public void Match_Should_Return_Primary_DefaultResolver()
    {
        var engine = CreateEngine();

        var result = engine.Match("anything.test", "-");

        // Match() returns the FIRST default resolver
        Assert.Single(result.Upstreams);
        Assert.Equal("default", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task QueryAsync_Should_Fallback_Through_All_DefaultResolvers()
    {
        var engine = CreateEngine();
        var query = BuildQuery("anything.test");

        var response = await engine.QueryAsync("anything.test", query, "id", CancellationToken.None);

        // All resolvers fail → SERVFAIL
        Assert.Equal(2, response[3] & 0x0F);
    }
}
