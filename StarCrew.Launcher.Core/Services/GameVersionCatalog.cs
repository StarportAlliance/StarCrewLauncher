using System.Text.Json;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

internal sealed class GameVersionCatalog : IGameVersionCatalog
{
    private const string CatalogUrl = "https://asset.stcrew.app/versions.jsonc";

    private const string CacheFileName = "versions.jsonc";

    private static readonly Uri CatalogUri = new(CatalogUrl, UriKind.Absolute);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly HttpClient _http;
    private readonly string _cacheDirectory;
    private readonly IAppStrings _strings;
    private readonly IVersionCacheSettings _settings;

    public GameVersionCatalog(IAppStrings strings, IVersionCacheSettings settings)
        : this(new HttpClient(), AppPaths.CacheDirectory, strings, settings) { }

    internal GameVersionCatalog(
        HttpClient httpClient,
        string cacheDirectory,
        IAppStrings strings,
        IVersionCacheSettings settings
    )
    {
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(settings);
        _strings = strings;
        _http = httpClient;
        _cacheDirectory = cacheDirectory;
        _settings = settings;
    }

    public async Task<VersionCatalogLoadResult> LoadAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default
    )
    {
        bool cacheEnabled = _settings.MaxAgeHours > 0;
        VersionCatalogLoadResult? cached = cacheEnabled
            ? await TryReadCacheAsync(cancellationToken)
            : null;
        if (!forceRefresh && cached is not null && IsFresh(cached.WrittenAt))
        {
            return cached;
        }

        VersionCatalog? remote = await TryFetchRemoteAsync(cancellationToken);
        if (remote is not null)
        {
            return new VersionCatalogLoadResult(remote, DateTimeOffset.UtcNow);
        }

        if (cached is not null)
        {
            return cached;
        }

        throw new InvalidOperationException(_strings.GetString("Catalog_FetchFailed"));
    }

    public void ClearCache()
    {
        try
        {
            string cacheFile = Path.Combine(_cacheDirectory, CacheFileName);
            File.Delete(cacheFile);
            File.Delete(cacheFile + ".tmp");
        }
        catch (Exception)
        {
            // 删除失败时静默忽略，禁用缓存后同样不会再读取
        }
    }

    private bool IsFresh(DateTimeOffset writtenAt) =>
        DateTimeOffset.UtcNow - writtenAt <= TimeSpan.FromHours(_settings.MaxAgeHours);

    private async Task<VersionCatalog?> TryFetchRemoteAsync(CancellationToken cancellationToken)
    {
        try
        {
            string json = await _http.GetStringAsync(CatalogUri, cancellationToken);
            VersionCatalog? parsed = Parse(json);
            // 服务端返回空列表时不覆盖本地可用缓存；禁用缓存时也不落盘
            if (parsed is not null && parsed.Versions.Count > 0 && _settings.MaxAgeHours > 0)
            {
                await TryWriteCacheAsync(json, cancellationToken);
            }

            return parsed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<VersionCatalogLoadResult?> TryReadCacheAsync(
        CancellationToken cancellationToken
    )
    {
        try
        {
            string cacheFile = Path.Combine(_cacheDirectory, CacheFileName);
            if (!File.Exists(cacheFile))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(cacheFile, cancellationToken);
            VersionCatalog? parsed = Parse(json);
            if (parsed is null)
            {
                return null;
            }

            DateTime writtenAt = File.GetLastWriteTimeUtc(cacheFile);
            return new VersionCatalogLoadResult(
                parsed,
                new DateTimeOffset(writtenAt, TimeSpan.Zero)
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task TryWriteCacheAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_cacheDirectory);
            string cacheFile = Path.Combine(_cacheDirectory, CacheFileName);
            string tempFile = cacheFile + ".tmp";
            await File.WriteAllTextAsync(tempFile, json, cancellationToken);
            File.Move(tempFile, cacheFile, overwrite: true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 缓存写入失败时静默忽略，下次上网照常拉取
        }
    }

    private static VersionCatalog? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<VersionCatalog>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
