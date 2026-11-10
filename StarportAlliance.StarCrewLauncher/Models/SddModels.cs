namespace StarportAlliance.StarCrewLauncher.Models;

/// <summary>StarDepotDownloader 的机器可读事件与调用参数。</summary>
internal sealed record SddDepot(uint DepotId, ulong ManifestId);

/// <summary>下载开始事件，包含清单解析后的未压缩总大小。</summary>
internal sealed record SddStartInfo(IReadOnlyList<SddDepot> Depots, long TotalBytes);

/// <summary>下载进度，<paramref name="Percent" /> 为 0-100。</summary>
internal sealed record SddProgress(long Downloaded, long Total, double Percent);

/// <summary>二维码矩阵，<paramref name="Rows" /> 每行由 '0'/'1' 组成。</summary>
internal sealed record SddQrCode(int Size, IReadOnlyList<string> Rows);

/// <summary>本地缓存的登录凭据状态。</summary>
/// <param name="Checked">是否成功查询到状态；调用失败时为 false。</param>
internal sealed record SddAuthStatus(bool Checked, bool HasCredentials, string? AccountName);

/// <summary>一次下载调用所需的全部参数。</summary>
internal sealed record SddDownloadRequest(
    string AccountName,
    long ManifestId,
    string InstanceDirectory,
    string MetadataDirectory
);

internal sealed record SddLoginResult(
    bool IsSuccess,
    string? AccountName,
    string? ErrorCode,
    string? ErrorMessage
);

internal sealed record SddDownloadResult(
    bool IsSuccess,
    long? TotalBytes,
    int ExitCode,
    string? ErrorCode,
    string? ErrorMessage
);

internal sealed record SddLogoutResult(bool IsSuccess, string? ErrorCode, string? ErrorMessage);

internal static class SddErrorCode
{
    /// <summary>本地凭据不可用，需要用户重新扫码登录。</summary>
    public const string AuthRequired = "auth_required";

    public const string AuthFailed = "auth_failed";
    public const string DepotUnavailable = "depot_unavailable";
}
