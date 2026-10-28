using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class AppearancePage : Page
{
    public AppearancePage()
    {
        InitializeComponent();
    }

    internal AppearanceViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as AppearanceViewModel;
        Bindings.Update();
    }

    private void FolderCard_Click(object sender, RoutedEventArgs e) =>
        ViewModel?.OpenLogFolderCommand.Execute(null);

    // NumberBox 失焦时才把空输入判为 NaN，此时把显示值拉回已保存值
    private void RetentionBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is NumberBox box && double.IsNaN(box.Value) && ViewModel is not null)
        {
            box.Value = ViewModel.RetentionDays;
        }
    }
}
