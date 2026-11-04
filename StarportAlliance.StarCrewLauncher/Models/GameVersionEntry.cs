namespace StarportAlliance.StarCrewLauncher.Models;

/// <summary>Among Us 版本信息。</summary>
internal sealed record GameVersionEntry(
    string VersionLabel,
    long ManifestId,
    DateTimeOffset PublishedAt,
    IReadOnlyList<string> Tags
);
