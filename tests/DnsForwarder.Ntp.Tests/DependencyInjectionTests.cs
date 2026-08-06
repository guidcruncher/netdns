using Xunit;
using Microsoft.Extensions.DependencyInjection;
using DnsForwarder.Ntp;

public class DependencyInjectionTests
{
    [Fact]
    public void AddNtpServer_RegistersAllServices()
    {
        var services = new ServiceCollection();

        services.AddNtpServer();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ITimeSource>());
        Assert.NotNull(provider.GetService<INtpRequestHandler>());
        Assert.NotNull(provider.GetService<NtpRuntimeLoader>());
        Assert.NotNull(provider.GetService<NtpServerService>());
    }
}
