using DnsForwarder.Dhcp.Bootstrap;
using DnsForwarder.Dns.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Hosting;

public static class RuntimeLoader
{
    public static async Task LoadAsync(IHost host)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Starting runtime loader…");

        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider;

        logger.LogInformation("Loading DNS runtime…");
        var dnsLoader = new DnsForwarderRuntimeLoader(
            provider.GetRequiredService<IConfiguration>());
        await dnsLoader.LoadAsync(provider);
        logger.LogInformation("DNS runtime loaded.");

        logger.LogInformation("Loading DHCP runtime…");
        var dhcpLoader = new DhcpRuntimeLoader(
            provider.GetRequiredService<IConfiguration>());
        await dhcpLoader.LoadAsync(provider);
        logger.LogInformation("DHCP runtime loaded.");

        logger.LogInformation("Runtime loader completed.");
    }
}
