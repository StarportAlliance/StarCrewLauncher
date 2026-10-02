namespace StarCrew.Launcher.Services;

/// <summary>Steam 定位所需的注册表与文件系统环境接缝。</summary>
internal interface ISteamEnvironment
{
    /// <summary>从注册表读取候选 Steam 主目录（可能含 null 或空白，调用方过滤）。</summary>
    IEnumerable<string?> GetRegistrySteamPaths();

    /// <summary>目录是否存在。</summary>
    bool DirectoryExists(string path);

    /// <summary>文件是否存在。</summary>
    bool FileExists(string path);

    /// <summary>读取文本文件，失败时返回 null 而不是抛异常。</summary>
    string? ReadAllTextOrNull(string path);

    /// <summary>Program Files (x86) 目录。</summary>
    string GetProgramFilesX86();

    /// <summary>Program Files 目录。</summary>
    string GetProgramFiles();
}
