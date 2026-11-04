using System.Text.RegularExpressions;

namespace StarCrew.Launcher.Services;

internal static partial class LogRedaction
{
    /// <summary>掩码账号名：长度 ≥6 保留前 3 后 2，1~5 保留首尾各 1，其余字符换成星号。</summary>
    public static string MaskAccountName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return name.Length switch
        {
            0 => name,
            <= 2 => new string('*', name.Length),
            >= 6 => string.Concat(
                name.AsSpan(0, 3),
                new string('*', name.Length - 5),
                name.AsSpan(name.Length - 2)
            ),
            _ => string.Concat(name[0], new string('*', name.Length - 2), name[^1]),
        };
    }

    /// <summary>抹掉输出行中 <c>-username</c> 后面的账号名。</summary>
    public static string Scrub(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        return AccountToken()
            .Replace(line, match => match.Groups[1].Value + MaskAccountName(match.Groups[2].Value));
    }

    [GeneratedRegex(@"(-username\s+)(\S+)", RegexOptions.CultureInvariant)]
    private static partial Regex AccountToken();
}
