namespace StarCrew.Launcher.Services;

/// <summary>字节数的格式化。</summary>
internal static class ByteFormat
{
    private const double Kilo = 1024;
    private const double Mega = Kilo * 1024;
    private const double Giga = Mega * 1024;

    /// <summary>格式化字节数为可读的字符串。</summary>
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

    public static string Rate(double bytesPerSecond) => $"{Describe(bytesPerSecond)}/s";
}
