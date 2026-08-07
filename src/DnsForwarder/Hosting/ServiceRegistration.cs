using DnsForwarder.Dhcp.Bootstrap;
using DnsForwarder.Dns.Bootstrap;
using DnsForwarder.Events.Bootstrap;
using DnsForwarder.Metrics.Bootstrap;
using DnsForwarder.Ntp.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder.Hosting;

public static class ServiceRegistration
{
    public static void Register(HostBuilderContext ctx, IServiceCollection services)
    {
        var serverOptions = ctx.Configuration
            .GetSection("Server").Get<ServerOptions>() ?? new ServerOptions();

        services.AddSingleton(serverOptions);

        services.AddEventBus(ctx.Configuration);
        services.AddDnsForwarder(ctx.Configuration);
        services.AddDhcpServer(ctx.Configuration);
        services.AddNtpServer(ctx.Configuration);

        services.AddMetricServices(ctx.Configuration);
    }
}
