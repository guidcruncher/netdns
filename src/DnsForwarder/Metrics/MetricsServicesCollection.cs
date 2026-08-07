using DnsForwarder.Events;
using DnsForwarder.Exporters;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder.Metrics.Bootstrap;

public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddMetricServices(
        this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var metrics = server.Metrics;

        // Always register the registry (safe, lightweight)
        services.AddSingleton<MetricsRegistry>();
        services.AddSingleton<IEventConsumer, MetricsEventConsumer>();

        return services;
    }
}

