using Microsoft.Extensions.DependencyInjection;
using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Infrastructure.SshNet.DependencyInjection;
using System.Runtime.Versioning;

namespace RemoteLinuxManager.IntegrationTests;

[SupportedOSPlatform("windows")]
public class ServiceRegistrationTests
{
    [Fact]
    public void AddInfrastructure_RegistersCoreServices()
    {
        var services = new ServiceCollection();
        services.AddRemoteLinuxManagerInfrastructure();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IRemoteSessionService>());
        Assert.NotNull(provider.GetService<IFileTransferService>());
        Assert.NotNull(provider.GetService<IScriptExecutionService>());
        Assert.NotNull(provider.GetService<IHostProfileService>());
    }
}
