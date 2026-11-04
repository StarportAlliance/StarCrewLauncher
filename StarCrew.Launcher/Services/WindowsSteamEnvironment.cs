using Microsoft.Win32;

namespace StarCrew.Launcher.Services;

/// <summary>适用于 Windows 的 Steam 定位所需的注册表与文件系统环境接口。</summary>
internal sealed class WindowsSteamEnvironment : ISteamEnvironment
{
    public IEnumerable<string?> GetRegistrySteamPaths()
    {
        yield return ReadRegistrySteamPath(RegistryHive.CurrentUser, @"Software\Valve\Steam");
        yield return ReadRegistrySteamPath(RegistryHive.LocalMachine, @"SOFTWARE\Valve\Steam");
        yield return ReadRegistrySteamPath(
            RegistryHive.LocalMachine,
            @"SOFTWARE\WOW6432Node\Valve\Steam"
        );
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public string? ReadAllTextOrNull(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return null;
        }
    }

    public string GetProgramFilesX86() =>
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    public string GetProgramFiles() =>
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

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

            return raw.Replace('/', Path.DirectorySeparatorChar);
        }
        catch
        {
            return null;
        }
    }
}
