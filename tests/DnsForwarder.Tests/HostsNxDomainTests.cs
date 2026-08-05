using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DnsForwarder.Tests;

public sealed class HostsNxDomainTests
{
    [Fact]
    public void NonexistentDomain_Should_Return_NXDOMAIN()
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

        var logger = NullLogger<DnsForwarder.RuleEngine.RuleEngine>.Instance;
        var engine = new DnsForwarder.RuleEngine.RuleEngine(options, logger);

        var result = engine.Match("nonexistent.test", "-");

        Assert.NotEmpty(result.Upstreams);
        Assert.Equal("default", result.Upstreams[0].Name);
    }
}
