namespace StarCrew.Launcher.Services;

/// <summary>字节数的可读化：下载进度里显示大小与速率用，纯格式化无 IO。</summary>
internal static class ByteFormat
{
    private const double Kilo = 1024;
    private const double Mega = Kilo * 1024;
    private const double Giga = Mega * 1024;

    /// <summary>B/KB/MB/GB 按量级降到一两位小数，字号不变的方形区域正好放得下。</summary>
    public static string Describe(double bytes)
    {
        if (bytes >= Giga)
        {
            return $"{bytes / Giga:F2} GB";
        }

        if (bytes >= Mega)
        {
            return $"{bytes / Mega:F1} MB";
        }

        if (bytes >= Kilo)
        {
            return $"{bytes / Kilo:F0} KB";
        }

        return $"{bytes:F0} B";
    }

    /// <summary>速率：沿用 <see cref="Describe" /> 的单位，只在尾巴接 /s。</summary>
    public static string Rate(double bytesPerSecond) => $"{Describe(bytesPerSecond)}/s";
}
