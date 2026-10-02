using System.Text.RegularExpressions;

namespace StarCrew.Launcher.Services;

/// <summary>在 Steam 安装目录与游戏库中定位 Among Us 可执行文件。</summary>
internal sealed partial class SteamGameLocator : ISteamGameLocator
{
    internal const int AmongUsAppId = 945360;
    private const string AmongUsRelativePath = @"steamapps\common\Among Us\Among Us.exe";

    private readonly ISteamEnvironment _environment;

    /// <summary>使用指定的环境接缝构造定位器。</summary>
    public SteamGameLocator(ISteamEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _environment = environment;
    }

    /// <summary>查找 Among Us.exe，找到返回完整路径，否则返回 null。</summary>
    public string? FindGameExe()
    {
        foreach (string library in EnumerateLibraries())
        {
            string candidate = Path.Combine(library, AmongUsRelativePath);
            if (_environment.FileExists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>枚举本机 Steam 库目录，主库优先。</summary>
    private IEnumerable<string> EnumerateLibraries()
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string steamDir in EnumerateSteamDirs())
        {
            if (seen.Add(steamDir))
            {
                yield return steamDir;
            }

            string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
            if (!_environment.FileExists(vdf))
            {
                continue;
            }

            foreach (string library in ParseLibraryPaths(vdf))
            {
                if (seen.Add(library))
                {
                    yield return library;
                }
            }
        }
    }

    /// <summary>枚举本机 Steam 主目录（注册表优先，默认路径兜底）。</summary>
    private IEnumerable<string> EnumerateSteamDirs()
    {
        List<string?> candidates = new List<string?>(_environment.GetRegistrySteamPaths());
        candidates.Add(Path.Combine(_environment.GetProgramFilesX86(), "Steam"));
        candidates.Add(Path.Combine(_environment.GetProgramFiles(), "Steam"));

        foreach (string? dir in candidates)
        {
            if (!string.IsNullOrWhiteSpace(dir) && _environment.DirectoryExists(dir))
            {
                yield return dir;
            }
        }
    }

    /// <summary>从 libraryfolders.vdf 中提取全部库路径（只取 path 项并校验目录存在）。</summary>
    private IEnumerable<string> ParseLibraryPaths(string vdfPath)
    {
        string? content = TryReadAllText(vdfPath);
        if (content is null)
        {
            yield break;
        }

        foreach (Match match in LibraryPathRegex().Matches(content))
        {
            string raw = match
                .Groups[1]
                .Value.Replace(@"\\", @"\", StringComparison.Ordinal)
                .Replace('/', Path.DirectorySeparatorChar);
            if (!string.IsNullOrWhiteSpace(raw) && _environment.DirectoryExists(raw))
            {
                yield return raw;
            }
        }
    }

    /// <summary>读取 VDF 文本，环境接缝抛异常时也视为无额外库而不上浮。</summary>
    private string? TryReadAllText(string vdfPath)
    {
        try
        {
            return _environment.ReadAllTextOrNull(vdfPath);
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex LibraryPathRegex();
}
