using System.Diagnostics;
using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Infrastructure.SshNet.Services;

public sealed class SshNetScriptExecutionService : IScriptExecutionService
{
    private readonly SshNetRemoteSessionService _sessionService;

    public SshNetScriptExecutionService(SshNetRemoteSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<ScriptExecutionResult> ExecuteAsync(
        ScriptExecutionRequest request,
        CancellationToken cancellationToken)
    {
        return request.InputType switch
        {
            ScriptInputType.LocalFile => await ExecuteLocalFileAsync(request, cancellationToken),
            ScriptInputType.InlineText => await ExecuteInlineScriptAsync(request, cancellationToken),
            _ => new ScriptExecutionResult
            {
                StandardOutput = string.Empty,
                StandardError = $"Unsupported input type: {request.InputType}",
                ExitCode = -1,
                Elapsed = TimeSpan.Zero
            }
        };
    }

    private async Task<ScriptExecutionResult> ExecuteLocalFileAsync(
        ScriptExecutionRequest request,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(request.Content))
        {
            return new ScriptExecutionResult
            {
                StandardOutput = string.Empty,
                StandardError = $"Script file not found: {request.Content}",
                ExitCode = -1,
                Elapsed = TimeSpan.Zero
            };
        }

        var sftpClient = _sessionService.GetRequiredSftpClient();
        var remoteScriptPath = BuildRemoteScriptPath(request.RemoteWorkingDirectory);

        await Task.Run(() =>
        {
            using var stream = File.OpenRead(request.Content);
            sftpClient.UploadFile(stream, remoteScriptPath);
        }, cancellationToken);

        var commandText =
            $"chmod +x {Quote(remoteScriptPath)} && bash {Quote(remoteScriptPath)}; code=$?; rm -f {Quote(remoteScriptPath)}; exit $code";

        return await RunCommandAsync(commandText, request.Timeout, cancellationToken);
    }

    private Task<ScriptExecutionResult> ExecuteInlineScriptAsync(
        ScriptExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var commandText =
            $"cd -- {Quote(request.RemoteWorkingDirectory)} && bash -lc {Quote(request.Content)}";

        return RunCommandAsync(commandText, request.Timeout, cancellationToken);
    }

    private async Task<ScriptExecutionResult> RunCommandAsync(
        string commandText,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var sshClient = _sessionService.GetRequiredSshClient();
        var command = sshClient.CreateCommand(commandText);
        command.CommandTimeout = timeout;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await Task.Run(command.Execute, cancellationToken);
            stopwatch.Stop();

            return new ScriptExecutionResult
            {
                StandardOutput = command.Result ?? string.Empty,
                StandardError = command.Error ?? string.Empty,
                ExitCode = command.ExitStatus ?? -1,
                Elapsed = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ScriptExecutionResult
            {
                StandardOutput = string.Empty,
                StandardError = ex.Message,
                ExitCode = -1,
                Elapsed = stopwatch.Elapsed
            };
        }
    }

    private static string BuildRemoteScriptPath(string remoteWorkingDirectory)
    {
        var normalizedDirectory = string.IsNullOrWhiteSpace(remoteWorkingDirectory)
            ? "."
            : remoteWorkingDirectory.TrimEnd('/');

        return $"{normalizedDirectory}/rlm-script-{Guid.NewGuid():N}.sh";
    }

    private static string Quote(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'")}'";
    }
}
