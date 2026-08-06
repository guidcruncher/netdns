using System.Net;

using DnsForwarder.Filtering;
using DnsForwarder.RuleEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDnsForwarder(this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var options = server.Dns;

        //
        // Command-line overrides
        //
        if (config["ListenOverride"] is string listen)
        {
            var parts = listen.Split(':');
            options.Listen.Address = parts[0];
            options.Listen.Port = int.Parse(parts[1]);
        }

        if (config["ResolverOverride"] is string resolver)
        {
            var parts = resolver.Split(':');
            options.DefaultResolver.Address = parts[0];
            options.DefaultResolver.Port = parts.Length > 1 ? int.Parse(parts[1]) : 53;
        }

        //
        // Core options
        //
        services.AddSingleton(options);

        //
        // DNS client + caching
        //
        services.AddSingleton<StaticDnsClient>();

        services.AddSingleton<IDnsClient>(sp =>
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

        //
        // Rule engine + hosts loader
        //
        services.AddSingleton<RuleEngine.RuleEngine>();
        services.AddSingleton<HostsFileSource>();

        //
        // Forwarder + server
        //
        services.AddSingleton<DnsForwarderService>();
        services.AddHostedService<DnsServer>();

        return services;
    }
}

