using Microsoft.Extensions.Logging;
using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class SteamLoginManager : ISteamLoginManager
{
    private readonly ISddClient _sdd;
    private readonly IInstanceStore _store;
    private readonly IAppStrings _strings;
    private readonly ILogger<SteamLoginManager> _logger;

    public SteamLoginManager(
        ISddClient sdd,
        IInstanceStore store,
        IAppStrings strings,
        ILogger<SteamLoginManager> logger
    )
    {
        ArgumentNullException.ThrowIfNull(sdd);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        _sdd = sdd;
        _store = store;
        _strings = strings;
        _logger = logger;
    }

    public async Task<SteamLoginOutcome> LoginWithQrAsync(
        IProgress<SddQrCode>? qr = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("开始二维码登录");

        try
        {
            SddLoginResult result = await _sdd.LoginWithQrAsync(qr, cancellationToken);
            if (!result.IsSuccess)
            {
                string message = result.ErrorMessage ?? _strings.GetString("Download_UnknownError");
                _logger.LogWarning("二维码登录失败：{Reason}", LogRedaction.Scrub(message));
                return new SteamLoginOutcome(false, null, message);
            }

            LauncherState state = _store.Load();
            _store.Save(state with { SteamAccountName = result.AccountName });
            _logger.LogInformation(
                "二维码登录成功 {Account}",
                LogRedaction.MaskAccountName(result.AccountName!)
            );
            return new SteamLoginOutcome(true, result.AccountName, null);
        }
        catch (OperationCanceledException)
        {
            return new SteamLoginOutcome(false, null, _strings.GetString("Download_Cancelled"));
        }
    }
}
