namespace StarCrew.Launcher.Models;

/// <summary>字体族选项；<see cref="FamilyName"/> 为 <see langword="null"/> 表示跟随系统默认字体。</summary>
public sealed record FontOption(string DisplayName, string OriginalName, string? FamilyName)
{
    public bool IsDefault => FamilyName is null;
}
