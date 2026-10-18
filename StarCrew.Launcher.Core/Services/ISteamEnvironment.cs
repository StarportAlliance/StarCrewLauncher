namespace StarCrew.Launcher.Services;

/// <summary>Steam 定位所需的注册表与文件系统环境接口。</summary>
internal interface ISteamEnvironment
{
    /// <summary>从注册表读取候选 Steam 主目录。</summary>
    /// <returns>候选 Steam 主目录，<b>可能为 null/空字符串</b>。</returns>
    IEnumerable<string?> GetRegistrySteamPaths();

    /// <summary>路径是否是已存在的目录。</summary>
    bool DirectoryExists(string path);

    /// <summary>路径是否是已存在的文件。</summary>
    bool FileExists(string path);

    /// <summary>
    /// 读取文本文件。
    /// </summary>
    /// <returns>文件内容，读取失败时返回 null。</returns>
    string? ReadAllTextOrNull(string path);

    /// <summary>Program Files (x86) 目录。</summary>
    string GetProgramFilesX86();

    /// <summary>Program Files 目录。</summary>
    string GetProgramFiles();
}
