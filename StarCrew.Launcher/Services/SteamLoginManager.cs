using System.Globalization;
using Microsoft.Extensions.Logging;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class SteamLoginManager : ISteamLoginManager
{
    private const int AmongUsAppId = 945360;
    private const int AmongUsDepotId = 945361;

    private readonly DepotDownloaderLocator _locator;
    private readonly IDepotDownloaderRunner _runner;
    private readonly IInstanceStore _store;
    private readonly IAppStrings _strings;
    private readonly ILogger<SteamLoginManager> _logger;

    public SteamLoginManager(
        DepotDownloaderLocator locator,
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

    public async Task<SteamLoginOutcome> LoginAsync(
        string username,
        string password,
        string? steamGuardCode,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(password);

        _logger.LogInformation("开始账号登录 {Account}", LogRedaction.MaskAccountName(username));

        string? depotDownloaderPath = _locator.Find();
        if (depotDownloaderPath is null)
        {
            _logger.LogWarning("账号登录失败：未找到 DepotDownloader");
            return new SteamLoginOutcome(false, null, _strings.GetString("Dd_Missing"));
        }

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            List<string> arguments =
            [
                "-app",
                AmongUsAppId.ToString(CultureInfo.InvariantCulture),
                "-depot",
                AmongUsDepotId.ToString(CultureInfo.InvariantCulture),
                "-manifest-only",
                "-dir",
                tempDir,
                "-username",
                username,
                "-password",
                password,
                "-remember-password",
                "-no-mobile",
            ];

            string? stdin = steamGuardCode is null ? null : $"{steamGuardCode.Trim()}\n";
            ExecutableOutcome outcome = await _runner.RunAsync(
                new ExecutableCommand(depotDownloaderPath, arguments, stdin),
                null,
                cancellationToken
            );

            if (outcome.ExitCode == 0)
            {
                LauncherState state = _store.Load();
                _store.Save(state with { SteamAccountName = username });
                _logger.LogInformation(
                    "账号登录成功 {Account}",
                    LogRedaction.MaskAccountName(username)
                );
                return new SteamLoginOutcome(true, username, null);
            }

            _logger.LogWarning("账号登录失败，退出码 {ExitCode}", outcome.ExitCode);
            string? last = outcome.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
            return new SteamLoginOutcome(
                false,
                null,
                last ?? _strings.Format("Login_FailedExitCodeFormat", outcome.ExitCode)
            );
        }
        finally
        {
            TryDeleteTempDirectory(tempDir);
        }
    }

    public async Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("开始二维码登录");

        string? depotDownloaderPath = _locator.Find();
        if (depotDownloaderPath is null)
        {
            _logger.LogWarning("二维码登录失败：未找到 DepotDownloader");
            return new SteamLoginOutcome(false, null, _strings.GetString("Dd_Missing"));
        }

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
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
                new ExecutableCommand(depotDownloaderPath, arguments, null, LogOutput: false),
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
