namespace StarCrew.Launcher.Services;

/// <summary>Steam 定位所需的注册表与文件系统环境接缝。</summary>
internal interface ISteamEnvironment
{
    /// <summary>从注册表读取候选 Steam 主目录（含 null/空白，调用方过滤）。</summary>
    IEnumerable<string?> GetRegistrySteamPaths();

    bool DirectoryExists(string path);

    bool FileExists(string path);

    /// <summary>读文本文件，失败回 null（不抛）。</summary>
    string? ReadAllTextOrNull(string path);

    string GetProgramFilesX86();

    string GetProgramFiles();
}
