using System.Net;
using System.Text.RegularExpressions;

using DnsForwarder.Dns;
using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.Filtering;
using DnsForwarder.Dns.RuleEngine;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DnsForwarder.Dns.Tests;

public class HostsTests
{
    private RuleEngine.RuleEngine CreateEngine()
    {
        var opts = new DnsForwarderOptions
        {
            // UPDATED: DefaultResolvers replaces DefaultResolver
            DefaultResolvers =
            {
                new UpstreamResolverOptions
                {
                    Name = "default",
                    Address = "1.1.1.1",
                    Port = 53,
                    Rule = null,
                    Block = false
                }
            },

            Resolvers = new List<UpstreamResolverOptions>()
        };

        var logger = NullLogger<DnsForwarder.Dns.RuleEngine.RuleEngine>.Instance;
        return new RuleEngine.RuleEngine(opts, logger);
    }

    [Fact]
    public async Task HostsFile_Should_Load_Entries()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "127.0.0.1 localhost",
            "192.168.1.10 nas.local # inline comment",
            "# full line comment",
            "  "
        });

        var engine = CreateEngine();
        var source = new HostsFileSource(new[] { tmp });

        await engine.AddHostsAsync(source);

        var result1 = engine.Match("localhost", "-");
        var result2 = engine.Match("nas.local", "--");

        Assert.False(result1.Block);
        Assert.False(result2.Block);

        Assert.IsType<StaticDnsClient>(result1.Upstreams[0].Client);
        Assert.IsType<StaticDnsClient>(result2.Upstreams[0].Client);
    }

    [Fact]
    public async Task HostsEntry_Should_Override_DefaultResolver()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.5 internal.local"
        });

        var engine = CreateEngine();
        var source = new HostsFileSource(new[] { tmp });

        await engine.AddHostsAsync(source);

        var result = engine.Match("internal.local", "---");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_Suffix_Should_Match()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.1 *.example.com"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(new HostsFileSource(new[] { tmp }));

        var result = engine.Match("foo.example.com", "suffix");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_Prefix_Should_Match()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.2 example.*"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(new HostsFileSource(new[] { tmp }));

        var result = engine.Match("example.domain", "prefix");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_Substring_Should_Match()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.3 *ads*"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(new HostsFileSource(new[] { tmp }));

        var result = engine.Match("superadsdomain.com", "substring");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_LongestCoreWins()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.10 *.example.com",
            "10.0.0.20 *ample.com",
            "10.0.0.30 *ple.com"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(new HostsFileSource(new[] { tmp }));

        // foo.example.com matches all three, but longest core = "example.com"
        var result = engine.Match("foo.example.com", "specificity");

        var client = Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);

        // Extract IP from StaticDnsClient via reflection
        var ipField = typeof(StaticDnsClient).GetField("_ip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ip = (IPAddress)ipField!.GetValue(client)!;

        Assert.Equal(IPAddress.Parse("10.0.0.10"), ip);
    }
}
