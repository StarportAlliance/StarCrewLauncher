using System.Text.Json;
using System.Text.Json.Serialization;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class InstanceStore : IInstanceStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public InstanceStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public LauncherState Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return LauncherState.Empty;
            }

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<LauncherState>(json, Options) ?? LauncherState.Empty;
        }
        catch (Exception)
        {
            TryBackupCorruptFile();
            return LauncherState.Empty;
        }
    }

    public void Save(LauncherState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            string? directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(state, Options));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (IOException)
        {
            // 写入失败不影响当前会话
        }
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            File.Move(_filePath, _filePath + ".bak", overwrite: true);
        }
        catch (Exception)
        {
            // 备份失败也无所谓
        }
    }
}
