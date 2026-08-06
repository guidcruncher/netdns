using DnsForwarder;
using DnsForwarder.Ntp;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DnsForwarder.Ntp.Bootstrap;


public sealed class NtpRuntimeLoader : IHostedService
{
    private readonly ILogger<NtpRuntimeLoader> _logger;
    private readonly NtpServerOptions _options;
    private readonly ITimeSource _timeSource;

    public NtpRuntimeLoader(
        ILogger<NtpRuntimeLoader> logger,
        NtpServerOptions options,
        ITimeSource timeSource)
    {
        _logger = logger;
        _options = options;
        _timeSource = timeSource;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("NTP Server is disabled. Runtime loader will not initialize.");
            return;
        }

        _logger.LogInformation("NTP Runtime Loader starting…");

        // Example: warm-up reference timestamp
        var refUtc = _timeSource.ReferenceUtc;
        _logger.LogInformation("Reference time initialized: {RefUtc}", refUtc);

        // Example: load upstream sync or GPS discipline
        await Task.Delay(10, cancellationToken); // placeholder for real work

        _logger.LogInformation("NTP Runtime Loader completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NTP Runtime Loader stopping.");
        return Task.CompletedTask;
    }
}

