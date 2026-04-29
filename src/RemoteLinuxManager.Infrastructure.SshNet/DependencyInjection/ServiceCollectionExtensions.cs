using Microsoft.Extensions.DependencyInjection;
using RemoteLinuxManager.Application.Security;
using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Infrastructure.SshNet.Persistence;
using RemoteLinuxManager.Infrastructure.SshNet.Services;
using System.Runtime.Versioning;

namespace RemoteLinuxManager.Infrastructure.SshNet.DependencyInjection;

public static class ServiceCollectionExtensions
{
    [SupportedOSPlatform("windows")]
    public static IServiceCollection AddRemoteLinuxManagerInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<SshNetRemoteSessionService>();
        services.AddSingleton<IRemoteSessionService>(provider => provider.GetRequiredService<SshNetRemoteSessionService>());
        services.AddSingleton<IFileTransferService, SshNetFileTransferService>();
        services.AddSingleton<IScriptExecutionService, SshNetScriptExecutionService>();
        services.AddSingleton<IRemoteFileBrowserService, SshNetRemoteFileBrowserService>();

        services.AddSingleton<AppDatabase>();
        services.AddSingleton<IHostProfileService, SqliteHostProfileService>();
        services.AddSingleton<ICredentialStore, SqliteCredentialStore>();

        return services;
    }
}
