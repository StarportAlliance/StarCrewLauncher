using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class DepotDownloaderProvisioner : IDepotDownloaderProvisioner
{
    private const string ReleaseTag = "DepotDownloader_3.4.0";
    private const string X64AssetName = "DepotDownloader-windows-x64.zip";
    private const string Arm64AssetName = "DepotDownloader-windows-arm64.zip";
    private const string X64Sha256 =
        "41C9E9F0DF54B3AD02E67A11726756E5C73283BD7C2E1B04ACFA5AE4C2ED3767";
    private const string Arm64Sha256 =
        "1449BA47775E9974036E615BEDF00D72BEF747CC5B93A78048ED4E0B2C63B2B3";

    private readonly INetworkSettings _networkSettings;
    private readonly HttpClient _http;
    private readonly string _toolsDirectory;
    private readonly string? _expectedSha256Override;
    private readonly IAppStrings _strings;

    public DepotDownloaderProvisioner(INetworkSettings networkSettings, IAppStrings strings)
        : this(networkSettings, new HttpClient(), strings) { }

    internal DepotDownloaderProvisioner(
        INetworkSettings networkSettings,
        HttpClient httpClient,
        IAppStrings strings
    )
        : this(
            networkSettings,
            httpClient,
            Path.Combine(AppPaths.ToolsDirectory, "DepotDownloader"),
            null,
            strings
        ) { }

    internal DepotDownloaderProvisioner(
        INetworkSettings networkSettings,
        HttpClient httpClient,
        string toolsDirectory,
        string? expectedSha256Override,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(networkSettings);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolsDirectory);
        _networkSettings = networkSettings;
        _strings = strings;
        _http = httpClient;
        _toolsDirectory = toolsDirectory;
        _expectedSha256Override = expectedSha256Override;
    }

    public bool IsInstalled() => File.Exists(GetExecutablePath());

    public async Task<DepotDownloaderEnsureResult> EnsureAsync(
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        string execPath = GetExecutablePath();
        if (File.Exists(execPath))
        {
            return new DepotDownloaderEnsureResult(true, _strings.GetString("Dd_Ready"), execPath);
        }

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);
            (string assetName, string platformSha) = SelectForCurrentPlatform();
            string expectedSha = _expectedSha256Override ?? platformSha;
            string url = BuildDownloadUrl(assetName);
            string zipPath = Path.Combine(tempDir, assetName);

            using (
                HttpResponseMessage response = await _http.GetAsync(
                    new Uri(url, UriKind.Absolute),
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken
                )
            )
            {
                response.EnsureSuccessStatusCode();
                long? contentLength = response.Content.Headers.ContentLength;
                long? total = contentLength is > 0 ? contentLength : null;
                progress?.Report(
                    new DownloadProgressInfo(
                        DownloadStage.PreparingTool,
                        0,
                        _strings.GetString("Dd_Downloading"),
                        total
                    )
                );
                await using (
                    Stream source = await response.Content.ReadAsStreamAsync(cancellationToken)
                )
                {
                    await using (FileStream destination = File.Create(zipPath))
                    {
                        if (total is null)
                        {
                            await source.CopyToAsync(destination, cancellationToken);
                        }
                        else
                        {
                            byte[] buffer = new byte[81920];
                            long received = 0;
                            int read;
                            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                            {
                                await destination.WriteAsync(
                                    buffer.AsMemory(0, read),
                                    cancellationToken
                                );
                                received += read;
                                double percent = Math.Clamp(received * 100.0 / total.Value, 0, 100);
                                progress?.Report(
                                    new DownloadProgressInfo(
                                        DownloadStage.PreparingTool,
                                        percent,
                                        _strings.Format("Dd_DownloadingPercentFormat", percent),
                                        total
                                    )
                                );
                            }
                        }
                    }
                }
            }

            string actualSha = await ComputeSha256Async(zipPath, cancellationToken);
            if (!string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase))
            {
                return new DepotDownloaderEnsureResult(
                    false,
                    _strings.GetString("Dd_ShaMismatch"),
                    null
                );
            }

            string extracted = Path.Combine(tempDir, "extracted");
            await ZipFile.ExtractToDirectoryAsync(
                zipPath,
                extracted,
                overwriteFiles: true,
                cancellationToken
            );
            string? exeSource = Directory
                .EnumerateFiles(extracted, "DepotDownloader.exe", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (exeSource is null)
            {
                return new DepotDownloaderEnsureResult(
                    false,
                    _strings.GetString("Dd_PackageIncomplete"),
                    null
                );
            }

            string destinationDir = Path.GetDirectoryName(execPath)!;
            Directory.CreateDirectory(destinationDir);
            File.Move(exeSource, execPath, overwrite: true);
            string? licenseSource = Directory
                .EnumerateFiles(extracted, "LICENSE", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (licenseSource is not null)
            {
                File.Move(licenseSource, Path.Combine(destinationDir, "LICENSE"), overwrite: true);
            }

            return new DepotDownloaderEnsureResult(
                true,
                _strings.GetString("Dd_DownloadedReady"),
                execPath
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DepotDownloaderEnsureResult(
                false,
                _strings.Format("Dd_DownloadFailedFormat", ex.Message),
                null
            );
        }
        finally
        {
            TryDeleteTempDirectory(tempDir);
        }
    }

    private string BuildDownloadUrl(string assetName)
    {
        string official =
            $"https://github.com/SteamRE/DepotDownloader/releases/download/{ReleaseTag}/{assetName}";
        return _networkSettings.Accelerator switch
        {
            DownloadAccelerator.GhProxy => $"https://gh-proxy.org/{official}",
            DownloadAccelerator.Custom => BuildCustomUrl(official),
            _ => official,
        };
    }

    private string BuildCustomUrl(string official)
    {
        string prefix = _networkSettings.CustomPrefix.Trim();
        if (prefix.Length == 0)
        {
            return official;
        }

        // 设置里的前缀可能是旧版本写入的非法值，回退官方直连而不是拼出坏地址。
        return HttpsPrefix.IsValid(prefix) ? $"{prefix.TrimEnd('/')}/{official}" : official;
    }

    private static (string AssetName, string Sha256) SelectForCurrentPlatform() =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.Arm64 => (Arm64AssetName, Arm64Sha256),
            _ => (X64AssetName, X64Sha256),
        };

    private string GetExecutablePath() => Path.Combine(_toolsDirectory, "DepotDownloader.exe");

    private static async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken
    )
    {
        await using FileStream stream = new(filePath, FileMode.Open, FileAccess.Read);
        byte[] data = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(data);
    }

    private static void TryDeleteTempDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception)
        {
            // 清理失败不影响主流程
        }
    }
}
