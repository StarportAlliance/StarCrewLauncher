using StarportAlliance.StarCrewLauncher.Models;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.DirectWrite;

namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class SystemFontCatalog : ISystemFontCatalog
{
    // 目前只有 zh-CN 资源，按“包含中文字形”过滤即可覆盖当前界面语言
    private const uint ChineseGlyph = 0x4E2D;

    public IReadOnlyList<FontOption> GetFonts()
    {
        List<FontOption> fonts = [];
        try
        {
            HRESULT result = PInvoke.DWriteCreateFactory(
                DWRITE_FACTORY_TYPE.DWRITE_FACTORY_TYPE_SHARED,
                out IDWriteFactory factory
            );
            result.ThrowOnFailure();

            factory.GetSystemFontCollection(out IDWriteFontCollection collection, true);
            uint count = collection.GetFontFamilyCount();
            for (uint index = 0; index < count; index++)
            {
                collection.GetFontFamily(index, out IDWriteFontFamily family);
                family.GetFamilyNames(out IDWriteLocalizedStrings names);
                string? localized = ReadName(names, "zh-cn") ?? ReadName(names, "en-us");
                string? original = ReadName(names, "en-us") ?? localized;
                if (string.IsNullOrWhiteSpace(localized) || string.IsNullOrWhiteSpace(original))
                {
                    continue;
                }

                family.GetFirstMatchingFont(
                    DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL,
                    DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL,
                    DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL,
                    out IDWriteFont font
                );
                font.HasCharacter(ChineseGlyph, out BOOL exists);
                if (!exists)
                {
                    continue;
                }

                string secondary = string.Equals(
                    localized,
                    original,
                    StringComparison.OrdinalIgnoreCase
                )
                    ? string.Empty
                    : original;
                fonts.Add(new FontOption(localized, secondary, original));
            }
        }
        catch (Exception)
        {
            // 字体枚举失败等同于没有可选的系统字体，界面退回默认字体
        }

        fonts.Sort(
            (left, right) =>
                StringComparer.CurrentCulture.Compare(left.DisplayName, right.DisplayName)
        );
        return fonts;
    }

    private static string? ReadName(IDWriteLocalizedStrings names, string locale)
    {
        names.FindLocaleName(locale, out uint index, out BOOL exists);
        if (!exists)
        {
            return null;
        }

        names.GetStringLength(index, out uint length);
        if (length == 0)
        {
            return null;
        }

        char[] buffer = new char[length + 1];
        names.GetString(index, buffer.AsSpan());
        return new string(buffer, 0, (int)length);
    }
}
