using System.Net;

namespace DnsForwarder.Events;

public sealed record NtpSyncEvent(
    DateTime Timestamp,
    IPAddress ClientIp,
    string? ClientName,
    TimeSpan Offset,
    bool Success)
    : EventRecord(Timestamp);
