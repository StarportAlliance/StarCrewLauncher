namespace StarCrew.Launcher.Models;

/// <summary>Steam 上的一个 Among Us 原版版本条目（仅供展示，downloading 用 manifestId 精确安装）。</summary>
internal sealed record GameVersionEntry(
    string VersionLabel,
    long ManifestId,
    DateTimeOffset PublishedAt,
    IReadOnlyList<string> Tags,
    bool Unavailable
);
