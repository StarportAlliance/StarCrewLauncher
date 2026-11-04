using System.Globalization;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>强调色基色及其浅/深派生色。</summary>
internal readonly record struct AccentShades(
    AccentRgb Base,
    AccentRgb Light1,
    AccentRgb Light2,
    AccentRgb Light3,
    AccentRgb Dark1,
    AccentRgb Dark2,
    AccentRgb Dark3
);

/// <summary>sRGB 三通道值。</summary>
internal readonly record struct AccentRgb(byte R, byte G, byte B);

/// <summary>强调色的十六进制解析与浅/深色派生。</summary>
internal static class AccentPalette
{
    // 与 App.xaml 里 Light/Dark 主题字典的 SystemAccentColor 保持一致
    internal const string DefaultLight = "#1A83CB";
    internal const string DefaultDark = "#8ED1FF";

    private const double MinLightness = 0.04;
    private const double MaxLightness = 1.0;
    private static readonly double[] LightSteps = [0.10, 0.20, 0.30];
    private static readonly double[] DarkSteps = [0.10, 0.20, 0.30];

    /// <summary>解析 <c>#RRGGBB</c> 或 <c>RRGGBB</c>；失败返回 <see langword="false"/>。</summary>
    internal static bool TryParse(string? hex, out AccentRgb rgb)
    {
        rgb = default;
        if (string.IsNullOrWhiteSpace(hex))
        {
            return false;
        }

        string text = hex.Trim();
        if (text.StartsWith('#'))
        {
            text = text[1..];
        }

        if (
            text.Length != 6
            || !byte.TryParse(
                text[..2],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out byte r
            )
            || !byte.TryParse(
                text[2..4],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out byte g
            )
            || !byte.TryParse(
                text[4..],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out byte b
            )
        )
        {
            return false;
        }

        rgb = new AccentRgb(r, g, b);
        return true;
    }

    /// <summary>格式化为大写的 <c>#RRGGBB</c>。</summary>
    internal static string ToHex(AccentRgb rgb) =>
        FormattableString.Invariant($"#{rgb.R:X2}{rgb.G:X2}{rgb.B:X2}");

    /// <summary>规范化十六进制写法；无法解析时返回 <see langword="null"/>。</summary>
    internal static string? Normalize(string? hex) =>
        TryParse(hex, out AccentRgb rgb) ? ToHex(rgb) : null;

    /// <summary>由基色派生浅 1-3、深 1-3 两档色阶。</summary>
    internal static AccentShades Derive(string baseHex)
    {
        if (!TryParse(baseHex, out AccentRgb rgb))
        {
            throw new ArgumentException("强调色必须是 #RRGGBB 格式。", nameof(baseHex));
        }

        (double h, double s, double l) = ToHsl(rgb);
        return new AccentShades(
            rgb,
            FromHsl(h, s, Shift(l, LightSteps[0])),
            FromHsl(h, s, Shift(l, LightSteps[1])),
            FromHsl(h, s, Shift(l, LightSteps[2])),
            FromHsl(h, s, Shift(l, -DarkSteps[0])),
            FromHsl(h, s, Shift(l, -DarkSteps[1])),
            FromHsl(h, s, Shift(l, -DarkSteps[2]))
        );
    }

    private static double Shift(double lightness, double delta) =>
        Math.Clamp(lightness + delta, MinLightness, MaxLightness);

    private static (double H, double S, double L) ToHsl(AccentRgb rgb)
    {
        double r = rgb.R / 255.0;
        double g = rgb.G / 255.0;
        double b = rgb.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double l = (max + min) / 2;

        if (max == min)
        {
            return (0, 0, l);
        }

        double d = max - min;
        double s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        double h;
        if (max == r)
        {
            h = (g - b) / d + (g < b ? 6 : 0);
        }
        else if (max == g)
        {
            h = ((b - r) / d) + 2;
        }
        else
        {
            h = ((r - g) / d) + 4;
        }

        return (h / 6, s, l);
    }

    private static AccentRgb FromHsl(double h, double s, double l)
    {
        if (s == 0)
        {
            byte gray = ToByte(l);
            return new AccentRgb(gray, gray, gray);
        }

        double q = l < 0.5 ? l * (1 + s) : l + s - (l * s);
        double p = (2 * l) - q;
        return new AccentRgb(
            ToByte(Hue(p, q, h + (1.0 / 3))),
            ToByte(Hue(p, q, h)),
            ToByte(Hue(p, q, h - (1.0 / 3)))
        );
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0)
        {
            t += 1;
        }
        else if (t > 1)
        {
            t -= 1;
        }

        if (t < 1.0 / 6)
        {
            return p + ((q - p) * 6 * t);
        }

        if (t < 1.0 / 2)
        {
            return q;
        }

        if (t < 2.0 / 3)
        {
            return p + ((q - p) * ((2.0 / 3) - t) * 6);
        }

        return p;
    }

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value * 255, MidpointRounding.AwayFromZero), 0, 255);
}
