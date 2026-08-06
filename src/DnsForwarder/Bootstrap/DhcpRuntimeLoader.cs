using DnsForwarder.Dhcp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder.Bootstrap;

public sealed class DhcpRuntimeLoader
{
    private readonly IConfiguration _config;

    public DhcpRuntimeLoader(IConfiguration config)
    {
        _config = config;
    }

    public async Task LoadAsync(IServiceProvider services)
    {
        var server = _config.Get<ServerOptions>() ?? new ServerOptions();
        var dhcp = server.Dhcp;

        if (!dhcp.Enabled)
            return;

        // Example: load static leases from file
        var store = services.GetRequiredService<IDhcpLeaseStore>();

        if (store is JsonDhcpLeaseStore json && File.Exists(dhcp.LeaseStorePath))
        {
            await json.LoadAsync();
        }
    }
}
