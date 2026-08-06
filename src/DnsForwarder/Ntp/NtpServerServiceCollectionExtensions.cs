using System.Net;

using DnsForwarder;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Ntp;

public static class NtpServerServiceCollectionExtensions
{
    public static IServiceCollection AddNtpServer(
        this IServiceCollection services,
        Action<NtpServerOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddSingleton<ITimeSource, SystemTimeSource>();
        services.AddSingleton<INtpRequestHandler, NtpRequestHandler>();
        services.AddHostedService<NtpServerService>();

        return services;
    }
}
