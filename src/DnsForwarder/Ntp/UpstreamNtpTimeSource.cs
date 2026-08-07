using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace DnsForwarder.Ntp;

public sealed class UpstreamNtpTimeSource : INtpTimeSource, IAsyncDisposable
{
    private readonly ILogger<UpstreamNtpTimeSource> _logger;
    private readonly UpstreamNtpOptions _options;

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _syncTask;

    private DateTime _referenceUtc = DateTime.UtcNow;
    private TimeSpan _offset = TimeSpan.Zero;
    private int _stratum = 2;

    public UpstreamNtpTimeSource(
        ILogger<UpstreamNtpTimeSource> logger,
        NtpServerOptions options)
    {
        _logger = logger;
        _options = options.Upstream;

        _syncTask = Task.Run(() => SyncLoopAsync(_cts.Token));
    }

    public Task<NtpTimeResult> GetTimeAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow + _offset;

        return Task.FromResult(new NtpTimeResult(
            UtcNow: now,
            Offset: _offset,
            Stratum: _stratum,
            ReferenceUtc: _referenceUtc));
    }

    private async Task SyncLoopAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Upstream NTP sync disabled.");
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            foreach (var server in _options.Servers)
            {
                try
                {
                    await SyncOnceAsync(server, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upstream NTP sync failed for {Server}", server);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), ct)
                      .ConfigureAwait(false);
        }
    }

    private async Task SyncOnceAsync(string server, CancellationToken ct)
    {
        using var udp = new UdpClient();
        udp.Client.ReceiveTimeout = 3000;

        var ip = await System.Net.Dns.GetHostAddressesAsync(server, ct).ConfigureAwait(false);
        var endpoint = new IPEndPoint(ip[0], 123);

        var request = BuildClientRequest();
        var t1 = DateTime.UtcNow;

        await udp.SendAsync(request, request.Length, endpoint).ConfigureAwait(false);

        var response = await udp.ReceiveAsync(ct).ConfigureAwait(false);
        var t4 = DateTime.UtcNow;

        ParseResponse(response.Buffer, t1, t4);
    }

    private static byte[] BuildClientRequest()
    {
        var buffer = new byte[48];
        buffer[0] = 0b_00100011; // LI=0, VN=4, Mode=3 (client)
        return buffer;
    }

    private void ParseResponse(byte[] buffer, DateTime t1, DateTime t4)
    {
        var t2 = ReadTimestamp(buffer, 32);
        var t3 = ReadTimestamp(buffer, 40);

        var offset = ((t2 - t1) + (t3 - t4)) / 2;
        var delay = (t4 - t1) - (t3 - t2);

        _offset = offset;
        _referenceUtc = t3;
        _stratum = buffer[1];

        _logger.LogInformation(
            "Upstream NTP sync: offset={Offset} delay={Delay} stratum={Stratum}",
            offset, delay, _stratum);
    }

    private static DateTime ReadTimestamp(byte[] buffer, int offset)
    {
        uint seconds = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset, 4));
        uint fraction = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 4, 4));

        var epoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double fracSeconds = fraction / (double)0x100000000L;

        return epoch.AddSeconds(seconds + fracSeconds);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            await _syncTask.ConfigureAwait(false);
        }
        catch
        {
        }

        _cts.Dispose();
    }
}
