using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class DepotDownloaderProvisionerTests : IDisposable
{
    private readonly string _root;

    public DepotDownloaderProvisionerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureAsync_WhenExeExists_ReturnsSuccessWithoutNetwork()
    {
        string toolsDir = NewToolsDir();
        await File.WriteAllTextAsync(Path.Combine(toolsDir, "DepotDownloader.exe"), "x");
        using HttpClient http = CreateClient(_ =>
            throw new InvalidOperationException("不应发起网络请求")
        );
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            toolsDir,
            null,
            TestStrings.Instance
        );

        DepotDownloaderEnsureResult result = await provisioner.EnsureAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.Combine(toolsDir, "DepotDownloader.exe"), result.ExecutablePath);
    }

    [Fact]
    public void IsInstalled_WhenExeMissing_ReturnsFalse()
    {
        using HttpClient http = new();
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            NewToolsDir(),
            null,
            TestStrings.Instance
        );

        Assert.False(provisioner.IsInstalled());
    }

    [Fact]
    public async Task IsInstalled_WhenExeExists_ReturnsTrue()
    {
        string toolsDir = NewToolsDir();
        await File.WriteAllTextAsync(Path.Combine(toolsDir, "DepotDownloader.exe"), "x");
        using HttpClient http = new();
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            toolsDir,
            null,
            TestStrings.Instance
        );

        Assert.True(provisioner.IsInstalled());
    }

    [Fact]
    public async Task EnsureAsync_ReportsDownloadProgress()
    {
        byte[] zip = CreateZip();
        string sha = Convert.ToHexString(SHA256.HashData(zip));
        SyncProgress progress = new();
        using HttpClient http = CreateClient(_ => Ok(zip));
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            NewToolsDir(),
            sha,
            TestStrings.Instance
        );

        DepotDownloaderEnsureResult result = await provisioner.EnsureAsync(progress, default);

        Assert.True(result.IsSuccess);
        Assert.Contains(
            progress.Reports,
            info => info.Stage == DownloadStage.PreparingTool && info.Percent > 0
        );
        // 大小来自 Content-Length
        Assert.All(progress.Reports, info => Assert.Equal(zip.Length, info.TotalBytes));
    }

    [Fact]
    public async Task EnsureAsync_WithHttpError_ReturnsFailure()
    {
        using HttpClient http = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            NewToolsDir(),
            null,
            TestStrings.Instance
        );

        DepotDownloaderEnsureResult result = await provisioner.EnsureAsync();

        Assert.False(result.IsSuccess);
        Assert.Null(result.ExecutablePath);
        Assert.Contains("失败", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureAsync_WithShaMismatch_ReturnsFailure()
    {
        byte[] zip = CreateZip();
        using HttpClient http = CreateClient(_ => Ok(zip));
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            NewToolsDir(),
            null,
            TestStrings.Instance
        );

        DepotDownloaderEnsureResult result = await provisioner.EnsureAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("SHA-256", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureAsync_WithValidZip_ExtractsExeAndLicense()
    {
        byte[] zip = CreateZip();
        string sha = Convert.ToHexString(SHA256.HashData(zip));
        string toolsDir = NewToolsDir();
        using HttpClient http = CreateClient(_ => Ok(zip));
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            toolsDir,
            sha,
            TestStrings.Instance
        );

        DepotDownloaderEnsureResult result = await provisioner.EnsureAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.Combine(toolsDir, "DepotDownloader.exe"), result.ExecutablePath);
        Assert.True(File.Exists(Path.Combine(toolsDir, "DepotDownloader.exe")));
        Assert.True(File.Exists(Path.Combine(toolsDir, "LICENSE")));
    }

    [Theory]
    [InlineData(
        "Official",
        "",
        "https://github.com/SteamRE/DepotDownloader/releases/download/DepotDownloader_3.4.0/DepotDownloader-windows-"
    )]
    [InlineData(
        "GhProxy",
        "",
        "https://gh-proxy.org/https://github.com/SteamRE/DepotDownloader/releases/download/DepotDownloader_3.4.0/DepotDownloader-windows-"
    )]
    [InlineData(
        "Custom",
        "https://acc.example.com/",
        "https://acc.example.com/https://github.com/SteamRE/DepotDownloader/releases/download/DepotDownloader_3.4.0/DepotDownloader-windows-"
    )]
    [InlineData(
        "Custom",
        "",
        "https://github.com/SteamRE/DepotDownloader/releases/download/DepotDownloader_3.4.0/DepotDownloader-windows-"
    )]
    public async Task EnsureAsync_BuildsExpectedDownloadUrl(
        string acceleratorName,
        string prefix,
        string expectedStart
    )
    {
        DownloadAccelerator accelerator = Enum.Parse<DownloadAccelerator>(acceleratorName);
        string? captured = null;
        using HttpClient http = CreateClient(request =>
        {
            captured = request.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        DepotDownloaderProvisioner provisioner = new(
            SettingsWith(accelerator, prefix),
            http,
            NewToolsDir(),
            null,
            TestStrings.Instance
        );

        _ = await provisioner.EnsureAsync();

        Assert.NotNull(captured);
        Assert.StartsWith(expectedStart, captured, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EnsureAsync_WithCanceledToken_ThrowsOperationCanceled()
    {
        using HttpClient http = CreateClient(_ => Ok(CreateZip()));
        DepotDownloaderProvisioner provisioner = new(
            OfficialSettings(),
            http,
            NewToolsDir(),
            null,
            TestStrings.Instance
        );
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provisioner.EnsureAsync(default, cts.Token)
        );
    }

    private string NewToolsDir()
    {
        string dir = Path.Combine(_root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static INetworkSettings OfficialSettings() =>
        SettingsWith(DownloadAccelerator.Official, string.Empty);

    private static INetworkSettings SettingsWith(DownloadAccelerator accelerator, string prefix)
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        settings.Accelerator.Returns(accelerator);
        settings.CustomPrefix.Returns(prefix);
        return settings;
    }

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new FakeHandler(respond)) { BaseAddress = new Uri("https://localhost/") };

    private static HttpResponseMessage Ok(byte[] body) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(body) };

    // 同步上报
    private sealed class SyncProgress : IProgress<DownloadProgressInfo>
    {
        public List<DownloadProgressInfo> Reports { get; } = [];

        public void Report(DownloadProgressInfo value) => Reports.Add(value);
    }

    private static byte[] CreateZip()
    {
        using MemoryStream stream = new();
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (StreamWriter exe = new(archive.CreateEntry("DepotDownloader.exe").Open()))
            {
                exe.Write("fake-exe");
            }

            using StreamWriter license = new(archive.CreateEntry("LICENSE").Open());
            license.Write("fake-license");
        }

        return stream.ToArray();
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_respond(request));
        }
    }
}
