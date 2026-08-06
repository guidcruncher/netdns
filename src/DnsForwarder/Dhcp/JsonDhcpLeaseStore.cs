using System.Text.Json;

namespace DnsForwarder.Dhcp;

public sealed class JsonDhcpLeaseStore : IDhcpLeaseStore
{
    private readonly string _path;

    public JsonDhcpLeaseStore(string path)
    {
        _path = path;
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_path))
            return;

        // TODO: deserialize leases
        var json = await File.ReadAllTextAsync(_path);
    }

    public async Task SaveAsync()
    {
        // TODO: serialize leases
        await File.WriteAllTextAsync(_path, "{}");
    }
}
