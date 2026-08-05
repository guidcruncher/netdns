using System.Net;
using DnsForwarder.Filtering;
using DnsForwarder.RuleEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsForwarder;

public sealed class DnsForwarderBootstrap
{
    private readonly IConfiguration _config;
    private readonly IServiceCollection _services;

    public DnsForwarderBootstrap(IConfiguration config, IServiceCollection services)
    {
        _config = config;
        _services = services;
    }

    public void ConfigureServices()
    {
        var server = _config.Get<ServerOptions>() ?? new ServerOptions();
        var options = server.Dns;

        // Override listen address via --listen
        if (_config["ListenOverride"] is string listen)
        {
            var parts = listen.Split(':');
            options.Listen.Address = parts[0];
            options.Listen.Port = int.Parse(parts[1]);
        }

        // Override default resolver via --resolver
        if (_config["ResolverOverride"] is string resolver)
        {
            var parts = resolver.Split(':');
            options.DefaultResolver.Address = parts[0];
            options.DefaultResolver.Port = parts.Length > 1 ? int.Parse(parts[1]) : 53;
        }

        _services.AddSingleton(options);

        // DNS client + caching
        _services.AddSingleton<StaticDnsClient>();

        _services.AddSingleton<IDnsClient>(sp =>
        {
            var opt = sp.GetRequiredService<DnsForwarderOptions>();
            var endpoint = new IPEndPoint(
                IPAddress.Parse(opt.DefaultResolver.Address),
                opt.DefaultResolver.Port);

            IDnsClient client = new UdpDnsClient(endpoint);

            if (opt.Caching.Enabled)
                client = new CachingDnsClientDecorator(client, opt.Caching.MaxEntries);

            return client;
        });

        // Rule engine
        _services.AddSingleton<RuleEngine.RuleEngine>();

        // Hosts loader
        _services.AddSingleton<HostsFileSource>();

        // DNS forwarder service + server
        _services.AddSingleton<DnsForwarderService>();
        _services.AddHostedService<DnsServer>();
    }

    public async Task LoadDataAsync(IServiceProvider services)
    {
        var options = services.GetRequiredService<DnsForwarderOptions>();
        var engine = services.GetRequiredService<RuleEngine.RuleEngine>();

        //
        // HOSTS FILES
        //
        if (options.HostsFiles?.Any() == true)
        {
            var hostsPaths = options.HostsFiles
                .Select(p => p.StartsWith("file://") ? p[7..] : p);

            var hostsSource = new HostsFileSource(hostsPaths);
            await engine.AddHostsAsync(hostsSource);
        }

        //
        // Helper: choose correct loader based on prefix
        //
        IBlocklistSource CreateSource(IEnumerable<string> items)
        {
            var fileItems = items
                .Where(i => i.StartsWith("file://"))
                .Select(i => i.Replace("file://", ""));

            var urlItems = items
                .Where(i => !i.StartsWith("file://"));

            if (fileItems.Any() && urlItems.Any())
            {
                return new CompositeBlocklistSource(new IBlocklistSource[]
                {
                    new FileBlocklistSource(fileItems),
                    new UrlBlocklistSource(urlItems)
                });
            }

            if (fileItems.Any())
                return new FileBlocklistSource(fileItems);

            return new UrlBlocklistSource(urlItems);
        }

        //
        // BLOCKLISTS
        //
        if (options.Blocklists?.Any() == true)
        {
            var source = CreateSource(options.Blocklists);
            await engine.AddListAsync(source, block: true);
        }

        //
        // ALLOWLISTS
        //
        if (options.Allowlists?.Any() == true)
        {
            var source = CreateSource(options.Allowlists);
            await engine.AddListAsync(source, block: false);
        }
    }
}
