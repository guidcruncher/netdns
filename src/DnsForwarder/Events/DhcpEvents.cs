using System.Net;
using System.Net.NetworkInformation;

namespace DnsForwarder.Events;

public sealed record DhcpLeaseAllocatedEvent(
    DateTime Timestamp,
    IPAddress ClientIp,
    PhysicalAddress Mac,
    string? ClientName,
    IPAddress ServerId,
    DateTime LeaseStart,
    DateTime LeaseExpiry)
    : EventRecord(Timestamp);

public sealed record DhcpLeaseReleasedEvent(
    DateTime Timestamp,
    PhysicalAddress Mac,
    IPAddress? ClientIp,
    string? ClientName)
    : EventRecord(Timestamp);

public sealed record DhcpNakEvent(
    DateTime Timestamp,
    PhysicalAddress Mac,
    IPAddress? RequestedIp,
    string Reason)
    : EventRecord(Timestamp);
