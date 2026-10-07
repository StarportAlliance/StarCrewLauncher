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
}
