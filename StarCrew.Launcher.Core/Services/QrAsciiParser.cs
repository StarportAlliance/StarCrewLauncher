using System.Diagnostics.CodeAnalysis;

namespace StarCrew.Launcher.Services;

/// <summary>
/// 解析 DepotDownloader 输出的 ASCII 二维码。格式为 QRCoder 的
/// <c>GetLineByLineGraphic(1)</c>：每模块两个字符（暗 "██"、亮 "  "），
/// 每行一个模块行，含四周 4 模块静区，整体为正方形。
/// </summary>
internal static class QrAsciiParser
{
    private const char Dark = '█';

    /// <summary>把一段连续输出行解析成模块矩阵（true 为暗模块）；格式不符时返回 false。</summary>
    public static bool TryParse(
        IReadOnlyList<string> lines,
        [NotNullWhen(true)] out bool[][]? matrix
    )
    {
        matrix = null;
        if (lines.Count < 21)
        {
            // QR 最小版本（1）也有 21 个模块，用于挡住恰好凑成方块的杂散输出。
            return false;
        }

        int side = lines.Count;
        foreach (string line in lines)
        {
            if (line.Length != side * 2)
            {
                return false;
            }

            foreach (char c in line)
            {
                if (c is not (Dark or ' '))
                {
                    return false;
                }
            }
        }

        matrix = new bool[side][];
        for (int y = 0; y < side; y++)
        {
            string line = lines[y];
            bool[] row = new bool[side];
            for (int x = 0; x < side; x++)
            {
                row[x] = line[x * 2] == Dark;
            }
            matrix[y] = row;
        }

        return true;
    }
}
