using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.ViewModels;

internal sealed class VersionItemViewModel
{
    public VersionItemViewModel(GameVersionEntry entry, string publishedText, string installToolTip)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
        PublishedText = publishedText;
        InstallToolTip = installToolTip;
    }

    public GameVersionEntry Entry { get; }

    public string VersionLabel => Entry.VersionLabel;

    public IReadOnlyList<string> Tags => Entry.Tags;

    public string PublishedText { get; }

    public string InstallToolTip { get; }
}
