namespace StarCrew.Launcher.Models;

/// <summary>要执行的外部程序调用：可执行文件路径、参数与可选 stdin。</summary>
internal sealed record ExecutableCommand(
    string FilePath,
    IReadOnlyList<string> Arguments,
    string? StandardInput
);
