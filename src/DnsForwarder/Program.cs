using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder;

public class Program
{
    public static async Task Main(string[] args)
    {
        //
        // Command-line flags
        //
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

        //
        // Host builder
        //
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
            })
            .Build();

        //
        // Runtime loading (hosts, blocklists, allowlists)
        //
        var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();

        using (var scope = scopeFactory.CreateScope())
        {
            var loader = new DnsForwarderRuntimeLoader(
                scope.ServiceProvider.GetRequiredService<IConfiguration>());

            await loader.LoadAsync(scope.ServiceProvider);
        }

        await host.RunAsync();
    }
}
