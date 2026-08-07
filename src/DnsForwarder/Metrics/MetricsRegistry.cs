using System.Text;

using DnsForwarder.Events;

namespace DnsForwarder.Metrics;

public sealed class MetricsRegistry
{
    private long _dnsQueriesTotal;
    private long _dnsResponsesTotal;
    private long _dnsNxDomainTotal;
    private long _dnsServFailTotal;

    private long _dnsCacheHitsTotal;

    private readonly double[] _dnsLatencyBuckets =
        new double[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2 };

    private readonly long[] _dnsLatencyCounts;
    private double _dnsLatencySum;
    private long _dnsLatencyTotalCount;

    private long _dhcpLeaseAllocationsTotal;
    private long _dhcpLeasesActive;

    private long _ntpSyncTotal;
    private long _ntpSyncFailuresTotal;
    private double _ntpOffsetMs;

    private readonly object _lock = new();

    public MetricsRegistry()
    {
        _dnsLatencyCounts = new long[_dnsLatencyBuckets.Length];
    }

    // -----------------------------
    // DNS Metrics
    // -----------------------------

    public void RecordDnsQuery(DnsQueryEvent evt)
    {
        lock (_lock)
        {
            _dnsQueriesTotal++;
        }
    }

    public void RecordDnsResponse(DnsResponseEvent evt)
    {
        lock (_lock)
        {
            _dnsResponsesTotal++;

            if (evt.Status == "NXDOMAIN")
                _dnsNxDomainTotal++;
            else if (evt.Status == "SERVFAIL")
                _dnsServFailTotal++;
        }
    }

    public void RecordDnsCacheHit()
    {
        lock (_lock)
        {
            _dnsCacheHitsTotal++;
        }
    }

    public void RecordDnsLatency(double seconds)
    {
        lock (_lock)
        {
            _dnsLatencySum += seconds;
            _dnsLatencyTotalCount++;

            for (int i = 0; i < _dnsLatencyBuckets.Length; i++)
            {
                if (seconds <= _dnsLatencyBuckets[i])
                {
                    _dnsLatencyCounts[i]++;
                    break;
                }
            }
        }
    }

    // -----------------------------
    // DHCP Metrics
    // -----------------------------

    public void RecordDhcpLeaseAllocated(DhcpLeaseAllocatedEvent evt)
    {
        lock (_lock)
        {
            _dhcpLeaseAllocationsTotal++;
            _dhcpLeasesActive++;
        }
    }

    public void RecordDhcpLeaseReleased(DhcpLeaseReleasedEvent evt)
    {
        lock (_lock)
        {
            if (_dhcpLeasesActive > 0)
                _dhcpLeasesActive--;
        }
    }

    // -----------------------------
    // NTP Metrics
    // -----------------------------

    public void RecordNtpSync(NtpSyncEvent evt)
    {
        lock (_lock)
        {
            _ntpSyncTotal++;
            if (!evt.Success)
                _ntpSyncFailuresTotal++;

            _ntpOffsetMs = evt.Offset.TotalMilliseconds;
        }
    }

    // -----------------------------
    // Prometheus Output
    // -----------------------------

    public string RenderPrometheus()
    {
        lock (_lock)
        {
            var sb = new StringBuilder();

            // DNS Counters
            sb.AppendLine("# HELP dns_queries_total Total number of DNS queries.");
            sb.AppendLine("# TYPE dns_queries_total counter");
            sb.AppendLine($"dns_queries_total {_dnsQueriesTotal}");

            sb.AppendLine("# HELP dns_responses_total Total number of DNS responses.");
            sb.AppendLine("# TYPE dns_responses_total counter");
            sb.AppendLine($"dns_responses_total {_dnsResponsesTotal}");

            sb.AppendLine("# HELP dns_nxdomain_total Total number of NXDOMAIN responses.");
            sb.AppendLine("# TYPE dns_nxdomain_total counter");
            sb.AppendLine($"dns_nxdomain_total {_dnsNxDomainTotal}");

            sb.AppendLine("# HELP dns_servfail_total Total number of SERVFAIL responses.");
            sb.AppendLine("# TYPE dns_servfail_total counter");
            sb.AppendLine($"dns_servfail_total {_dnsServFailTotal}");

            sb.AppendLine("# HELP dns_cache_hits_total Total number of DNS cache hits.");
            sb.AppendLine("# TYPE dns_cache_hits_total counter");
            sb.AppendLine($"dns_cache_hits_total {_dnsCacheHitsTotal}");

            // DNS Latency Histogram
            sb.AppendLine("# HELP dns_latency_seconds DNS query latency in seconds.");
            sb.AppendLine("# TYPE dns_latency_seconds histogram");

            long cumulative = 0;
            for (int i = 0; i < _dnsLatencyBuckets.Length; i++)
            {
                cumulative += _dnsLatencyCounts[i];
                sb.AppendLine(
                    $"dns_latency_seconds_bucket{{le=\"{_dnsLatencyBuckets[i]}\"}} {cumulative}"
                );
            }

            sb.AppendLine($"dns_latency_seconds_sum {_dnsLatencySum}");
            sb.AppendLine($"dns_latency_seconds_count {_dnsLatencyTotalCount}");

            // DHCP
            sb.AppendLine("# HELP dhcp_lease_allocations_total Total number of DHCP lease allocations.");
            sb.AppendLine("# TYPE dhcp_lease_allocations_total counter");
            sb.AppendLine($"dhcp_lease_allocations_total {_dhcpLeaseAllocationsTotal}");

            sb.AppendLine("# HELP dhcp_leases_active Number of active DHCP leases.");
            sb.AppendLine("# TYPE dhcp_leases_active gauge");
            sb.AppendLine($"dhcp_leases_active {_dhcpLeasesActive}");

            // NTP
            sb.AppendLine("# HELP ntp_sync_total Total number of NTP sync attempts.");
            sb.AppendLine("# TYPE ntp_sync_total counter");
            sb.AppendLine($"ntp_sync_total {_ntpSyncTotal}");

            sb.AppendLine("# HELP ntp_sync_failures_total Total number of failed NTP syncs.");
            sb.AppendLine("# TYPE ntp_sync_failures_total counter");
            sb.AppendLine($"ntp_sync_failures_total {_ntpSyncFailuresTotal}");

            sb.AppendLine("# HELP ntp_offset_ms Last measured NTP offset in milliseconds.");
            sb.AppendLine("# TYPE ntp_offset_ms gauge");
            sb.AppendLine($"ntp_offset_ms {_ntpOffsetMs}");

            return sb.ToString();
        }
    }
}
