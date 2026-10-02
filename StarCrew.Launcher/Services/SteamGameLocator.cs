using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace StarCrew.Launcher.Services;

/// <summary>在 Steam 安装目录与游戏库中定位 Among Us 可执行文件。</summary>
internal sealed partial class SteamGameLocator
{
    internal const int AmongUsAppId = 945360;
    private const string AmongUsRelativePath = @"steamapps\common\Among Us\Among Us.exe";

    /// <summary>查找 Among Us.exe，找到返回完整路径，否则返回 null。</summary>
    public string? FindGameExe()
    {
        foreach (string library in EnumerateLibraries())
        {
            string candidate = Path.Combine(library, AmongUsRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>枚举本机 Steam 库目录，主库优先。</summary>
    private static IEnumerable<string> EnumerateLibraries()
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string steamDir in EnumerateSteamDirs())
        {
            if (seen.Add(steamDir))
            {
                yield return steamDir;
            }

            string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
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
    private static IEnumerable<string> EnumerateSteamDirs()
    {
        foreach (
            string? dir in new[]
            {
                ReadRegistrySteamPath(RegistryHive.CurrentUser, @"Software\Valve\Steam"),
                ReadRegistrySteamPath(RegistryHive.LocalMachine, @"SOFTWARE\Valve\Steam"),
                ReadRegistrySteamPath(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\WOW6432Node\Valve\Steam"
                ),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Steam"
                ),
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Steam"
                ),
            }
        )
        {
            if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                yield return dir;
            }
        }
    }

    private static string? ReadRegistrySteamPath(RegistryHive hive, string subKey)
    {
        try
        {
            using RegistryKey? baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using RegistryKey? key = baseKey.OpenSubKey(subKey);
            string? raw = key?.GetValue("SteamPath") as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            // 注册表中的路径使用正斜杠，统一为本地分隔符。
            return raw.Replace('/', Path.DirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>从 libraryfolders.vdf 中提取全部库路径（只取 path 项并校验目录存在）。</summary>
    private static IEnumerable<string> ParseLibraryPaths(string vdfPath)
    {
        string content;
        try
        {
            content = File.ReadAllText(vdfPath);
        }
        catch
        {
            yield break;
        }

        foreach (Match match in LibraryPathRegex().Matches(content))
        {
            string raw = match
                .Groups[1]
                .Value.Replace(@"\\", @"\", StringComparison.Ordinal)
                .Replace('/', Path.DirectorySeparatorChar);
            if (!string.IsNullOrWhiteSpace(raw) && Directory.Exists(raw))
            {
                yield return raw;
            }
        }
    }

    [GeneratedRegex(@"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex LibraryPathRegex();
}
