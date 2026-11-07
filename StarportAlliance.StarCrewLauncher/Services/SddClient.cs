using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

/// <summary>通过 <c>--json</c> NDJSON 协议调用 StarDepotDownloader。</summary>
internal sealed class SddClient : ISddClient
{
    private const int AmongUsAppId = 945360;
    private const int AmongUsDepotId = 945361;

    private readonly StarDepotDownloaderLocator _locator;
    private readonly IDepotDownloaderRunner _runner;
    private readonly IAppStrings _strings;
    private readonly ILogger<SddClient> _logger;

    public SddClient(
        StarDepotDownloaderLocator locator,
        IDepotDownloaderRunner runner,
        IAppStrings strings,
        ILogger<SddClient> logger
    )
    {
        ArgumentNullException.ThrowIfNull(locator);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        _locator = locator;
        _runner = runner;
        _strings = strings;
        _logger = logger;
    }

    public async Task<SddAuthStatus> GetAuthStatusAsync(
        string? accountName,
        CancellationToken cancellationToken = default
    )
    {
        string? toolPath = _locator.Find();
        if (toolPath is null)
        {
            _logger.LogWarning("无法查询登录状态：未找到 StarDepotDownloader");
            return new SddAuthStatus(false, false, null);
        }

        List<string> arguments =
        [
            "-auth-status",
            "--json",
            "-account-file",
            AppPaths.SddAccountFile,
        ];
        if (!string.IsNullOrWhiteSpace(accountName))
        {
            arguments.Add("-username");
            arguments.Add(accountName);
        }

        ExecutableOutcome outcome = await _runner.RunAsync(
            new ExecutableCommand(toolPath, arguments, null, LogOutput: false),
            null,
            cancellationToken
        );

        if (outcome.ExitCode != 0)
        {
            _logger.LogWarning("查询登录状态失败，退出码 {ExitCode}", outcome.ExitCode);
            return new SddAuthStatus(false, false, null);
        }

        foreach (SddEvent item in ParseEvents(outcome.OutputLines))
        {
            if (item.Name != "status")
            {
                continue;
            }

            return new SddAuthStatus(
                true,
                ReadBoolean(item.Payload, "hasCredentials"),
                ReadString(item.Payload, "account")
            );
        }

        return new SddAuthStatus(false, false, null);
    }

    public async Task<SddLoginResult> LoginWithQrAsync(
        IProgress<SddQrCode>? qr,
        CancellationToken cancellationToken = default
    )
    {
        string? toolPath = _locator.Find();
        if (toolPath is null)
        {
            return new SddLoginResult(
                false,
                null,
                SddErrorCode.ToolMissing,
                _strings.GetString("Sdd_Missing")
            );
        }

        List<string> arguments =
        [
            "-login-only",
            "-qr",
            "-remember-password",
            "--json",
            "-account-file",
            AppPaths.SddAccountFile,
        ];

        ExecutableOutcome outcome = await _runner.RunAsync(
            new ExecutableCommand(toolPath, arguments, null, LogOutput: false),
            null,
            cancellationToken
        );

        string? accountName = null;
        SddFailure? failure = null;

        foreach (SddEvent item in ParseEvents(outcome.OutputLines))
        {
            switch (item.Name)
            {
                case "qr":
                    qr?.Report(ReadQrCode(item.Payload));
                    break;
                case "auth":
                    accountName = ReadString(item.Payload, "account");
                    break;
                case "error":
                    failure = ReadFailure(item.Payload);
                    break;
            }
        }

        if (failure is not null)
        {
            return new SddLoginResult(false, null, failure.Code, failure.Message);
        }

        if (outcome.ExitCode != 0)
        {
            return new SddLoginResult(false, null, "sdd_failed", FailureText(outcome));
        }

        if (string.IsNullOrWhiteSpace(accountName))
        {
            return new SddLoginResult(
                false,
                null,
                "sdd_failed",
                _strings.GetString("Login_AccountNameParseFailed")
            );
        }

        _logger.LogInformation("扫码登录成功 {Account}", LogRedaction.MaskAccountName(accountName));
        return new SddLoginResult(true, accountName, null, null);
    }

    public async Task<SddLogoutResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        string? toolPath = _locator.Find();
        if (toolPath is null)
        {
            TryDeleteAccountFile();
            return new SddLogoutResult(true, null, null);
        }

        List<string> arguments = ["-logout", "--json", "-account-file", AppPaths.SddAccountFile];

        ExecutableOutcome outcome = await _runner.RunAsync(
            new ExecutableCommand(toolPath, arguments, null, LogOutput: false),
            null,
            cancellationToken
        );

        TryDeleteAccountFile();

        if (outcome.ExitCode != 0)
        {
            SddFailure? failure = FindFailure(outcome);
            return new SddLogoutResult(
                false,
                failure?.Code ?? "sdd_failed",
                failure?.Message ?? FailureText(outcome)
            );
        }

