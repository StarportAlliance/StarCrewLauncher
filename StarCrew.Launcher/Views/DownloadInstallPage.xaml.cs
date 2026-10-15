using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;
using Windows.System;

namespace StarCrew.Launcher.Views;

public sealed partial class DownloadInstallPage : Page
{
    private readonly IAppStrings _strings = App.Services.GetRequiredService<IAppStrings>();

    public DownloadInstallPage()
    {
        InitializeComponent();
    }

    internal DownloadPageViewModel? ViewModel { get; private set; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        // 参数由版本列表传入：直接复用下载页视图模型，安装动作与校验只有一份实现。
        ViewModel = e.Parameter as DownloadPageViewModel;
        Bindings.Update();
    }

    private void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        _ = RunInstallAsync();
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

    /// <summary>开始安装：进度对话框跟踪下载，安装完成后退回版本列表。</summary>
    private async Task RunInstallAsync()
    {
        if (
            ViewModel is not { IsBusy: false } viewModel
            || viewModel.SelectedVersion is not GameVersionEntry entry
        )
        {
            return;
        }

        ProgressBar bar = new() { Minimum = 0, Maximum = 100 };
        TextBlock status = new() { Text = _strings.GetString("Account_PreparingDownload") };
        DownloadProgressFooter footer = new(_strings);
        ContentDialog progressDialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.Format("Install_DialogTitleFormat", entry.VersionLabel);
                d.Content = new StackPanel { Spacing = 8, Children = { status, bar, footer.Root } };
                d.CloseButtonText = _strings.GetString("Common_Cancel");
            }
        );

        using CancellationTokenSource cts = new();
        progressDialog.Closing += (_, _) => cts.Cancel();
        IProgress<DownloadProgressInfo> reporter = new Progress<DownloadProgressInfo>(info =>
        {
            bar.Value = info.Percent;
            status.Text = info.Message;
            footer.Apply(info);
        });

        Task<ContentDialogResult> showing = progressDialog.ShowAsync().AsTask();
        GameInstallResult result = await viewModel.InstallAsync(
            entry,
            viewModel.InstanceName,
            reporter,
            cts.Token
        );
        progressDialog.Hide();
        await showing;

        if (result.IsSuccess)
        {
            // 实例已落库，退回版本列表：页面重建、视图模型复用切走前那份（Frame 回退栈存的是同一实例），
            // 版本列表取的是远端目录快照，安装落库不改变它，无需刷新。
            App.Services.GetRequiredService<FrameNavigationService>().GoBack();
        }
    }
}
