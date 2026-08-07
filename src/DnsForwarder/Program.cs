using DnsForwarder.Hosting;

using Microsoft.Extensions.Configuration;

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

        var host = HostBuilderFactory.Build(args, cmd);

        await RuntimeLoader.LoadAsync(host);

        var serverOptions = host.Services.GetRequiredService<ServerOptions>();
        MetricsSidecar.StartIfEnabled(host, serverOptions, args);

        await host.RunAsync();
    }
}
