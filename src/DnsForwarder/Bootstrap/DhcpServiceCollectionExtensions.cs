using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DnsForwarder;
using DnsForwarder.Dhcp;

namespace DnsForwarder.Bootstrap;

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

        services.AddSingleton<DhcpServerEngine>();

        // Hosted DHCP server
        services.AddHostedService<DhcpHostedService>();

        return services;
    }
}
