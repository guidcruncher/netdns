using DnsForwarder.Events;

namespace DnsForwarder.Metrics;

public sealed class MetricsRegistry
{
    private long _dnsQueriesTotal;
    private long _dnsResponsesTotal;
    private long _dnsNxDomainTotal;
    private long _dnsServFailTotal;

    private long _dhcpLeaseAllocationsTotal;
    private long _dhcpLeasesActive;

    private long _ntpSyncTotal;
    private long _ntpSyncFailuresTotal;
    private double _ntpOffsetMs;

    private readonly object _lock = new();

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

    public string RenderPrometheus()
    {
        lock (_lock)
        {
            var sb = new System.Text.StringBuilder();

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

            sb.AppendLine("# HELP dhcp_lease_allocations_total Total number of DHCP lease allocations.");
            sb.AppendLine("# TYPE dhcp_lease_allocations_total counter");
            sb.AppendLine($"dhcp_lease_allocations_total {_dhcpLeaseAllocationsTotal}");

            sb.AppendLine("# HELP dhcp_leases_active Number of active DHCP leases.");
            sb.AppendLine("# TYPE dhcp_leases_active gauge");
            sb.AppendLine($"dhcp_leases_active {_dhcpLeasesActive}");

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
