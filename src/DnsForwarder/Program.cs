using DnsForwarder.Bootstrap;

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
                services.AddDnsForwarder(ctx.Configuration);
                services.AddDhcpServer(ctx.Configuration);   // <-- DHCP added here
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

        await host.RunAsync();
    }
}
