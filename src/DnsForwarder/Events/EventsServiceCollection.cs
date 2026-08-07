using DnsForwarder;
using DnsForwarder.Dhcp;
using DnsForwarder.Dns;
using DnsForwarder.Events;
using DnsForwarder.Exporters;
using DnsForwarder.Metrics;
using DnsForwarder.Ntp;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder.Events.Bootstrap;

public static class EventsServiceCollection
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var metrics = server.Metrics;

        // Make MetricOptions injectable
        services.AddSingleton<MetricOptions>(metrics);

        // Shared EventBus
        services.AddSingleton<EventBus>();

        // Metrics registry (required for Prometheus)
        services.AddSingleton<MetricsRegistry>();

        // Metrics facades (DNS/DHCP/NTP → registry)
        services.AddSingleton<IDhcpMetrics, DhcpMetrics>();
        services.AddSingleton<IDnsMetrics, DnsMetrics>();
        services.AddSingleton<INtpMetrics, NtpMetrics>();

        // Exporters
        if (!metrics.Enabled)
        {
            services.AddHostedService<NullEventExporter>();
            return services;
        }

        switch (metrics.StorageEngine)
        {
            case "json":
                services.AddHostedService<JsonEventExporter>();
                break;

            case "litedb":
                services.AddHostedService<LiteDbEventExporter>();
                break;

            case "prometheus":
                break;

            default:
                services.AddHostedService<NullEventExporter>();
                break;
        }

        return services;
    }
}
