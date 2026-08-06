using System.Net;

namespace DnsForwarder.Events;

public sealed record DnsQueryEvent(
    DateTime Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    string QueryName,
    string QueryType)
    : EventRecord(Timestamp);

public sealed record DnsResponseEvent(
    DateTime Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    string QueryName,
    string QueryType,
    string Status,
    IPAddress? ResponseIp)
    : EventRecord(Timestamp);

public sealed record DnsUpstreamLatencyEvent(
    DateTime Timestamp,
    string UpstreamName,
    TimeSpan Duration,
    bool Success)
    : EventRecord(Timestamp);
