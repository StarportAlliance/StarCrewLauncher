using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class InstanceStore : IInstanceStore
{
    private readonly string _filePath;
    private readonly ILogger<InstanceStore> _logger;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public InstanceStore(string filePath, ILogger<InstanceStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(logger);
        _filePath = filePath;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "实例状态文件 {Path} 解析失败，已备份并回退为空状态", _filePath);
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
            _logger.LogDebug("实例状态已保存：{Count} 个实例", state.Instances.Count);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "写入实例状态文件 {Path} 失败", _filePath);
        }
    }

    private void TryBackupCorruptFile()
    {
        try
        {
            File.Move(_filePath, _filePath + ".bak", overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "备份损坏的实例状态文件失败");
        }
    }
}
