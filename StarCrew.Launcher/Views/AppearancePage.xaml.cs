using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class AppearancePage : Page
{
    private HighContrastWatcher? _highContrast;
    private int _thumbnailVersion;

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
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            _ = UpdateCustomBackgroundThumbnailAsync();
        }

        _highContrast = App.Services.GetRequiredService<HighContrastWatcher>();
        _highContrast.Changed += OnHighContrastChanged;
        UpdateHighContrast();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_highContrast is not null)
        {
            _highContrast.Changed -= OnHighContrastChanged;
            _highContrast = null;
        }

        base.OnNavigatedFrom(e);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (
            e.PropertyName
            is nameof(AppearanceViewModel.BackgroundImagePath)
                or nameof(AppearanceViewModel.HasCustomBackgroundImage)
        )
        {
            _ = UpdateCustomBackgroundThumbnailAsync();
        }
    }

    private async Task UpdateCustomBackgroundThumbnailAsync()
    {
        int version = ++_thumbnailVersion;
        string? path = ViewModel?.BackgroundImagePath;
        if (path is null)
        {
            CustomBackgroundThumbnailBrush.ImageSource = null;
            return;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            BitmapImage bitmap = new();
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
            if (version == _thumbnailVersion)
            {
                CustomBackgroundThumbnailBrush.ImageSource = bitmap;
            }
        }
        catch (Exception)
        {
            // 缩略图失败时留空，不影响背景本身
            if (version == _thumbnailVersion)
            {
                CustomBackgroundThumbnailBrush.ImageSource = null;
            }
        }
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
