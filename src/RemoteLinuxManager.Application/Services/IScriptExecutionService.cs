using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Application.Services;

public interface IScriptExecutionService
{
    Task<ScriptExecutionResult> ExecuteAsync(
        ScriptExecutionRequest request,
        CancellationToken cancellationToken);
}
