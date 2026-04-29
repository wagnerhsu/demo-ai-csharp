using Microsoft.Extensions.DependencyInjection;
using RemoteLinuxManager.Application.Security;
using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Infrastructure.SshNet.Security;
using RemoteLinuxManager.Infrastructure.SshNet.Services;

namespace RemoteLinuxManager.Infrastructure.SshNet.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRemoteLinuxManagerInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<SshNetRemoteSessionService>();
        services.AddSingleton<IRemoteSessionService>(provider => provider.GetRequiredService<SshNetRemoteSessionService>());
        services.AddSingleton<IFileTransferService, SshNetFileTransferService>();
        services.AddSingleton<IScriptExecutionService, SshNetScriptExecutionService>();
        services.AddSingleton<IRemoteFileBrowserService, SshNetRemoteFileBrowserService>();
        services.AddSingleton<IHostProfileService, InMemoryHostProfileService>();
        services.AddSingleton<ICredentialStore, InMemoryCredentialStore>();

        return services;
    }
}
