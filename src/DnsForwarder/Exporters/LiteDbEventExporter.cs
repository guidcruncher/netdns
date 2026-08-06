using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DnsForwarder.Events;
using LiteDB;

namespace DnsForwarder.Exporters;

public sealed class LiteDbEventExporter : BackgroundService
{
    private readonly ILogger<LiteDbEventExporter> _logger;
    private readonly EventBus _bus;
    private readonly string _dbPath;
    private LiteDatabase? _db;

    public LiteDbEventExporter(
        ILogger<LiteDbEventExporter> logger,
        EventBus bus)
    {
        _logger = logger;
        _bus = bus;

        _dbPath = Path.Combine(AppContext.BaseDirectory, "events.db");
        _db = new LiteDatabase(_dbPath);

        Initialize();
    }

    private void Initialize()
    {
        // Ensure collections exist
        _db.GetCollection("dns_events");
        _db.GetCollection("dhcp_events");
        _db.GetCollection("ntp_events");

        _logger.LogInformation("LiteDB event exporter initialized at {Path}", _dbPath);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LiteDB event exporter started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            while (_bus.TryDequeue(out var evt))
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

            await Task.Delay(50, stoppingToken);
        }
    }

    private void WriteEvent(EventRecord evt)
    {
        if (_db == null)
            return;

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
        var col = _db!.GetCollection("dns_events");
        col.Insert(new
        {
            timestamp = q.Timestamp,
            client_ip = q.ClientIp.ToString(),
            client_name = q.ClientName,
            query_name = q.QueryName,
            query_type = q.QueryType,
            status = "QUERY",
            response_ip = ""
        });
    }

    private void WriteDnsResponse(DnsResponseEvent r)
    {
        var col = _db!.GetCollection("dns_events");
        col.Insert(new
        {
            timestamp = r.Timestamp,
            client_ip = r.ClientIp.ToString(),
            client_name = r.ClientName,
            query_name = r.QueryName,
            query_type = r.QueryType,
            status = r.Status,
            response_ip = r.ResponseIp?.ToString() ?? ""
        });
    }

    private void WriteDhcpLease(DhcpLeaseAllocatedEvent d)
    {
        var col = _db!.GetCollection("dhcp_events");
        col.Insert(new
        {
            timestamp = d.Timestamp,
            client_ip = d.ClientIp.ToString(),
            mac = d.Mac.ToString(),
            client_name = d.ClientName,
            lease_start = d.LeaseStart,
            lease_expiry = d.LeaseExpiry,
            server_id = d.ServerId.ToString()
        });
    }

    private void WriteDhcpRelease(DhcpLeaseReleasedEvent rel)
    {
        var col = _db!.GetCollection("dhcp_events");
        col.Insert(new
        {
            timestamp = rel.Timestamp,
            client_ip = "",
            mac = rel.Mac.ToString(),
            client_name = "",
            lease_start = "",
            lease_expiry = "",
            server_id = ""
        });
    }

    private void WriteNtpSync(NtpSyncEvent n)
    {
        var col = _db!.GetCollection("ntp_events");
        col.Insert(new
        {
            timestamp = n.Timestamp,
            client_ip = n.ClientIp.ToString(),
            client_name = n.ClientName,
            offset_ms = n.Offset.TotalMilliseconds,
            success = n.Success
        });
    }

    public override void Dispose()
    {
        _db?.Dispose();
        base.Dispose();
    }
}
