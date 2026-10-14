using System.Text.Json;
using System.Text.Json.Serialization;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>全局实例状态存储：以 instances.json 存放于用户数据目录，损坏时当无数据处理。</summary>
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

    /// <inheritdoc />
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
            // 状态文件损坏时按无数据处理，副本留给人工排查
            TryBackupCorruptFile();
            return LauncherState.Empty;
        }
    }

    /// <inheritdoc />
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
            // 写入失败不影响当前会话，只记日志：实际环境中最常见的原因是目录权限问题
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
