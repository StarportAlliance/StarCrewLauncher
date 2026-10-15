using System.Globalization;
using System.Xml.Linq;
using StarCrew.Launcher;

namespace StarCrew.Launcher.Tests;

/// <summary>测试用文案源：直接解析仓库内 Resources.resw，让断言与文案保持单一来源一致；键缺失即抛错。</summary>
internal sealed class TestStrings : IAppStrings
{
    internal static readonly TestStrings Instance = new();

    private readonly Dictionary<string, string> _values;

    internal TestStrings()
    {
        _values = XDocument
            .Load(FindReswPath())
            .Root!.Elements("data")
            .ToDictionary(d => d.Attribute("name")!.Value, d => d.Element("value")!.Value);
    }

    public string GetString(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _values.TryGetValue(key, out string? value)
            ? value
            : throw new InvalidOperationException($"缺少文案资源：{key}");
    }

    public string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, GetString(key), args);

    // 从测试输出目录逐级上溯，定位 Launcher 项目里的 resw。
    private static string FindReswPath()
    {
        for (
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            dir is not null;
            dir = dir.Parent
        )
        {
            string candidate = Path.Combine(
                dir.FullName,
                "StarCrew.Launcher",
                "Strings",
                "zh-CN",
                "Resources.resw"
            );
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("未找到 Resources.resw，请确认仓库结构未被改动。");
    }
}
