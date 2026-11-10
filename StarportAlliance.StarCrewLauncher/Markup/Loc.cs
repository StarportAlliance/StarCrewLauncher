using Microsoft.Extensions.DependencyInjection;

namespace StarportAlliance.StarCrewLauncher.Markup;

/// <summary>供 XAML 以 <c>{x:Bind i18n:Loc.Get('Key')}</c> 读取 resw 文案。</summary>
/// <remarks>x:Bind 默认 OneTime，语言切换需重启应用。</remarks>
internal static class Loc
{
    /// <summary>按 key 获取当前语言的文案。</summary>
    internal static string Get(string key) =>
        App.Services.GetRequiredService<IAppStrings>().GetString(key);
}
