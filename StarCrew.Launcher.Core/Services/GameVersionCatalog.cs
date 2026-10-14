using System.Text.Json;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>版本目录读取器：远端 JSON（asset.stcrew.app）→ 本地缓存，均不可用时抛异常由调用方提示。</summary>
internal sealed class GameVersionCatalog : IGameVersionCatalog
{
    private const string CatalogUrl = "https://asset.stcrew.app/versions.json";

    private static readonly Uri CatalogUri = new(CatalogUrl, UriKind.Absolute);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly string _cacheDirectory;

    public GameVersionCatalog()
        : this(new HttpClient(), AppPaths.CacheDirectory) { }

    internal GameVersionCatalog(HttpClient httpClient, string cacheDirectory)
    {
        _http = httpClient;
        _cacheDirectory = cacheDirectory;
    }

    /// <inheritdoc />
    public async Task<VersionCatalog> LoadAsync(CancellationToken cancellationToken = default)
    {
        VersionCatalog? remote = await TryFetchRemoteAsync(cancellationToken);
        if (remote is not null)
        {
            return remote;
        }

        VersionCatalog? cached = await TryReadCacheAsync(cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        throw new InvalidOperationException("版本目录获取失败，请检查网络连接后重试。");
    }

    private async Task<VersionCatalog?> TryFetchRemoteAsync(CancellationToken cancellationToken)
    {
        try
        {
            string json = await _http.GetStringAsync(CatalogUri, cancellationToken);
            VersionCatalog? parsed = Parse(json);
            if (parsed is not null)
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

    private async Task<VersionCatalog?> TryReadCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            string cacheFile = Path.Combine(_cacheDirectory, "versions.json");
            if (!File.Exists(cacheFile))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(cacheFile, cancellationToken);
            return Parse(json);
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
            string cacheFile = Path.Combine(_cacheDirectory, "versions.json");
            await File.WriteAllTextAsync(cacheFile, json, cancellationToken);
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
