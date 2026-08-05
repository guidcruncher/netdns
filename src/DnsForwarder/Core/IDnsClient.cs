namespace DnsForwarder;

public interface IDnsClient
{
    Task<byte[]> QueryAsync(byte[] request, CancellationToken ct);
}