        return new SddLogoutResult(true, null, null);
    }

    public async Task<SddDownloadResult> DownloadAsync(
        SddDownloadRequest request,
        IProgress<SddProgress>? progress,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        string? toolPath = _locator.Find();
        if (toolPath is null)
        {
            return new SddDownloadResult(
                false,
                null,
                -1,
                SddErrorCode.ToolMissing,
                _strings.GetString("Sdd_Missing")
            );
        }

        List<string> arguments =
        [
            "--json",
            "-account-file",
            AppPaths.SddAccountFile,
            "-loginid",
            RandomNumberGenerator.GetInt32(1, int.MaxValue).ToString(CultureInfo.InvariantCulture),
            "-app",
            AmongUsAppId.ToString(CultureInfo.InvariantCulture),
            "-depot",
            AmongUsDepotId.ToString(CultureInfo.InvariantCulture),
            "-manifest",
            request.ManifestId.ToString(CultureInfo.InvariantCulture),
            "-dir",
            request.InstanceDirectory,
            "-config-dir",
            request.MetadataDirectory,
            "-username",
            request.AccountName,
            "-remember-password",
        ];

        _logger.LogInformation(
            "调用 StarDepotDownloader 下载清单 {Manifest}（账号 {Account}）",
            request.ManifestId,
            LogRedaction.MaskAccountName(request.AccountName)
        );

        ExecutableOutcome outcome = await _runner.RunAsync(
            new ExecutableCommand(toolPath, arguments, null),
            null,
            cancellationToken
        );

        long? totalBytes = null;
        SddFailure? failure = null;

        foreach (SddEvent item in ParseEvents(outcome.OutputLines))
        {
            switch (item.Name)
            {
                case "start":
                    totalBytes = ReadInt64(item.Payload, "totalBytes");
                    progress?.Report(new SddProgress(0, totalBytes.Value, 0));
                    break;
                case "progress":
                    progress?.Report(
                        new SddProgress(
                            ReadInt64(item.Payload, "downloaded"),
                            ReadInt64(item.Payload, "total"),
                            ReadDouble(item.Payload, "percent")
                        )
                    );
                    break;
                case "error":
                    failure = ReadFailure(item.Payload);
                    break;
            }
        }

        if (failure is not null)
        {
            return new SddDownloadResult(
                false,
                totalBytes,
                outcome.ExitCode,
                failure.Code,
                failure.Message
            );
        }

        if (outcome.ExitCode != 0)
        {
            return new SddDownloadResult(
                false,
                totalBytes,
                outcome.ExitCode,
                "sdd_failed",
                FailureText(outcome)
            );
        }

        return new SddDownloadResult(true, totalBytes, 0, null, null);
    }

    private static SddQrCode ReadQrCode(JsonElement payload)
    {
        List<string> rows = [];
        if (
            payload.TryGetProperty("rows", out JsonElement rowsElement)
            && rowsElement.ValueKind == JsonValueKind.Array
        )
        {
            foreach (JsonElement row in rowsElement.EnumerateArray())
            {
                if (row.ValueKind == JsonValueKind.String && row.GetString() is { } text)
                {
                    rows.Add(text);
                }
            }
        }

        int size = (int)ReadInt64(payload, "size");
        return new SddQrCode(size > 0 ? size : rows.Count, rows);
    }

    private static SddFailure? FindFailure(ExecutableOutcome outcome)
    {
        foreach (SddEvent item in ParseEvents(outcome.OutputLines))
        {
            if (item.Name == "error")
            {
                return ReadFailure(item.Payload);
            }
        }

        return null;
    }

    private static SddFailure ReadFailure(JsonElement payload) =>
        new(
            ReadString(payload, "code") ?? "sdd_failed",
            ReadString(payload, "message") ?? string.Empty
        );

    private static string FailureText(ExecutableOutcome outcome)
    {
        string? line =
            outcome.ErrorLines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))
            ?? outcome.OutputLines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l));
        return line ?? $"StarDepotDownloader 退出码 {outcome.ExitCode}";
    }

    private static List<SddEvent> ParseEvents(IReadOnlyList<string> lines)
    {
        List<SddEvent> events = [];
        foreach (string line in lines)
        {
            if (line.Length == 0 || line[0] != '{')
            {
                continue;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(line);
                JsonElement root = document.RootElement;
                if (
                    root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("event", out JsonElement nameElement)
                    || nameElement.ValueKind != JsonValueKind.String
                )
                {
                    continue;
                }

                string? name = nameElement.GetString();
                if (name is not null)
                {
                    events.Add(new SddEvent(name, root.Clone()));
                }
            }
            catch (JsonException)
            {
                // 非 JSON 行（如 trace 输出）直接忽略
            }
        }

        return events;
    }

    private static bool ReadBoolean(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out JsonElement element)
        && element.ValueKind is JsonValueKind.True or JsonValueKind.False
        && element.GetBoolean();

    private static long ReadInt64(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out JsonElement element)
        && element.ValueKind == JsonValueKind.Number
        && element.TryGetInt64(out long value)
            ? value
            : 0;

    private static double ReadDouble(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out JsonElement element)
        && element.ValueKind == JsonValueKind.Number
            ? element.GetDouble()
            : 0;

    private static string? ReadString(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out JsonElement element)
        && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    private static void TryDeleteAccountFile()
    {
        try
        {
            if (File.Exists(AppPaths.SddAccountFile))
            {
                File.Delete(AppPaths.SddAccountFile);
            }
        }
        catch (IOException)
        {
            // 删不掉不影响内存中的登出状态
        }
        catch (UnauthorizedAccessException)
        {
            // 同上
        }
    }

    private sealed record SddEvent(string Name, JsonElement Payload);

    private sealed record SddFailure(string Code, string Message);
}
