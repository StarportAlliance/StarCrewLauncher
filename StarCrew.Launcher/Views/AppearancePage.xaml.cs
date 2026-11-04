using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class AppearancePage : Page
{
    private HighContrastWatcher? _highContrast;
    private CustomBackgroundImageCardViewModel? _customCard;

    public AppearancePage()
    {
        InitializeComponent();
    }

    internal AppearanceViewModel? ViewModel { get; private set; }

    public IReadOnlyList<object> BackgroundImageCards { get; private set; } = [];

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as AppearanceViewModel;
        if (ViewModel is not null)
        {
            _customCard = new CustomBackgroundImageCardViewModel(ViewModel);
            BackgroundImageCards = [.. ViewModel.BuiltInBackgrounds, _customCard];
        }

        Bindings.Update();

        _highContrast = App.Services.GetRequiredService<HighContrastWatcher>();
        _highContrast.Changed += OnHighContrastChanged;
        UpdateHighContrast();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        _customCard?.Detach();
        _customCard = null;

        if (_highContrast is not null)
        {
            _highContrast.Changed -= OnHighContrastChanged;
            _highContrast = null;
        }

        base.OnNavigatedFrom(e);
    }

    private void OnHighContrastChanged(object? sender, EventArgs e)
    {
        _ = sender;
        _ = e;
        UpdateHighContrast();
    }

    private void UpdateHighContrast()
    {
        bool highContrast = _highContrast?.IsEnabled == true;
        AccentHighContrastBar.IsOpen = highContrast;
        // 关闭状态的 InfoBar 仍会占用外边距，必须连可见性一起收起
        AccentHighContrastBar.Visibility = highContrast ? Visibility.Visible : Visibility.Collapsed;
        AccentLightCard.IsEnabled = !highContrast;
        AccentDarkCard.IsEnabled = !highContrast;
    }
}
