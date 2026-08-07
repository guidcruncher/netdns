using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Hosting;

public static class HostBuilderFactory
{
    public static IHost Build(string[] args, IConfiguration cmd)
    {
        return Host.CreateDefaultBuilder(args)
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
            .ConfigureServices(ServiceRegistration.Register)
            .Build();
    }
}

