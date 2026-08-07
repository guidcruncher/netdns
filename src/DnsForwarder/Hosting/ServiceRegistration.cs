using DnsForwarder.Dhcp.Bootstrap;
using DnsForwarder.Dns.Bootstrap;
using DnsForwarder.Events.Bootstrap;
using DnsForwarder.Metrics.Bootstrap;
using DnsForwarder.Ntp.Bootstrap;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Hosting;

public static class ServiceRegistration
{
    public static void Register(HostBuilderContext ctx, IServiceCollection services)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });

        var logger = loggerFactory.CreateLogger("ServiceRegistration");

        logger.LogInformation("Loading ServerOptions…");

        var serverOptions = new ServerOptions();
        ctx.Configuration.Bind(serverOptions);
        services.AddSingleton<ServerOptions>(serverOptions);


        logger.LogInformation("Registering EventBus…");
        services.AddEventBus(ctx.Configuration);

        logger.LogInformation("Registering DNS Forwarder…");
        services.AddDnsForwarder(ctx.Configuration);

        logger.LogInformation("Registering DHCP Server…");
        services.AddDhcpServer(ctx.Configuration);

        logger.LogInformation("Registering NTP Server…");
        services.AddNtpServer(ctx.Configuration);

        logger.LogInformation("Registering Metrics services…");
        services.AddMetricServices(ctx.Configuration);

        logger.LogInformation("All services registered successfully.");
    }
}
