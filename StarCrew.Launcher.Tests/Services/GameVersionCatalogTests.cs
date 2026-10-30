using System.Net;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class GameVersionCatalogTests : IDisposable
{
    private const string CacheFileName = "versions.jsonc";

    private readonly string _cacheDir;
    private readonly Dictionary<string, object> _settingsStore = [];

    public GameVersionCatalogTests()
    {
        _cacheDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_cacheDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_cacheDir))
        {
            Directory.Delete(_cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WithRemoteJsonc_ReturnsRemoteAndWritesCache()
    {
        const string remote = """
            {
              // 模式版本
              "version": 1.1,
              "versions": [
                {
                  "versionLabel": "2099.1.1",
                  "manifestId": 123,
                  "publishedAt": "2099-01-01T00:00:00+00:00",
                  "tags": ["最新"]
                }
              ]
            }
            """;
        using HttpClient http = CreateClient(_ => Ok(remote));
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Equal(1.1m, result.Catalog.Version);
        Assert.Single(result.Catalog.Versions);
        Assert.Equal("2099.1.1", result.Catalog.Versions[0].VersionLabel);
        Assert.Equal(123L, result.Catalog.Versions[0].ManifestId);
        Assert.True(File.Exists(CacheFile()));
    }

    [Fact]
    public async Task LoadAsync_WithFreshCache_SkipsRemoteRequest()
    {
        await WriteCacheAsync(CatalogJson(7, "cached"));
        bool requested = false;
        using HttpClient http = CreateClient(_ =>
        {
            requested = true;
            return NotFound();
        });
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Equal(7m, result.Catalog.Version);
        Assert.Equal("cached", result.Catalog.Versions[0].VersionLabel);
        Assert.False(requested);
    }

    [Fact]
    public async Task LoadAsync_WithExpiredCache_FetchesRemote()
    {
        await WriteCacheAsync(CatalogJson(2, "stale"));
        AgeCache(TimeSpan.FromHours(25));
        using HttpClient http = CreateClient(_ => Ok(CatalogJson(3, "fresh")));
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Equal(3m, result.Catalog.Version);
        Assert.Equal("fresh", result.Catalog.Versions[0].VersionLabel);
        Assert.Contains(
            "fresh",
            await File.ReadAllTextAsync(CacheFile()),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task LoadAsync_WithCustomMaxAge_UsesConfiguredHours()
    {
        _settingsStore[VersionCacheSettings.MaxAgeHoursKey] = 1;
        await WriteCacheAsync(CatalogJson(2, "stale"));
        AgeCache(TimeSpan.FromHours(2));
        using HttpClient http = CreateClient(_ => Ok(CatalogJson(3, "fresh")));
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Equal(3m, result.Catalog.Version);
    }

    [Fact]
    public async Task LoadAsync_WithCacheDisabled_FetchesRemoteAndKeepsCacheUntouched()
    {
        _settingsStore[VersionCacheSettings.MaxAgeHoursKey] = 0;
        await WriteCacheAsync(CatalogJson(2, "cached"));
        bool requested = false;
        using HttpClient http = CreateClient(_ =>
        {
            requested = true;
            return Ok(CatalogJson(4, "remote"));
        });
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Equal(4m, result.Catalog.Version);
        Assert.True(requested);
        Assert.Contains(
            "cached",
            await File.ReadAllTextAsync(CacheFile()),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task LoadAsync_WithCacheDisabledAndRemoteFailure_Throws()
    {
        _settingsStore[VersionCacheSettings.MaxAgeHoursKey] = 0;
        await WriteCacheAsync(CatalogJson(3, "cached"));
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = CreateCatalog(http);

        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.LoadAsync());
    }

    [Fact]
    public void ClearCache_DeletesCacheFiles()
    {
        File.WriteAllText(CacheFile(), CatalogJson(1, "cached"));
        File.WriteAllText(CacheFile() + ".tmp", "tmp");
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = CreateCatalog(http);

        catalog.ClearCache();

        Assert.False(File.Exists(CacheFile()));
        Assert.False(File.Exists(CacheFile() + ".tmp"));
    }

    [Fact]
    public async Task LoadAsync_WithForceRefresh_IgnoresFreshCache()
    {
        await WriteCacheAsync(CatalogJson(2, "cached"));
        using HttpClient http = CreateClient(_ => Ok(CatalogJson(4, "remote")));
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync(forceRefresh: true);

        Assert.Equal(4m, result.Catalog.Version);
        Assert.Equal("remote", result.Catalog.Versions[0].VersionLabel);
    }

    [Fact]
    public async Task LoadAsync_WithExpiredCacheAndRemoteFailure_FallsBackToCache()
    {
        await WriteCacheAsync(CatalogJson(3, "cached"));
        AgeCache(TimeSpan.FromHours(25));
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Equal(3m, result.Catalog.Version);
        Assert.Equal("cached", result.Catalog.Versions[0].VersionLabel);
    }

    [Fact]
    public async Task LoadAsync_WithEmptyRemote_DoesNotOverwriteCache()
    {
        await WriteCacheAsync(CatalogJson(5, "cached"));
        AgeCache(TimeSpan.FromHours(25));
        using HttpClient http = CreateClient(_ => Ok("""{"version": 6, "versions": []}"""));
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Empty(result.Catalog.Versions);
        Assert.Contains(
            "cached",
            await File.ReadAllTextAsync(CacheFile()),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task LoadAsync_WithInvalidRemoteJson_FallsBackToCache()
    {
        await WriteCacheAsync(CatalogJson(5, "cached"));
        AgeCache(TimeSpan.FromHours(25));
        using HttpClient http = CreateClient(_ => Ok("{ not json"));
        GameVersionCatalog catalog = CreateCatalog(http);

        VersionCatalogLoadResult result = await catalog.LoadAsync();

        Assert.Equal(5m, result.Catalog.Version);
        Assert.Equal("cached", result.Catalog.Versions[0].VersionLabel);
    }

    [Fact]
    public async Task LoadAsync_WithoutRemoteOrCache_ThrowsFailure()
    {
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = CreateCatalog(http);

        Exception failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.LoadAsync()
        );

        Assert.Equal(TestStrings.Instance.GetString("Catalog_FetchFailed"), failure.Message);
    }

    [Fact]
    public async Task LoadAsync_WithCorruptCache_ThrowsFailure()
    {
        await WriteCacheAsync("{ corrupt");
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = CreateCatalog(http);

        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_WithCanceledToken_ThrowsOperationCanceled()
    {
        using HttpClient http = CreateClient(_ => Ok("{}"));
        GameVersionCatalog catalog = CreateCatalog(http);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            catalog.LoadAsync(cancellationToken: cts.Token)
        );
    }

    private GameVersionCatalog CreateCatalog(HttpClient http) =>
        new(http, _cacheDir, TestStrings.Instance, new VersionCacheSettings(_settingsStore));

    private string CacheFile() => Path.Combine(_cacheDir, CacheFileName);

    private async Task WriteCacheAsync(string json) =>
        await File.WriteAllTextAsync(CacheFile(), json);

    private void AgeCache(TimeSpan age) =>
        File.SetLastWriteTimeUtc(CacheFile(), DateTime.UtcNow - age);

    private static string CatalogJson(decimal version, string label) =>
        $$"""
            {
              "version": {{version}},
              "versions": [
                {
                  "versionLabel": "{{label}}",
                  "manifestId": 1,
                  "publishedAt": "2099-01-01T00:00:00+00:00",
                  "tags": []
                }
              ]
            }
            """;

    private static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new HttpClient(new FakeHandler(respond)) { BaseAddress = new Uri("https://localhost/") };

    private static HttpResponseMessage Ok(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private static HttpResponseMessage NotFound() => new(HttpStatusCode.NotFound);

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
