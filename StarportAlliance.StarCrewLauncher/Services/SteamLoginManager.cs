using System.Globalization;
using Microsoft.Extensions.Logging;
using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class SteamLoginManager : ISteamLoginManager
{
    private const int AmongUsAppId = 945360;
    private const int AmongUsDepotId = 945361;

    private readonly StarDepotDownloaderLocator _locator;
    private readonly IDepotDownloaderRunner _runner;
    private readonly IInstanceStore _store;
    private readonly IAppStrings _strings;
    private readonly ILogger<SteamLoginManager> _logger;

    public SteamLoginManager(
        StarDepotDownloaderLocator locator,
        IDepotDownloaderRunner runner,
        IInstanceStore store,
        IAppStrings strings,
        ILogger<SteamLoginManager> logger
    )
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        _locator = locator;
        _runner = runner;
        _store = store;
        _strings = strings;
        _logger = logger;
    }

    public async Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("开始二维码登录");

        string? toolPath = _locator.Find();
        if (toolPath is null)
        {
            _logger.LogWarning("二维码登录失败：未找到 StarDepotDownloader");
            return new SteamLoginOutcome(false, null, _strings.GetString("Sdd_Missing"));
        }

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempDir);

            List<string> arguments =
            [
                "-app",
                AmongUsAppId.ToString(CultureInfo.InvariantCulture),
                "-depot",
                AmongUsDepotId.ToString(CultureInfo.InvariantCulture),
                "-manifest-only",
                "-dir",
                tempDir,
                "-qr",
                "-remember-password",
            ];

            ExecutableOutcome outcome = await _runner.RunAsync(
                new ExecutableCommand(toolPath, arguments, null, LogOutput: false),
                output,
                cancellationToken
            );

            if (outcome.ExitCode != 0)
            {
                _logger.LogWarning("二维码登录失败，退出码 {ExitCode}", outcome.ExitCode);
                string? lastQr = outcome.OutputLines.LastOrDefault(l =>
                    !string.IsNullOrWhiteSpace(l)
                );
                return new SteamLoginOutcome(
                    false,
                    null,
                    lastQr ?? _strings.Format("Login_FailedExitCodeFormat", outcome.ExitCode)
                );
            }

            // QR 登录成功后 DD 会在输出里给出“下次用 -username 复用”的提示
            string? accountName = ExtractAccountName(outcome.OutputLines);
            if (accountName is null)
            {
                _logger.LogWarning("二维码登录成功但未能解析账号名");
                return new SteamLoginOutcome(
                    false,
                    null,
                    _strings.GetString("Login_AccountNameParseFailed")
                );
            }

            LauncherState state = _store.Load();
            _store.Save(state with { SteamAccountName = accountName });
            _logger.LogInformation(
                "二维码登录成功 {Account}",
                LogRedaction.MaskAccountName(accountName)
            );
            return new SteamLoginOutcome(true, accountName, null);
        }
        finally
        {
            TryDeleteTempDirectory(tempDir);
        }
    }

    private static string? ExtractAccountName(IReadOnlyList<string> lines)
    {
        foreach (string line in lines)
        {
            int idx = line.IndexOf("-username ", StringComparison.Ordinal);
            if (idx < 0)
            {
                continue;
            }

            ReadOnlySpan<char> rest = line.AsSpan(idx + "-username ".Length);
            int end = rest.IndexOfAny([' ', '\t', '"', '\r', '\n']);
            string candidate = (end < 0 ? rest : rest[..end]).ToString();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
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
