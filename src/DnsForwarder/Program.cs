using System.Net;

using DnsForwarder.Dhcp.Bootstrap;
using DnsForwarder.Dns.Bootstrap;
using DnsForwarder.Events.Bootstrap;
using DnsForwarder.Metrics;
using DnsForwarder.Metrics.Bootstrap;
using DnsForwarder.Ntp.Bootstrap;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cmd = new ConfigurationBuilder()
            .AddCommandLine(args, new Dictionary<string, string>
            {
                ["--config"] = "ConfigPath",
                ["--env"] = "DOTNET_ENVIRONMENT",
                ["--listen"] = "ListenOverride",
                ["--resolver"] = "ResolverOverride",
                ["--log-level"] = "Logging:Level"
            })
            .Build();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var env = cmd["DOTNET_ENVIRONMENT"]
                          ?? ctx.HostingEnvironment.EnvironmentName;

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);

                if (env.Equals("Docker", StringComparison.OrdinalIgnoreCase))
                    config.AddJsonFile("appsettings.Docker.json", optional: true, reloadOnChange: true);

                if (cmd["ConfigPath"] is string customConfig && !string.IsNullOrWhiteSpace(customConfig))
                    config.AddJsonFile(customConfig, optional: false, reloadOnChange: true);

                config.AddEnvironmentVariables();
                config.AddConfiguration(cmd);
            })
            .ConfigureLogging((ctx, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();

                var level = ctx.Configuration["Logging:Level"] ?? "Information";
                logging.SetMinimumLevel(Enum.Parse<LogLevel>(level, ignoreCase: true));
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddEventBus(ctx.Configuration);

                services.AddDnsForwarder(ctx.Configuration);
                services.AddDhcpServer(ctx.Configuration);
                services.AddNtpServer(ctx.Configuration);

                // FIXED: Metrics DI registration
                services.AddMetricServices(ctx.Configuration);
            })
            .Build();

        //
        // Runtime loading (DNS + DHCP)
        //
        var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();

        using (var scope = scopeFactory.CreateScope())
        {
            var dnsLoader = new DnsForwarderRuntimeLoader(
                scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await dnsLoader.LoadAsync(scope.ServiceProvider);

            var dhcpLoader = new DhcpRuntimeLoader(
                scope.ServiceProvider.GetRequiredService<IConfiguration>());
            await dhcpLoader.LoadAsync(scope.ServiceProvider);
        }

        //
        // Start Prometheus sidecar HTTP server if enabled
        //
        var serverOptions = host.Services.GetRequiredService<IConfiguration>()
            .GetSection("Server").Get<ServerOptions>() ?? new ServerOptions();

        if (serverOptions.Metrics.StorageEngine == "prometheus")
        {
            var metricsAppBuilder = WebApplication.CreateBuilder(args);

            // Share DI with main host
            metricsAppBuilder.Services.AddSingleton(
                host.Services.GetRequiredService<MetricsRegistry>());
            var metricsPort = serverOptions.Metrics.ListenPort;
            var metricsIpAddress = IPAddress.Parse(serverOptions.Metrics.ListenAddress);

            metricsAppBuilder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Listen(metricsIpAddress, metricsPort);
            });

            var metricsApp = metricsAppBuilder.Build();

            metricsApp.MapGet("/metrics", (MetricsRegistry metrics) =>
            {
                var text = metrics.RenderPrometheus();
                return Results.Text(text, "text/plain; version=0.0.4");
            });

            _ = metricsApp.RunAsync(); // fire-and-forget sidecar
        }

        await host.RunAsync();
    }
}
