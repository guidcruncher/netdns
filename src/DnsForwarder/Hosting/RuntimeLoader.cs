using DnsForwarder.Dhcp.Bootstrap;
using DnsForwarder.Dns.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DnsForwarder.Hosting;

public static class RuntimeLoader
{
    public static async Task LoadAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider;

        var dnsLoader = new DnsForwarderRuntimeLoader(
            provider.GetRequiredService<IConfiguration>());
        await dnsLoader.LoadAsync(provider);

        var dhcpLoader = new DhcpRuntimeLoader(
            provider.GetRequiredService<IConfiguration>());
        await dhcpLoader.LoadAsync(provider);
    }
}
