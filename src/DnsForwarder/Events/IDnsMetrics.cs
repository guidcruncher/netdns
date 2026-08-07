

namespace DnsForwarder.Events;

public interface IDnsMetrics
{
    void RecordDnsQuery(DnsQueryEvent evt);
    void RecordDnsResponse(DnsResponseEvent evt);

    void RecordDnsCacheHit();
    void RecordDnsLatency(double seconds);
}
