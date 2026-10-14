using System.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Views;

public sealed partial class InstancesPage : Page
{
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

    private static SettingsCard BuildCard(InstanceItemViewModel item)
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
            Content = "启动",
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

        Button openFolderButton = new() { Command = item.OpenFolderCommand, Content = "打开目录" };

        DropDownButton moreButton = new() { Content = "更多" };
        MenuFlyout flyout = new();
        flyout.Items.Add(
            new MenuFlyoutItem { Text = "重命名", Command = item.RequestRenameCommand }
        );
        flyout.Items.Add(
            new MenuFlyoutItem { Text = "详情", Command = item.RequestDetailsCommand }
        );
        flyout.Items.Add(new MenuFlyoutItem { Text = "删除", Command = item.RequestDeleteCommand });
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
                d.Title = "重命名实例";
                d.Content = input;
                d.PrimaryButtonText = "保存";
                d.CloseButtonText = "取消";
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

        CheckBox deleteFilesBox = new() { Content = "同时删除游戏文件", IsChecked = true };
        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = "删除实例";
                d.Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Text =
                                $"确定要删除实例「{instance.Name}」吗？选中下方选项会同时删除该实例的游戏文件。",
                            TextWrapping = TextWrapping.Wrap,
                        },
                        deleteFilesBox,
                    },
                };
                d.PrimaryButtonText = "删除";
                d.CloseButtonText = "取消";
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
            ?? "从未启动";
        string stateText = instance.State == InstanceState.Ready ? "就绪" : "未完成";

        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = $"实例详情：{instance.Name}";
                d.Content = new TextBlock
                {
                    Text =
                        $"版本：v{instance.GameVersionLabel}\n状态：{stateText}\n安装目录：{instance.InstallDirectory}\n创建时间：{created}\n上次启动：{lastLaunched}",
                    TextWrapping = TextWrapping.Wrap,
                };
                d.CloseButtonText = "确定";
            }
        );
        _ = dialog.ShowAsync();
    }
}
