using System.Net;

using DnsForwarder.Filtering;
using DnsForwarder.RuleEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DnsForwarder;

public sealed class DnsForwarderRuntimeLoader
{
    private readonly IConfiguration _config;

    public DnsForwarderRuntimeLoader(IConfiguration config)
    {
        _config = config;
    }

    public async Task LoadAsync(IServiceProvider services)
    {
        var options = services.GetRequiredService<DnsForwarderOptions>();
        var engine = services.GetRequiredService<RuleEngine.RuleEngine>();

        await LoadHostsAsync(options, engine);
        await LoadBlocklistsAsync(options, engine);
        await LoadAllowlistsAsync(options, engine);
    }

    // ------------------------------------------------------------
    // HOSTS
    // ------------------------------------------------------------
    private async Task LoadHostsAsync(DnsForwarderOptions options, RuleEngine.RuleEngine engine)
    {
        if (options.HostsFiles?.Any() != true)
            return;

        var hostsPaths = options.HostsFiles
            .Select(p => p.StartsWith("file://") ? p[7..] : p);

        var hostsSource = new HostsFileSource(hostsPaths);
        await engine.AddHostsAsync(hostsSource);
    }

    // ------------------------------------------------------------
    // BLOCKLISTS
    // ------------------------------------------------------------
    private async Task LoadBlocklistsAsync(DnsForwarderOptions options, RuleEngine.RuleEngine engine)
    {
        if (options.Blocklists?.Any() != true)
            return;

        var source = CreateSource(options.Blocklists);
        await engine.AddListAsync(source, block: true);
    }

    // ------------------------------------------------------------
    // ALLOWLISTS
    // ------------------------------------------------------------
    private async Task LoadAllowlistsAsync(DnsForwarderOptions options, RuleEngine.RuleEngine engine)
    {
        if (options.Allowlists?.Any() != true)
            return;

        var source = CreateSource(options.Allowlists);
        await engine.AddListAsync(source, block: false);
    }

    // ------------------------------------------------------------
    // SOURCE SELECTION (file:// vs URL)
    // ------------------------------------------------------------
    private static IBlocklistSource CreateSource(IEnumerable<string> items)
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
}
