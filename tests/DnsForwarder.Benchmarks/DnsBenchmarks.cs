using System.Net;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using DnsForwarder.Dns.Core;
using DnsForwarder.Dns.RuleEngine;

using Microsoft.Extensions.Logging.Abstractions;

namespace DnsForwarder.Dns.Benchmarks;

public class DnsBenchmarks
{
    private byte[] _query = Array.Empty<byte>();
    private DnsForwarder.Dns.RuleEngine.RuleEngine _engine = default!;
    private IDnsClient _client = default!;
    private CachingDnsClientDecorator _cache = default!;

    [GlobalSetup]
    public void Setup()
    {
        _query = new byte[]
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

        var options = new DnsForwarderOptions
        {
            Resolvers =
            {
                new UpstreamResolverOptions
                {
                    Name = "Internal",
                    Rule = "^(.+\\.corp\\.local)$",
                    Address = "10.0.0.10",
                    Port = 53,
                    Block = false
                },
                new UpstreamResolverOptions
                {
                    Name = "BlockAds",
                    Rule = "^(ads|tracking)\\.",
                    Block = true
                }
            },
            DefaultResolver = new UpstreamResolverOptions
            {
                Name = "Cloudflare",
                Address = "1.1.1.1",
                Port = 53,
                Block = false
            },
            Caching = new CachingOptions
            {
                Enabled = true,
                MaxEntries = 10000
            }
        };

        var logger = NullLogger<DnsForwarder.Dns.RuleEngine.RuleEngine>.Instance;
        _engine = new DnsForwarder.Dns.RuleEngine.RuleEngine(options, logger);

        _client = new UdpDnsClient(new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53));
        _cache = new CachingDnsClientDecorator(_client, options.Caching.MaxEntries);

        // Warm cache
        _cache.QueryAsync(_query, default).GetAwaiter().GetResult();
    }

    [Benchmark]
    public void Parse_Dns_Query()
    {
        var msg = DnsParser.Parse(_query);
        _ = msg.Questions.Count;
    }

    [Benchmark]
    public void RuleEngine_Match_Default()
    {
        var result = _engine.Match("example.com", "-");
        _ = result.Upstreams;
    }

    [Benchmark]
    public void RuleEngine_Match_Block()
    {
        var result = _engine.Match("ads.example.com", "--");
        _ = result.Block;
    }

    [Benchmark]
    public void Cache_Hit()
    {
        var response = _cache.QueryAsync(_query, default).GetAwaiter().GetResult();
        _ = response.Length;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<DnsBenchmarks>();
    }
}
