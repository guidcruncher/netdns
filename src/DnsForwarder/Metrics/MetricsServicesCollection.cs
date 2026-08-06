using DnsForwarder;
using DnsForwarder.Events;
using DnsForwarder.Exporters;
using DnsForwarder.Metrics;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DnsForwarder.Metrics.Bootstrap;

public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddMetricServices(
        this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var metrics = server.Metrics;

        if (metrics.StorageEngine != "prometheus") { return services; }

        services.AddSingleton<MetricsRegistry>();

        services.AddHostedService<PrometheusMetricsExporter>();

        return services;
    }
}
