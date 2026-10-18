using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkPage()
    {
        InitializeComponent();
    }

    internal NetworkViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as NetworkViewModel;
        Bindings.Update();

        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateCustomCard();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel?.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NetworkViewModel.SelectedIndex))
        {
            UpdateCustomCard();
        }
    }

    private void UpdateCustomCard()
    {
        CustomPrefixCard?.Visibility =
            ViewModel?.CustomVisible == true ? Visibility.Visible : Visibility.Collapsed;
    }
}
