using System.Net;

using DnsForwarder.Metrics;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Hosting;

public static class MetricsSidecar
{
    public static void StartIfEnabled(IHost mainHost, ServerOptions serverOptions, string[] args)
    {
        var logger = mainHost.Services.GetRequiredService<ILogger<Program>>();

        if (serverOptions.Metrics.StorageEngine != "prometheus")
        {
            logger.LogInformation("Metrics sidecar disabled (StorageEngine != prometheus).");
            return;
        }

        logger.LogInformation(
            "Starting Prometheus metrics sidecar on http://{Address}:{Port}/{Location}",
            serverOptions.Metrics.ListenAddress,
            serverOptions.Metrics.ListenPort,
            serverOptions.Metrics.Location);

        var metricsHost = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(options =>
                {
                    options.Listen(
                        IPAddress.Parse(serverOptions.Metrics.ListenAddress),
                        serverOptions.Metrics.ListenPort);

                    logger.LogInformation(
                        "Kestrel bound to {Address}:{Port}",
                        serverOptions.Metrics.ListenAddress,
                        serverOptions.Metrics.ListenPort);
                });

                web.Configure(app =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(serverOptions.Metrics.Location, async context =>
                        {
                            var registry = mainHost.Services.GetRequiredService<MetricsRegistry>();
                            var text = registry.RenderPrometheus();

                            context.Response.ContentType = "text/plain; version=0.0.4";

                            logger.LogDebug("Metrics scraped from {RemoteIp}", context.Connection.RemoteIpAddress);

                            await context.Response.WriteAsync(text);
                        });
                    });

                    logger.LogInformation("Metrics endpoint registered at {Location}", serverOptions.Metrics.Location);
                });
            })
            .Build();

        _ = metricsHost.RunAsync();

        logger.LogInformation("Metrics sidecar started successfully.");
    }
}
