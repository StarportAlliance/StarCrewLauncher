using System.Diagnostics.CodeAnalysis;

namespace StarCrew.Launcher.Services;

/// <summary>DepotDownloader ASCII 二维码解析器。</summary>
internal static class QrAsciiParser
{
    private const char Dark = '█';

    public static bool TryParse(
        IReadOnlyList<string> lines,
        [NotNullWhen(true)] out bool[][]? matrix
    )
    {
        matrix = null;
        if (lines.Count < 21)
        {
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
