using System.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace StarCrew.Launcher.Services;

internal sealed class AppStrings : IAppStrings
{
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
