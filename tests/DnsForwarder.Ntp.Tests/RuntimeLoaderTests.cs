using DnsForwarder.Ntp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

public class RuntimeLoaderTests
{
    [Fact]
    public async Task Loader_DoesNotInitialize_WhenDisabled()
    {
        var logger = Mock.Of<ILogger<NtpRuntimeLoader>>();
        var timeSource = Mock.Of<ITimeSource>();
        var options = Options.Create(new NtpServerOptions { Enabled = false });

        var loader = new NtpRuntimeLoader(logger, options, timeSource);

        await loader.StartAsync(default);

        // No exception = pass
    }
}
