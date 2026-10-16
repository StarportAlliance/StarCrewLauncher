using System.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

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
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            ViewModel.RenameRequested += OnRenameRequested;
            ViewModel.DeleteRequested += OnDeleteRequested;
            ViewModel.DetailsRequested += OnDetailsRequested;
        }

        RebuildCards();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.RenameRequested -= OnRenameRequested;
            ViewModel.DeleteRequested -= OnDeleteRequested;
            ViewModel.DetailsRequested -= OnDetailsRequested;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (
            e.PropertyName
            is nameof(InstancesViewModel.Items)
                or nameof(InstancesViewModel.HasInstances)
        )
        {
            RebuildCards();
        }
    }

    private void RebuildCards()
    {
        InstancesList.Children.Clear();

        bool hasItems = ViewModel?.HasInstances ?? false;
        EmptyPanel.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
        InstancesList.Visibility = hasItems ? Visibility.Visible : Visibility.Collapsed;

        if (ViewModel is null)
        {
            return;
        }

        foreach (InstanceItemViewModel item in ViewModel.Items)
        {
            InstancesList.Children.Add(BuildCard(item));
        }
    }

    private SettingsCard BuildCard(InstanceItemViewModel item)
    {
        SettingsCard card = new() { Header = item.Name, Description = item.Summary };

        ImageIcon icon = new();
        try
        {
            icon.Source = new Microsoft.UI.Xaml.Media.Imaging.SvgImageSource(
                new Uri("ms-appx:///Assets/icon.svg")
            );
        }
        catch (Exception)
        {
            // 头像资源加载失败时用空图标兜底
        }

        card.HeaderIcon = icon;

        Button launchButton = new()
        {
            Command = item.LaunchCommand,
            Content = _strings.GetString("Instances_CardLaunch"),
            IsEnabled = item.CanLaunch,
        };
        try
        {
            launchButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        }
        catch (Exception)
        {
            // 资源不存在时退回默认按钮样式
        }

        Button openFolderButton = new()
        {
            Command = item.OpenFolderCommand,
            Content = _strings.GetString("Instances_CardOpenFolder"),
        };

        DropDownButton moreButton = new() { Content = _strings.GetString("Instances_CardMore") };
        MenuFlyout flyout = new();
        flyout.Items.Add(
            new MenuFlyoutItem
            {
                Text = _strings.GetString("Instances_CardRename"),
                Command = item.RequestRenameCommand,
            }
        );
        flyout.Items.Add(
            new MenuFlyoutItem
            {
                Text = _strings.GetString("Instances_CardDetails"),
                Command = item.RequestDetailsCommand,
            }
        );
        flyout.Items.Add(
            new MenuFlyoutItem
            {
                Text = _strings.GetString("Instances_CardDelete"),
                Command = item.RequestDeleteCommand,
            }
        );
        moreButton.Flyout = flyout;

        card.Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { launchButton, openFolderButton, moreButton },
        };

        return card;
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

        ViewModel.ConfirmDelete(instance, deleteFiles: deleteFilesBox.IsChecked == true);
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
