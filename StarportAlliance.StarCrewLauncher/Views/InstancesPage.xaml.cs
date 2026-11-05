using CommunityToolkit.WinUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;
using StarportAlliance.StarCrewLauncher.ViewModels;

namespace StarportAlliance.StarCrewLauncher.Views;

public sealed partial class InstancesPage : Page
{
    private readonly IAppStrings _strings = App.Services.GetRequiredService<IAppStrings>();

    public InstancesPage()
    {
        InitializeComponent();
    }

    internal InstancesViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as InstancesViewModel;
        Bindings.Update();
        if (ViewModel is not null)
        {
            ViewModel.RenameRequested += OnRenameRequested;
            ViewModel.DeleteRequested += OnDeleteRequested;
            ViewModel.DetailsRequested += OnDetailsRequested;
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (ViewModel is not null)
        {
            ViewModel.RenameRequested -= OnRenameRequested;
            ViewModel.DeleteRequested -= OnDeleteRequested;
            ViewModel.DetailsRequested -= OnDetailsRequested;
        }
    }

    private async void OnRenameRequested(object? sender, GameInstance instance)
    {
        if (ViewModel is null)
        {
            return;
        }

        TextBox input = new() { Text = instance.Name };
        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.GetString("Instances_RenameTitle");
                d.Content = input;
                d.PrimaryButtonText = _strings.GetString("Common_Save");
                d.CloseButtonText = _strings.GetString("Common_Cancel");
                d.DefaultButton = ContentDialogButton.Primary;
            }
        );
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        ViewModel.ConfirmRename(instance, input.Text);
    }

    private async void OnDeleteRequested(object? sender, GameInstance instance)
    {
        if (ViewModel is null)
        {
            return;
        }

        CheckBox deleteFilesBox = new()
        {
            Content = _strings.GetString("Instances_DeleteFilesAlso"),
            IsChecked = true,
        };
        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.GetString("Instances_DeleteTitle");
                d.Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = _strings.Format("Instances_DeleteConfirmFormat", instance.Name),
                            TextWrapping = TextWrapping.Wrap,
                        },
                        deleteFilesBox,
                    },
                };
                d.PrimaryButtonText = _strings.GetString("Common_Delete");
                d.CloseButtonText = _strings.GetString("Common_Cancel");
                d.DefaultButton = ContentDialogButton.Close;
            }
        );
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.ConfirmDeleteAsync(instance, deleteFiles: deleteFilesBox.IsChecked == true);
    }

    private void OnDetailsRequested(object? sender, GameInstance instance)
    {
        if (ViewModel is null)
        {
            return;
        }

        string created = instance
            .CreatedAt.ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        string lastLaunched =
            instance
                .LastLaunchedAt?.ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture)
            ?? _strings.GetString("Instances_NeverLaunched");
        string stateText =
            instance.State == InstanceState.Ready
                ? _strings.GetString("Instances_StateReady")
                : _strings.GetString("Instances_StateNotReady");

        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.Format("Instances_DetailsTitleFormat", instance.Name);
                d.Content = new TextBlock
                {
                    Text = _strings.Format(
                        "Instances_DetailsFormat",
                        instance.GameVersionLabel,
                        stateText,
                        instance.InstallDirectory,
                        created,
                        lastLaunched
                    ),
                    TextWrapping = TextWrapping.Wrap,
                };
                d.CloseButtonText = _strings.GetString("Common_Ok");
            }
        );
        _ = dialog.ShowAsync();
    }
}
