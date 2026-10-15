using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace StarCrew.Launcher.Services;

/// <summary>从 Strings/zh-CN/Resources.resw 取文案；键缺失直接抛错，尽早暴露拼写问题。</summary>
internal sealed class AppStrings : IAppStrings
{
    // 应用根下的 Strings/zh-CN/Resources.resw 对应默认的 "Resources" 映射，无参构造即取它。
    private readonly ResourceLoader _loader = new();

    public string GetString(string key)
    {
        string value = _loader.GetString(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"缺少文案资源：{key}");
        }

        return value;
    }

    public string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, GetString(key), args);
}
