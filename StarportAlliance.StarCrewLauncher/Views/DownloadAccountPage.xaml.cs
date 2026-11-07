using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;
using StarportAlliance.StarCrewLauncher.ViewModels;

namespace StarportAlliance.StarCrewLauncher.Views;

public sealed partial class DownloadAccountPage : Page
{
    private readonly IAppStrings _strings = App.Services.GetRequiredService<IAppStrings>();

    public DownloadAccountPage()
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
            UpdateAccountToggle();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        ViewModel?.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e
    )
    {
        _ = sender;
        if (e.PropertyName == nameof(DownloadPageViewModel.AccountName))
        {
            UpdateAccountToggle();
        }
        else if (e.PropertyName == nameof(DownloadPageViewModel.IsBusy))
        {
            AccountToggle.IsEnabled = ViewModel is { IsBusy: false };
        }
    }

    private void UpdateAccountToggle()
    {
        bool loggedIn = !string.IsNullOrWhiteSpace(ViewModel?.AccountName);
        AccountToggleText.Text = _strings.GetString(
            loggedIn ? "Account_ToggleLogout" : "Account_ToggleLogin"
        );
        if (loggedIn)
        {
            AccountToggle.ClearValue(FrameworkElement.StyleProperty);
        }
        else
        {
            AccountToggle.Style = (Style)Resources["AccountButtonAccentStyle"];
        }
    }

    private async void AccountToggle_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        if (ViewModel is null || ViewModel.IsBusy)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(ViewModel.AccountName))
        {
            LogoutFlyout.ShowAt(AccountToggle);
            return;
        }

        await OnLoginButtonClickedAsync();
        UpdateAccountToggle();
    }

    private async void LogoutConfirm_Click(object sender, RoutedEventArgs e)
    {
        _ = sender;
        _ = e;
        LogoutFlyout.Hide();
        if (ViewModel is not null)
        {
            await ViewModel.LogoutAsync();
        }
        UpdateAccountToggle();
    }

    private void LogoutFlyout_Closed(object? sender, object e)
    {
        _ = sender;
        _ = e;
        UpdateAccountToggle();
    }

    private async Task OnLoginButtonClickedAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        Image qrImage = new()
        {
            Visibility = Visibility.Collapsed,
            MaxWidth = 208,
            MaxHeight = 208,
        };
        ProgressRing qrRing = new()
        {
            IsActive = true,
            Width = 40,
            Height = 40,
        };
        Grid qrHost = new();
        qrHost.Children.Add(qrImage);
        qrHost.Children.Add(qrRing);
        Border qrContainer = new()
        {
            Width = 240,
            Height = 240,
            Background = new SolidColorBrush(Colors.Transparent),
            Child = qrHost,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        TextBlock status = new()
        {
            Text = _strings.GetString("Account_FetchingQr"),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 280,
        };
        StackPanel panel = new() { Spacing = 12, HorizontalAlignment = HorizontalAlignment.Center };
        panel.Children.Add(status);
        panel.Children.Add(qrContainer);

        ContentDialog dialog = ContentDialogFactory.Create(
            XamlRoot,
            d =>
            {
                d.Title = _strings.GetString("Account_QrLoginTitle");
                d.Content = panel;
                d.CloseButtonText = _strings.GetString("Common_Cancel");
            }
        );

        IProgress<SddQrCode> reporter = new Progress<SddQrCode>(code =>
        {
            bool[][] matrix = ConvertMatrix(code);
            double raster = Math.Max(1.0, XamlRoot.RasterizationScale);
            int scale = Math.Max(2, (int)Math.Round(8 * raster));
            (WriteableBitmap bitmap, int pixelSize) = RenderMatrix(matrix, scale);
            qrImage.Source = bitmap;
            qrImage.Width = pixelSize / raster;
            qrImage.Height = pixelSize / raster;
            qrImage.Visibility = Visibility.Visible;
            qrRing.IsActive = false;
            qrRing.Visibility = Visibility.Collapsed;
            qrContainer.Background = new SolidColorBrush(Colors.White);
            status.Text = _strings.GetString("Account_ScanQrHint");
        });

        using CancellationTokenSource cts = new();
        dialog.Closing += (_, _) => cts.Cancel();
        Task<ContentDialogResult> showing = dialog.ShowAsync().AsTask();
        try
        {
            SteamLoginOutcome outcome = await ViewModel.LoginWithQrAsync(reporter, cts.Token);

            if (outcome.IsSuccess)
            {
                status.Text = _strings.Format("Account_LoginSuccessFormat", outcome.AccountName);
                await Task.Delay(800);
            }
            else
            {
                status.Text =
                    outcome.ErrorMessage ?? _strings.GetString("Account_LoginFailedRetry");
                await Task.Delay(1200);
            }
        }
        finally
        {
            dialog.Hide();
        }

        await showing;
    }

    private static bool[][] ConvertMatrix(SddQrCode code)
    {
        bool[][] matrix = new bool[code.Rows.Count][];
        for (int y = 0; y < matrix.Length; y++)
        {
            string row = code.Rows[y];
            bool[] line = new bool[row.Length];
            for (int x = 0; x < row.Length; x++)
            {
                line[x] = row[x] == '1';
            }

            matrix[y] = line;
        }

        return matrix;
    }

    /// <summary>将模块矩阵渲染成位图。</summary>
    private static (WriteableBitmap Bitmap, int PixelSize) RenderMatrix(bool[][] matrix, int scale)
    {
        const int quietZone = 4;
        int coreSide = Math.Max(1, matrix.Length);
        int size = (coreSide + quietZone * 2) * scale;
        WriteableBitmap bitmap = new(size, size);
        using Stream pixels = bitmap.PixelBuffer.AsStream();
        byte[] rowBytes = new byte[size * 4];
        for (int y = 0; y < coreSide; y++)
        {
            for (int x = 0; x < coreSide; x++)
            {
                byte channel = matrix[y][x] ? (byte)0 : byte.MaxValue;
                for (int s = 0; s < scale; s++)
                {
                    int offset = ((x + quietZone) * scale + s) * 4;
                    rowBytes[offset] = channel;
                    rowBytes[offset + 1] = channel;
                    rowBytes[offset + 2] = channel;
                    rowBytes[offset + 3] = byte.MaxValue;
                }
            }

            for (int s = 0; s < scale; s++)
            {
                pixels.Write(rowBytes, 0, rowBytes.Length);
            }
        }

        bitmap.Invalidate();
        return (bitmap, size);
    }
}
