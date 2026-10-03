using Microsoft.Win32;

namespace StarCrew.Launcher.Services;

/// <summary>基于真实注册表与文件系统的生产实现。</summary>
internal sealed class WindowsSteamEnvironment : ISteamEnvironment
{
    /// <inheritdoc />
    public IEnumerable<string?> GetRegistrySteamPaths()
    {
        yield return ReadRegistrySteamPath(RegistryHive.CurrentUser, @"Software\Valve\Steam");
        yield return ReadRegistrySteamPath(RegistryHive.LocalMachine, @"SOFTWARE\Valve\Steam");
        yield return ReadRegistrySteamPath(
            RegistryHive.LocalMachine,
            @"SOFTWARE\WOW6432Node\Valve\Steam"
        );
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path) => Directory.Exists(path);

    /// <inheritdoc />
    public bool FileExists(string path) => File.Exists(path);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string GetProgramFilesX86() =>
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    /// <inheritdoc />
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
