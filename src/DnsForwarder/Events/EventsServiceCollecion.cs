using DnsForwarder.Dhcp;
using DnsForwarder.Dns;
using DnsForwarder.Events;
using DnsForwarder.Exporters;
using DnsForwarder.Ntp;

using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder.Ebents;

public static class EventsServiceCollection
{
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        // Shared EventBus
        services.AddSingleton<EventBus>();

        // Metrics facades
        services.AddSingleton<IDhcpMetrics, DhcpMetrics>();
        services.AddSingleton<IDnsMetrics, DnsMetrics>();
        services.AddSingleton<INtpMetrics, NtpMetrics>();

        // Exporters (background services)
        services.AddHostedService<JsonEventExporter>();

        return services;
    }
}
