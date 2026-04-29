namespace RemoteLinuxManager.Domain.Models;

public enum ScriptInputType
{
    LocalFile,
    InlineText
}

public sealed record ScriptExecutionRequest
{
    public ScriptInputType InputType { get; init; }

    public required string Content { get; init; }

    public string RemoteWorkingDirectory { get; init; } = ".";

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);
}

public sealed record ScriptExecutionResult
{
    public required string StandardOutput { get; init; }

    public required string StandardError { get; init; }

    public int ExitCode { get; init; }

    public TimeSpan Elapsed { get; init; }

    public bool Succeeded => ExitCode == 0;
}
