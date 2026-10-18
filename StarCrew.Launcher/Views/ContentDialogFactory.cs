using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StarCrew.Launcher.Views;

/// <summary>ContentDialog 工厂。</summary>
/// <remarks>
/// 代码创建的 Dialog 必须显式指定默认样式，否则模板不展开、入场动画丢失。<br />
/// 参阅 <see href="https://github.com/microsoft/microsoft-ui-xaml/issues/8476" />。
/// </remarks>
internal static class ContentDialogFactory
{
    /// <summary>创建已套默认样式并绑定 XamlRoot 的 Dialog。</summary>
    public static ContentDialog Create(XamlRoot xamlRoot, Action<ContentDialog>? configure = null)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"],
        };
        configure?.Invoke(dialog);
        return dialog;
    }
}
