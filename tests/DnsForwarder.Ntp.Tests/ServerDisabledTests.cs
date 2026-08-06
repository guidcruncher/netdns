using DnsForwarder.Ntp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Xunit;

public class ServerDisabledTests
{
    [Fact]
    public async Task Server_DoesNotBind_WhenDisabled()
    {
        var logger = Mock.Of<ILogger<NtpServerService>>();
        var handler = Mock.Of<INtpRequestHandler>();
        var options = Options.Create(new NtpServerOptions { Enabled = false });

        var server = new NtpServerService(logger, handler, options);

        await server.StartAsync(default);

        // No exception = pass
    }
}
