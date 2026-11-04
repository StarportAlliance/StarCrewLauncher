namespace StarportAlliance.StarCrewLauncher.Models;

/// <summary>外部程序执行结果。</summary>
internal sealed record ExecutableOutcome(int ExitCode, IReadOnlyList<string> OutputLines);
