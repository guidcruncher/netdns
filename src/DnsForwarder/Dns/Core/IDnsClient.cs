namespace DnsForwarder.Dns.Core;

public interface IDnsClient
{
    Task<byte[]> QueryAsync(byte[] request, CancellationToken ct);
}
