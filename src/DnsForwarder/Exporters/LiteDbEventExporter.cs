using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DnsForwarder.Events;
using LiteDB;

namespace DnsForwarder.Exporters;

public sealed class LiteDbEventExporter : BackgroundService
{
    private readonly ILogger<LiteDbEventExporter> _logger;
    private readonly EventBus _bus;
    private readonly LiteDatabase _db;

    public LiteDbEventExporter(
        ILogger<LiteDbEventExporter> logger,
        EventBus bus)
    {
        _logger = logger;
        _bus = bus;

        var dbPath = Path.Combine(AppContext.BaseDirectory, "events.db");
        _db = new LiteDatabase(dbPath);

        Initialize();
    }

    private void Initialize()
    {
        _db.GetCollection<DnsEventDoc>("dns_events");
        _db.GetCollection<DhcpEventDoc>("dhcp_events");
        _db.GetCollection<NtpEventDoc>("ntp_events");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LiteDB event exporter started.");

        await foreach (var evt in _bus.ConsumeAsync(stoppingToken))
        {
            try
            {
                WriteEvent(evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write event to LiteDB");
            }
        }
    }

    private void WriteEvent(EventRecord evt)
    {
        switch (evt)
        {
            case DnsQueryEvent q:
                WriteDnsQuery(q);
                break;

            case DnsResponseEvent r:
                WriteDnsResponse(r);
                break;

            case DhcpLeaseAllocatedEvent d:
                WriteDhcpLease(d);
                break;

            case DhcpLeaseReleasedEvent rel:
                WriteDhcpRelease(rel);
                break;

            case NtpSyncEvent n:
                WriteNtpSync(n);
                break;
        }
    }

    private void WriteDnsQuery(DnsQueryEvent q)
    {
        var col = _db.GetCollection<DnsEventDoc>("dns_events");
        col.Insert(new DnsEventDoc
        {
            Timestamp = q.Timestamp,
            ClientIp = q.ClientIp.ToString(),
            ClientName = q.ClientName ?? "",
            QueryName = q.QueryName,
            QueryType = q.QueryType,
            Status = "QUERY",
            ResponseIp = ""
        });
    }

    private void WriteDnsResponse(DnsResponseEvent r)
    {
        var col = _db.GetCollection<DnsEventDoc>("dns_events");
        col.Insert(new DnsEventDoc
        {
            Timestamp = r.Timestamp,
            ClientIp = r.ClientIp.ToString(),
            ClientName = r.ClientName ?? "",
            QueryName = r.QueryName,
            QueryType = r.QueryType,
            Status = r.Status,
            ResponseIp = r.ResponseIp?.ToString() ?? ""
        });
    }

    private void WriteDhcpLease(DhcpLeaseAllocatedEvent d)
    {
        var col = _db.GetCollection<DhcpEventDoc>("dhcp_events");
        col.Insert(new DhcpEventDoc
        {
            Timestamp = d.Timestamp,
            ClientIp = d.ClientIp.ToString(),
            Mac = d.Mac.ToString(),
            ClientName = d.ClientName ?? "",
            LeaseStart = d.LeaseStart,
            LeaseExpiry = d.LeaseExpiry,
            ServerId = d.ServerId.ToString()
        });
    }

    private void WriteDhcpRelease(DhcpLeaseReleasedEvent rel)
    {
        var col = _db.GetCollection<DhcpEventDoc>("dhcp_events");
        col.Insert(new DhcpEventDoc
        {
            Timestamp = rel.Timestamp,
            ClientIp = "",
            Mac = rel.Mac.ToString(),
            ClientName = "",
            LeaseStart = DateTime.MinValue,
            LeaseExpiry = DateTime.MinValue,
            ServerId = ""
        });
    }

    private void WriteNtpSync(NtpSyncEvent n)
    {
        var col = _db.GetCollection<NtpEventDoc>("ntp_events");
        col.Insert(new NtpEventDoc
        {
            Timestamp = n.Timestamp,
            ClientIp = n.ClientIp.ToString(),
            ClientName = n.ClientName ?? "",
            OffsetMs = n.Offset.TotalMilliseconds,
            Success = n.Success
        });
    }
}

public class DnsEventDoc
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ClientIp { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string QueryName { get; set; } = "";
    public string QueryType { get; set; } = "";
    public string Status { get; set; } = "";
    public string ResponseIp { get; set; } = "";
}

public class DhcpEventDoc
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ClientIp { get; set; } = "";
    public string Mac { get; set; } = "";
    public string ClientName { get; set; } = "";
    public DateTime LeaseStart { get; set; }
    public DateTime LeaseExpiry { get; set; }
    public string ServerId { get; set; } = "";
}

public class NtpEventDoc
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ClientIp { get; set; } = "";
    public string ClientName { get; set; } = "";
    public double OffsetMs { get; set; }
    public bool Success { get; set; }
}
