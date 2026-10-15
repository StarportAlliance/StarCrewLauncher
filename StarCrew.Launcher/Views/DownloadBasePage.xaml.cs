using System.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;
using Windows.System;

namespace StarCrew.Launcher.Views;

public sealed partial class DownloadBasePage : Page
{
    private readonly IAppStrings _strings = App.Services.GetRequiredService<IAppStrings>();

    public DownloadBasePage()
    {
        InitializeComponent();
    }

    internal DownloadPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel = e.Parameter as DownloadPageViewModel;
        Bindings.Update();

        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            RebuildVersionCards();
            if (string.IsNullOrWhiteSpace(ViewModel.AccountName))
            {
                // 每次进入都提示；等 Loaded 后再弹，保证 XamlRoot 可用。
                Loaded += OnLoadedShowLoginHint;
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (ViewModel is not null)
        {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _ = sender;
        if (e.PropertyName == nameof(DownloadPageViewModel.FilteredItems))
        {
            RebuildVersionCards();
        }
    }

    private void RebuildVersionCards()
    {
        VersionListStack.Children.Clear();
        if (ViewModel is null)
        {
            return;
        }

        foreach (GameVersionEntry entry in ViewModel.FilteredItems)
        {
            VersionListStack.Children.Add(BuildVersionCard(entry));
        }
    }

    private SettingsCard BuildVersionCard(GameVersionEntry entry)
    {
        DownloadPageViewModel viewModel = ViewModel!;
        string dateText = entry.PublishedAt.ToString(
            "yyyy-MM-dd HH:mm",
            System.Globalization.CultureInfo.InvariantCulture
        );

        // 版本名 + 标签徽章：徽章串在标题行右侧；暂不可用也留徽章，解释卡片为何点不动。
        StackPanel header = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(
            new TextBlock
            {
                Text = entry.VersionLabel,
                VerticalAlignment = VerticalAlignment.Center,
            }
        );
        if (entry.Unavailable)
        {
            header.Children.Add(
                new InfoBadge
                {
                    Style = (Style)Resources["TagInfoBadgeStyle"],
                    Tag = _strings.GetString("DownloadBase_UnavailableBadge"),
                }
            );
        }
        foreach (string tag in entry.Tags)
        {
            header.Children.Add(
                new InfoBadge { Style = (Style)Resources["TagInfoBadgeStyle"], Tag = tag }
            );
        }

        SettingsCard card = new()
        {
            Header = header,
            Description = _strings.Format("DownloadBase_PublishedAtFormat", dateText),
            // 整卡可点（与设置页关于卡片同款）：点击进入安装详情页，右侧 chevron 只作「可进入」的暗示。
            // 同一版本可以装多个实例，这里只挡暂不可用的版本。
            IsClickEnabled = true,
            IsEnabled = !entry.Unavailable,
        };
        card.ActionIcon = new FontIcon { Glyph = "\uE76C" };
        card.ActionIconToolTip = _strings.GetString("Common_Install");
        card.Click += (_, _) =>
        {
            viewModel.SelectedVersion = entry;
            viewModel.InstanceName = $"Among Us {entry.VersionLabel}";
            App.Services.GetRequiredService<FrameNavigationService>()
                .NavigateTo(
                    NavigationTarget.Install,
                    ViewModel,
                    new SlideNavigationTransitionInfo
                    {
                        Effect = SlideNavigationTransitionEffect.FromRight,
                    }
                );
        };

        return card;
    }

    // Esc 与点击空白处同一路径：焦点交给可接焦点的滚动区，输入框收起键盘焦点。
    // 用 Pointer 而非 Programmatic：后者在键盘输入后会被强制改写成 Keyboard，凭空画出焦点框（像按了次 Tab）。
    private void ContentScroll_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        _ = sender;
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            ContentScroll.Focus(FocusState.Pointer);
        }
    }

    private void OnLoadedShowLoginHint(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        Loaded -= OnLoadedShowLoginHint;
        _ = ShowLoginHintAfterTransitionAsync();
    }

    /// <summary>等外层导航转场（约 300ms）播完再弹，避免对话框顶掉页面切换动画。</summary>
    private async Task ShowLoginHintAfterTransitionAsync()
    {
        await Task.Delay(400);
        if (ViewModel is not null && string.IsNullOrWhiteSpace(ViewModel.AccountName))
        {
            await ShowLoginHintAsync();
        }
    }

    /// <summary>未登录提示；「前往登录」把下载页内部分区切到账号页。</summary>
    private async Task ShowLoginHintAsync()
    {
        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.GetString("DownloadBase_LoginHintTitle");
                d.Content = _strings.GetString("DownloadBase_LoginHintContent");
                d.PrimaryButtonText = _strings.GetString("DownloadBase_GoLogin");
                d.CloseButtonText = _strings.GetString("DownloadBase_Later");
                d.DefaultButton = ContentDialogButton.Primary;
            }
        );

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        // 分区帧挂在下载页里，沿视觉树找到宿主再切分区。
        DependencyObject? walker = this;
        while (walker is not null)
        {
            walker = VisualTreeHelper.GetParent(walker);
            if (walker is DownloadPage host)
            {
                host.ShowAccountSection();
                return;
            }
        }
    }
}
