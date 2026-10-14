using System.Globalization;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>Steam 登录管理器：通过 DD 的 -remember-password 自动缓存会话。</summary>
internal sealed class SteamLoginManager : ISteamLoginManager
{
    private const int AmongUsAppId = 945360;
    private const int AmongUsDepotId = 945361;

    private const string NotInstalledMessage =
        "DepotDownloader 尚未安装，请先在「账号」页的「组件」中安装。";

    private readonly IDepotDownloaderProvisioner _provisioner;
    private readonly IDepotDownloaderRunner _runner;
    private readonly IInstanceStore _store;

    public SteamLoginManager(
        IDepotDownloaderProvisioner provisioner,
        IDepotDownloaderRunner runner,
        IInstanceStore store
    )
    {
        ArgumentNullException.ThrowIfNull(provisioner);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(store);
        _provisioner = provisioner;
        _runner = runner;
        _store = store;
    }

    /// <inheritdoc />
    public async Task<SteamLoginOutcome> LoginAsync(
        string username,
        string password,
        string? steamGuardCode,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentNullException.ThrowIfNull(password);

        if (!_provisioner.IsInstalled())
        {
            return new SteamLoginOutcome(false, null, NotInstalledMessage);
        }

        DepotDownloaderEnsureResult ensure = await _provisioner.EnsureAsync(
            null,
            cancellationToken
        );
        if (!ensure.IsSuccess || string.IsNullOrEmpty(ensure.ExecutablePath))
        {
            return new SteamLoginOutcome(false, null, ensure.Message);
        }

        // Manifest-only invocation: Just validate credentials and cache tokens.
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
                new ExecutableCommand(ensure.ExecutablePath, arguments, stdin),
                null,
                cancellationToken
            );

            if (outcome.ExitCode == 0)
            {
                LauncherState state = _store.Load();
                _store.Save(state with { SteamAccountName = username });
                return new SteamLoginOutcome(true, username, null);
            }

            string? last = outcome.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
            return new SteamLoginOutcome(
                false,
                null,
                last ?? $"登录失败，退出码 {outcome.ExitCode}。"
            );
        }
        finally
        {
            TryDeleteTempDirectory(tempDir);
        }
    }

    /// <inheritdoc />
    public async Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!_provisioner.IsInstalled())
        {
            return new SteamLoginOutcome(false, null, NotInstalledMessage);
        }

        DepotDownloaderEnsureResult ensure = await _provisioner.EnsureAsync(
            null,
            cancellationToken
        );
        if (!ensure.IsSuccess || string.IsNullOrEmpty(ensure.ExecutablePath))
        {
            return new SteamLoginOutcome(false, null, ensure.Message);
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
                new ExecutableCommand(ensure.ExecutablePath, arguments, null),
                output,
                cancellationToken
            );

            if (outcome.ExitCode != 0)
            {
                string? lastQr = outcome.OutputLines.LastOrDefault(l =>
                    !string.IsNullOrWhiteSpace(l)
                );
                return new SteamLoginOutcome(
                    false,
                    null,
                    lastQr ?? $"登录失败，退出码 {outcome.ExitCode}。"
                );
            }

            // QR 登录成功后 DD 会在输出里给出“下次用 -username 复用”的提示，从中解析账号名。
            string? accountName = ExtractAccountName(outcome.OutputLines);
            if (accountName is null)
            {
                return new SteamLoginOutcome(false, null, "登录成功但无法解析账号名。");
            }

            LauncherState state = _store.Load();
            _store.Save(state with { SteamAccountName = accountName });
            return new SteamLoginOutcome(true, accountName, null);
        }
        finally
        {
            TryDeleteTempDirectory(tempDir);
        }
    }

    /// <summary>从 DD 输出中提取 "-username X" 里的 X。</summary>
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
