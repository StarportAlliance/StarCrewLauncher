using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

internal sealed partial class CustomBackgroundImageCardViewModel : ObservableObject
{
    private readonly AppearanceViewModel _owner;
    private int _thumbnailVersion;

    internal CustomBackgroundImageCardViewModel(AppearanceViewModel owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _owner = owner;
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        _ = UpdateThumbnailAsync();
    }

    public string DisplayName => _owner.BackgroundImageDisplayName;

    public bool HasCustomBackgroundImage => _owner.HasCustomBackgroundImage;

    public bool CanAddBackgroundImage => _owner.CanAddBackgroundImage;

    public bool IsSelected => _owner.IsCustomBackgroundImageSelected;

    public bool IsHighlighted => _owner.IsCustomBackgroundImageHighlighted;

    public ICommand SelectCommand => _owner.SelectCustomBackgroundImageCommand;

    public ICommand RemoveCommand => _owner.RemoveBackgroundImageCommand;

    public ICommand AddCommand => _owner.ChooseBackgroundImageCommand;

    [ObservableProperty]
    public partial BitmapImage? Thumbnail { get; set; }

    internal void Detach() => _owner.PropertyChanged -= OnOwnerPropertyChanged;

    private void OnOwnerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppearanceViewModel.BackgroundImageDisplayName):
                OnPropertyChanged(nameof(DisplayName));
                break;
            case nameof(AppearanceViewModel.HasCustomBackgroundImage):
                OnPropertyChanged(nameof(HasCustomBackgroundImage));
                _ = UpdateThumbnailAsync();
                break;
            case nameof(AppearanceViewModel.CanAddBackgroundImage):
                OnPropertyChanged(nameof(CanAddBackgroundImage));
                break;
            case nameof(AppearanceViewModel.IsCustomBackgroundImageSelected):
                OnPropertyChanged(nameof(IsSelected));
                break;
            case nameof(AppearanceViewModel.IsCustomBackgroundImageHighlighted):
                OnPropertyChanged(nameof(IsHighlighted));
                break;
            case nameof(AppearanceViewModel.BackgroundImagePath):
                _ = UpdateThumbnailAsync();
                break;
        }
    }

    private async Task UpdateThumbnailAsync()
    {
        int version = ++_thumbnailVersion;
        string? path = _owner.BackgroundImagePath;
        if (path is null)
        {
            Thumbnail = null;
            return;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            BitmapImage bitmap = new();
            await bitmap.SetSourceAsync(stream.AsRandomAccessStream());
            if (version == _thumbnailVersion)
            {
                Thumbnail = bitmap;
            }
        }
        catch (Exception)
        {
            // 缩略图失败时留空，不影响背景本身
            if (version == _thumbnailVersion)
            {
                Thumbnail = null;
            }
        }
    }
}
