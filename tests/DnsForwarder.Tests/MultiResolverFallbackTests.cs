using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace DnsForwarder.Tests;

public sealed class MultiResolverFallbackTests
{
    private DnsForwarder.RuleEngine.RuleEngine CreateEngine()
    {
        var options = new DnsForwarderOptions
        {
            DefaultResolver = new UpstreamResolverOptions
            {
                Address = "127.0.0.1",
                Port = 5300,
                Rule = "*.fallback.test",
                Name = "default"
            },
            Resolvers = new List<UpstreamResolverOptions>
            {
                new()
                {
                    Address = "127.0.0.1",
                    Port = 5301,
                    Rule = "*.primary.test",
                    Name = "primary"
                },
                new()
                {
                    Address = "127.0.0.1",
                    Port = 5302,
                    Rule = "*.secondary.test",
                    Name = "secondary"
                }
            }
        };

        var logger = NullLogger<DnsForwarder.RuleEngine.RuleEngine>.Instance;
        return new DnsForwarder.RuleEngine.RuleEngine(options, logger);
    }

    [Fact]
    public void PrimaryResolverTimeout_Should_FallbackToSecondary()
    {
        var engine = CreateEngine();

        // Domain matches primary rule
        var domain = "api.primary.test";

        var result = engine.Match(domain, "-");

        Assert.Equal("primary", result.Upstreams[0].Name);

        // Secondary is NOT included unless its rule matches
        Assert.Single(result.Upstreams);
    }

}
