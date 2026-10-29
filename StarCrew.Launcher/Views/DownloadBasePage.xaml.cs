using System.ComponentModel;
using System.Numerics;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

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
                Loaded += OnLoadedShowLoginHint;
            }
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel?.PropertyChanged -= OnViewModelPropertyChanged;
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

        StackPanel titleRow = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(
            new TextBlock
            {
                Text = entry.VersionLabel,
                VerticalAlignment = VerticalAlignment.Center,
            }
        );
        foreach (string tag in entry.Tags)
        {
            titleRow.Children.Add(
                new InfoBadge { Style = (Style)Resources["TagInfoBadgeStyle"], Tag = tag }
            );
        }

        TextBlock description = new()
        {
            Text = _strings.Format("DownloadBase_PublishedAtFormat", dateText),
            Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap,
        };
        try
        {
            description.Foreground = (Brush)
                Application.Current.Resources["TextFillColorSecondaryBrush"];
        }
        catch (Exception)
        {
            // 可选主题资源缺失时退回继承前景色
        }

        ThemeShadow iconShadow = new();
        Border icon = new()
        {
            Width = 40,
            Height = 40,
            CornerRadius = (CornerRadius)Application.Current.Resources["ControlCornerRadius"],
            Background = new ImageBrush
            {
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/AmongUs.png")),
            },
            Shadow = iconShadow,
            Translation = new Vector3(0, 0, 8),
        };

        SettingsCard card = new()
        {
            Header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    icon,
                    new StackPanel
                    {
                        Spacing = 2,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children = { titleRow, description },
                    },
                },
            },
            Padding = new Thickness(12),
            IsClickEnabled = true,
            ActionIcon = new FontIcon { Glyph = "\uE76C" },
            ActionIconToolTip = _strings.GetString("Common_Install"),
        };
        iconShadow.Receivers.Add(card);
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

    private void OnLoadedShowLoginHint(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        Loaded -= OnLoadedShowLoginHint;
        _ = ShowLoginHintAfterTransitionAsync();
    }

    private async Task ShowLoginHintAfterTransitionAsync()
    {
        await Task.Delay(400);
        if (ViewModel is not null && string.IsNullOrWhiteSpace(ViewModel.AccountName))
        {
            await ShowLoginHintAsync();
        }
    }

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
