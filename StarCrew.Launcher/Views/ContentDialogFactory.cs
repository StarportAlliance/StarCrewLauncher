using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace StarCrew.Launcher.Views;

/// <summary>
/// ContentDialog 工厂：代码创建的对话框必须显式指定默认样式，否则模板不展开、入场动画丢失（WinUI #8476）。
/// </summary>
internal static class ContentDialogFactory
{
    /// <summary>创建已套默认样式并绑定 XamlRoot 的对话框；其余属性经 configure 设置。</summary>
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
