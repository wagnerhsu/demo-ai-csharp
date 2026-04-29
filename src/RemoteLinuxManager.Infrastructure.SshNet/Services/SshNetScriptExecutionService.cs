using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
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
        var script = request.Content;
        byte[]? stdinData = null;

        if (!string.IsNullOrEmpty(request.SudoPassword))
        {
            var sudoCount = Regex.Matches(script, @"\bsudo\b").Count;
            if (sudoCount > 0)
            {
                script = Regex.Replace(script, @"\bsudo(?!\s+-S)\b", "sudo -S");
                var stdinContent = string.Concat(Enumerable.Repeat(request.SudoPassword + "\n", sudoCount));
                stdinData = Encoding.UTF8.GetBytes(stdinContent);
            }
        }

        var commandText = $"cd -- {Quote(request.RemoteWorkingDirectory)} && bash -lc {Quote(script)}";
        return RunCommandAsync(commandText, request.Timeout, cancellationToken, stdinData);
    }

    private async Task<ScriptExecutionResult> RunCommandAsync(
        string commandText,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        byte[]? stdinData = null)
    {
        var sshClient = _sessionService.GetRequiredSshClient();
        var command = sshClient.CreateCommand(commandText);
        command.CommandTimeout = timeout;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (stdinData != null)
            {
                var executeTask = command.ExecuteAsync(cancellationToken);
                using var stdin = command.CreateInputStream();
                await stdin.WriteAsync(stdinData, cancellationToken);
                stdin.Close();
                await executeTask;
            }
            else
            {
                await Task.Run(command.Execute, cancellationToken);
            }

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
