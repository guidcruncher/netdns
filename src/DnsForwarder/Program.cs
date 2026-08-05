using System.Net;

using DnsForwarder.Filtering;
using DnsForwarder.RuleEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Parse command-line flags
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
                {
                    config.AddJsonFile("appsettings.Docker.json", optional: true, reloadOnChange: true);
                }

                var customConfig = cmd["ConfigPath"];
                if (!string.IsNullOrWhiteSpace(customConfig))
                {
                    config.AddJsonFile(customConfig, optional: false, reloadOnChange: true);
                }

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
                var server = ctx.Configuration.Get<ServerOptions>() ?? new ServerOptions;

                var options = server.Dns

                // Override listen address via --listen
                if (ctx.Configuration["ListenOverride"] is string listen)
                {
                    var parts = listen.Split(':');
                    options.Listen.Address = parts[0];
                    options.Listen.Port = int.Parse(parts[1]);
                }

                // Override default resolver via --resolver
                if (ctx.Configuration["ResolverOverride"] is string resolver)
                {
                    var parts = resolver.Split(':');
                    options.DefaultResolver.Address = parts[0];
                    options.DefaultResolver.Port = parts.Length > 1 ? int.Parse(parts[1]) : 53;
                }

                services.AddSingleton(options);

                // DNS client + caching
                services.AddSingleton<StaticDnsClient>();

                services.AddSingleton<IDnsClient>(sp =>
                {
                    var opt = sp.GetRequiredService<DnsForwarderOptions>();
                    var endpoint = new IPEndPoint(
                        IPAddress.Parse(opt.DefaultResolver.Address),
                        opt.DefaultResolver.Port);

                    IDnsClient client = new UdpDnsClient(endpoint);

                    if (opt.Caching.Enabled)
                        client = new CachingDnsClientDecorator(client, opt.Caching.MaxEntries);

                    return client;
                });

                // Rule engine
                services.AddSingleton<RuleEngine.RuleEngine>();

                // Hosts loader
                services.AddSingleton<HostsFileSource>();

                // DNS forwarder service + server
                services.AddSingleton<DnsForwarderService>();
                services.AddHostedService<DnsServer>();
            })
            .Build();

        //
        // Load hosts + blocklists + allowlists AFTER building the host
        //
        using (var scope = host.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<DnsForwarderOptions>();
            var engine = scope.ServiceProvider.GetRequiredService<RuleEngine.RuleEngine>();

            //
            // HOSTS FILES
            //
            if (options.HostsFiles?.Any() == true)
            {
                var hostsPaths = options.HostsFiles
                    .Select(p => p.StartsWith("file://") ? p[7..] : p);

                var hostsSource = new HostsFileSource(hostsPaths);
                await engine.AddHostsAsync(hostsSource);
            }

            //
            // Helper: choose correct loader based on prefix
            //
            IBlocklistSource CreateSource(IEnumerable<string> items)
            {
                var fileItems = items
                    .Where(i => i.StartsWith("file://"))
                    .Select(i => i.Replace("file://", ""));

                var urlItems = items
                    .Where(i => !i.StartsWith("file://"));

                if (fileItems.Any() && urlItems.Any())
                {
                    return new CompositeBlocklistSource(new IBlocklistSource[]
                    {
                        new FileBlocklistSource(fileItems),
                        new UrlBlocklistSource(urlItems)
                    });
                }

                if (fileItems.Any())
                    return new FileBlocklistSource(fileItems);

                return new UrlBlocklistSource(urlItems);
            }

            //
            // BLOCKLISTS
            //
            if (options.Blocklists?.Any() == true)
            {
                var source = CreateSource(options.Blocklists);
                await engine.AddListAsync(source, block: true);
            }

            //
            // ALLOWLISTS
            //
            if (options.Allowlists?.Any() == true)
            {
                var source = CreateSource(options.Allowlists);
                await engine.AddListAsync(source, block: false);
            }
        }

        await host.RunAsync();
    }
}
