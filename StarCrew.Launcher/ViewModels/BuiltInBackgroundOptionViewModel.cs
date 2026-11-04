using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.ViewModels;

public sealed partial class BuiltInBackgroundOptionViewModel : ObservableObject
{
    private readonly Action<BuiltInBackgroundOptionViewModel> _select;

    internal BuiltInBackgroundOptionViewModel(
        BuiltInBackground background,
        string displayName,
        Action<BuiltInBackgroundOptionViewModel> select
    )
    {
        ArgumentNullException.ThrowIfNull(background);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(select);
        Id = background.Id;
        DisplayName = displayName;
        ImageSource = background.ImageSource;
        _select = select;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string ImageSource { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsHighlighted { get; set; }

    [RelayCommand]
    private void Select() => _select(this);
}
