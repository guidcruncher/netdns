using System.Net;

using DnsForwarder;
using DnsForwarder.Dhcp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder.Dhcp.Bootstrap;

public static class DhcpServiceCollectionExtensions
{
    public static IServiceCollection AddDhcpServer(this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var dhcp = server.Dhcp;

        if (!dhcp.Enabled)
            return services; // DHCP disabled — do nothing


        // DHCP engine + lease store
        services.AddSingleton<DhcpOptions>(dhcp);

        services.AddSingleton<IDhcpLeaseStore>(sp =>
            new JsonDhcpLeaseStore(dhcp.LeaseStorePath));

        services.AddSingleton<IUdpTransport>(sp =>
        {
            var opts = sp.GetRequiredService<DhcpOptions>();
            return new UdpTransport(
                IPAddress.Parse(opts.ListenAddress),
                opts.ListenPort);
        });

        services.AddSingleton<IDhcpLeaseStore, InMemoryDhcpLeaseStore>();
        services.AddSingleton<DhcpLeaseEngine>();
        services.AddSingleton<DhcpServerEngine>();

        // Hosted DHCP server
        services.AddHostedService<DhcpHostedService>();

        return services;
    }
}
