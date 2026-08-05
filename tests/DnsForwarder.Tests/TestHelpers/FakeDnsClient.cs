using System.Text;

using DnsForwarder;

namespace DnsForwarder.Tests;

public sealed class FakeDnsClient : IDnsClient
{
    private readonly string _marker;

    public FakeDnsClient(string marker)
    {
        _marker = marker;
    }

    public Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        return Task.FromResult(Encoding.UTF8.GetBytes(_marker));
    }
}
