using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StarportAlliance.StarCrewLauncher.ViewModels;

namespace StarportAlliance.StarCrewLauncher.Views;

internal sealed class BackgroundImageCardTemplateSelector : DataTemplateSelector
{
    public DataTemplate? BuiltInTemplate { get; set; }

    public DataTemplate? CustomTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item) =>
        item is BuiltInBackgroundOptionViewModel ? BuiltInTemplate : CustomTemplate;

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
