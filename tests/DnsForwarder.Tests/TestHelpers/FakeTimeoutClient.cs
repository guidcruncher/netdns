using DnsForwarder;

namespace DnsForwarder.Tests;

public sealed class FakeTimeoutClient : IDnsClient
{
    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        // Simulate a resolver that never responds
        await Task.Delay(Timeout.Infinite, ct);

        // This line is never reached
        return Array.Empty<byte>();
    }
}
