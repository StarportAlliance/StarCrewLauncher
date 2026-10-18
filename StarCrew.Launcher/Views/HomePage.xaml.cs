using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    internal HomeViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as HomeViewModel;
        Bindings.Update();
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            UpdateInstanceCombo();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel?.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.HasInstances))
        {
            UpdateInstanceCombo();
        }
    }

    private void UpdateInstanceCombo() =>
        InstanceCombo.Visibility =
            ViewModel?.HasInstances == true ? Visibility.Visible : Visibility.Collapsed;
}
