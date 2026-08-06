using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace DnsForwarder.Dhcp;

public sealed class JsonDhcpLeaseStore : IDhcpLeaseStore
{
    private readonly string _path;

    private readonly Dictionary<PhysicalAddress, DhcpLease> _leases = new();
    private readonly HashSet<IPAddress> _badIps = new();

    public JsonDhcpLeaseStore(string path)
    {
        _path = path;
    }

    // ------------------------------------------------------------
    // LOAD
    // ------------------------------------------------------------
    public async Task LoadAsync()
    {
        if (!File.Exists(_path))
            return;

        var json = await File.ReadAllTextAsync(_path);

        var dto = JsonSerializer.Deserialize<DhcpLeaseStoreDto>(json);
        if (dto == null)
            return;

        _leases.Clear();
        foreach (var l in dto.Leases)
        {
            var mac = new PhysicalAddress(l.Mac);
            _leases[mac] = new DhcpLease
            {
                Mac = mac,
                Ip = new IPAddress(l.Ip),
                ExpiresAt = l.ExpiresAt
            };
        }

        _badIps.Clear();
        foreach (var ip in dto.BadIps)
            _badIps.Add(new IPAddress(ip));
    }

    // ------------------------------------------------------------
    // SAVE
    // ------------------------------------------------------------
    public async Task SaveAsync()
    {
        var dto = new DhcpLeaseStoreDto
        {
            Leases = _leases.Values.Select(l => new DhcpLeaseDto
            {
                Mac = l.Mac.GetAddressBytes(),
                Ip = l.Ip.GetAddressBytes(),
                ExpiresAt = l.ExpiresAt
            }).ToList(),

            BadIps = _badIps.Select(ip => ip.GetAddressBytes()).ToList()
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_path, json);
    }

    // ------------------------------------------------------------
    // ACTIVE LEASES
    // ------------------------------------------------------------
    public IEnumerable<DhcpLease> GetActiveLeases()
    {
        var now = DateTimeOffset.UtcNow;
        return _leases.Values.Where(l => l.ExpiresAt > now);
    }

    // ------------------------------------------------------------
    // SAVE / UPDATE LEASE
    // ------------------------------------------------------------
    public void Save(DhcpLease lease)
    {
        _leases[lease.Mac] = lease;
        _ = SaveAsync();
    }

    // ------------------------------------------------------------
    // REMOVE LEASE
    // ------------------------------------------------------------
    public void Remove(PhysicalAddress mac)
    {
        _leases.Remove(mac);
        _ = SaveAsync();
    }

    // ------------------------------------------------------------
    // BAD IPs
    // ------------------------------------------------------------
    public IEnumerable<IPAddress> GetBadIps() => _badIps;

    public void AddBadIp(IPAddress ip)
    {
        _badIps.Add(ip);
        _ = SaveAsync();
    }

    public void RemoveBadIp(IPAddress ip)
    {
        _badIps.Remove(ip);
        _ = SaveAsync();
    }

    // ------------------------------------------------------------
    // DTOs
    // ------------------------------------------------------------
    private sealed class DhcpLeaseStoreDto
    {
        public List<DhcpLeaseDto> Leases { get; set; } = new();
        public List<byte[]> BadIps { get; set; } = new();
    }

    private sealed class DhcpLeaseDto
    {
        public byte[] Mac { get; set; } = default!;
        public byte[] Ip { get; set; } = default!;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}
