using DnsForwarder.Dns.Core;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DnsForwarder.Dns.Tests;

public sealed class HostsTimeoutTests
{
    [Fact]
    public void UpstreamTimeout_Should_Throw_TaskCanceledException()
    {
        var options = new DnsForwarderOptions
        {
            DefaultResolver = new UpstreamResolverOptions
            {
                Address = "127.0.0.1",
                Port = 5300,
                Rule = "*.test",
                Name = "default"
            }
        };


        var logger = NullLogger<DnsForwarder.Dns.RuleEngine.RuleEngine>.Instance;
        var engine = new DnsForwarder.Dns.RuleEngine.RuleEngine(options, logger);

        var result = engine.Match("nonexistent.test", "-");

        Assert.NotEmpty(result.Upstreams);
        Assert.Equal("default", result.Upstreams[0].Name);
    }
}
