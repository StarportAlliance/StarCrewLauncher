namespace StarCrew.Launcher.Services;

/// <summary>自定义加速源前缀的 HTTPS 合法性判定。</summary>
internal static class HttpsPrefix
{
    /// <summary>是否为带主机名的 https 绝对地址（空串不算合法，需由调用方单独处理）。</summary>
    public static bool IsValid(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && !string.IsNullOrEmpty(uri.Host);
}
