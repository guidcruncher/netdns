using System.Text.Json;

using DnsForwarder.Events;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DnsForwarder.Exporters;

public sealed class SqliteEventExporter : BackgroundService
{
    private readonly ILogger<SqliteEventExporter> _logger;
    private readonly EventBus _bus;
    private readonly string _dbPath;

    public SqliteEventExporter(
        ILogger<SqliteEventExporter> logger,
        EventBus bus)
    {
        _logger = logger;
        _bus = bus;

        _dbPath = Path.Combine(AppContext.BaseDirectory, "events.db");

        Initialize();
    }

    private void Initialize()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS dns_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    client_ip TEXT,
    client_name TEXT,
    query_name TEXT,
    query_type TEXT,
    status TEXT,
    response_ip TEXT
);

CREATE TABLE IF NOT EXISTS dhcp_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    client_ip TEXT,
    mac TEXT,
    client_name TEXT,
    lease_start TEXT,
    lease_expiry TEXT,
    server_id TEXT
);

CREATE TABLE IF NOT EXISTS ntp_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp TEXT NOT NULL,
    client_ip TEXT,
    client_name TEXT,
    offset_ms REAL,
    success INTEGER
);
";
        cmd.ExecuteNonQuery();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQLite event exporter started. DB: {Path}", _dbPath);

        while (!stoppingToken.IsCancellationRequested)
        {
            while (_bus.TryRead(out var evt))
            {
                try
                {
                    WriteEvent(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to write event to SQLite");
                }
            }

            await Task.Delay(50, stoppingToken);
        }
    }

    private void WriteEvent(EventRecord evt)
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();

        switch (evt)
        {
            case DnsQueryEvent q:
                WriteDnsQuery(conn, q);
                break;

            case DnsResponseEvent r:
                WriteDnsResponse(conn, r);
                break;

            case DhcpLeaseAllocatedEvent d:
                WriteDhcpLease(conn, d);
                break;

            case DhcpLeaseReleasedEvent rel:
                WriteDhcpRelease(conn, rel);
                break;

            case NtpSyncEvent n:
                WriteNtpSync(conn, n);
                break;
        }
    }

    private static void WriteDnsQuery(SqliteConnection conn, DnsQueryEvent q)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO dns_events (timestamp, client_ip, client_name, query_name, query_type, status, response_ip)
VALUES ($ts, $ip, $name, $qname, $qtype, $status, $rip)";
        cmd.Parameters.AddWithValue("$ts", q.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("$ip", q.ClientIp.ToString());
        cmd.Parameters.AddWithValue("$name", q.ClientName ?? "");
        cmd.Parameters.AddWithValue("$qname", q.QueryName);
        cmd.Parameters.AddWithValue("$qtype", q.QueryType);
        cmd.Parameters.AddWithValue("$status", "QUERY");
        cmd.Parameters.AddWithValue("$rip", "");
        cmd.ExecuteNonQuery();
    }

    private static void WriteDnsResponse(SqliteConnection conn, DnsResponseEvent r)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO dns_events (timestamp, client_ip, client_name, query_name, query_type, status, response_ip)
VALUES ($ts, $ip, $name, $qname, $qtype, $status, $rip)";
        cmd.Parameters.AddWithValue("$ts", r.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("$ip", r.ClientIp.ToString());
        cmd.Parameters.AddWithValue("$name", r.ClientName ?? "");
        cmd.Parameters.AddWithValue("$qname", r.QueryName);
        cmd.Parameters.AddWithValue("$qtype", r.QueryType);
        cmd.Parameters.AddWithValue("$status", r.Status);
        cmd.Parameters.AddWithValue("$rip", r.ResponseIp?.ToString() ?? "");
        cmd.ExecuteNonQuery();
    }

    private static void WriteDhcpLease(SqliteConnection conn, DhcpLeaseAllocatedEvent d)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO dhcp_events (timestamp, client_ip, mac, client_name, lease_start, lease_expiry, server_id)
VALUES ($ts, $ip, $mac, $name, $start, $expiry, $server)";
        cmd.Parameters.AddWithValue("$ts", d.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("$ip", d.ClientIp.ToString());
        cmd.Parameters.AddWithValue("$mac", d.Mac.ToString());
        cmd.Parameters.AddWithValue("$name", d.ClientName ?? "");
        cmd.Parameters.AddWithValue("$start", d.LeaseStart.ToString("o"));
        cmd.Parameters.AddWithValue("$expiry", d.LeaseExpiry.ToString("o"));
        cmd.Parameters.AddWithValue("$server", d.ServerId.ToString());
        cmd.ExecuteNonQuery();
    }

    private static void WriteDhcpRelease(SqliteConnection conn, DhcpLeaseReleasedEvent rel)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO dhcp_events (timestamp, client_ip, mac, client_name, lease_start, lease_expiry, server_id)
VALUES ($ts, '', $mac, '', '', '', '')";
        cmd.Parameters.AddWithValue("$ts", rel.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("$mac", rel.Mac.ToString());
        cmd.ExecuteNonQuery();
    }

    private static void WriteNtpSync(SqliteConnection conn, NtpSyncEvent n)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO ntp_events (timestamp, client_ip, client_name, offset_ms, success)
VALUES ($ts, $ip, $name, $offset, $success)";
        cmd.Parameters.AddWithValue("$ts", n.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("$ip", n.ClientIp.ToString());
        cmd.Parameters.AddWithValue("$name", n.ClientName ?? "");
        cmd.Parameters.AddWithValue("$offset", n.Offset.TotalMilliseconds);
        cmd.Parameters.AddWithValue("$success", n.Success ? 1 : 0);
        cmd.ExecuteNonQuery();
    }
}
