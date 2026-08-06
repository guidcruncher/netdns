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
        // Bind ServerOptions from configuration
        var server = config.GetSection("Server").Get<ServerOptions>() ?? new ServerOptions();
        var metrics = server.Metrics;

        // Always register the registry (safe, lightweight)
        services.AddSingleton<MetricsRegistry>();


        // Conditionally enable Prometheus
        if (string.Equals(metrics.StorageEngine, "prometheus", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHostedService<PrometheusMetricsExporter>();
        }

        return services;
    }
}
