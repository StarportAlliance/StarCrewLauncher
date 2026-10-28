namespace StarCrew.Launcher.Models;

/// <summary>要执行的外部程序调用。</summary>
internal sealed record ExecutableCommand(
    string FilePath,
    IReadOnlyList<string> Arguments,
    string? StandardInput,
    bool LogOutput = true
);
