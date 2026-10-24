using System.Net;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class GameVersionCatalogTests : IDisposable
{
    private readonly string _cacheDir;

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
        GameVersionCatalog catalog = new(http, _cacheDir, TestStrings.Instance);

        VersionCatalog result = await catalog.LoadAsync();

        Assert.Equal(1.1m, result.Version);
        Assert.Single(result.Versions);
        Assert.Equal("2099.1.1", result.Versions[0].VersionLabel);
        Assert.Equal(123L, result.Versions[0].ManifestId);
        Assert.True(File.Exists(Path.Combine(_cacheDir, "versions.jsonc")));
    }

    [Fact]
    public async Task LoadAsync_WithRemoteFailure_FallsBackToCache()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_cacheDir, "versions.jsonc"),
            """
            {
              "version": 3,
              "versions": [
                {
                  "versionLabel": "cached",
                  "manifestId": 456,
                  "publishedAt": "2026-01-01T00:00:00+00:00",
                  "tags": []
                }
              ]
            }
            """
        );
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = new(http, _cacheDir, TestStrings.Instance);

        VersionCatalog result = await catalog.LoadAsync();

        Assert.Equal(3m, result.Version);
        Assert.Single(result.Versions);
        Assert.Equal("cached", result.Versions[0].VersionLabel);
    }

    [Fact]
    public async Task LoadAsync_WithInvalidRemoteJson_FallsBackToCache()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_cacheDir, "versions.jsonc"),
            """{"version": 5, "versions": []}"""
        );
        using HttpClient http = CreateClient(_ => Ok("{ not json"));
        GameVersionCatalog catalog = new(http, _cacheDir, TestStrings.Instance);

        VersionCatalog result = await catalog.LoadAsync();

        Assert.Equal(5m, result.Version);
        Assert.Empty(result.Versions);
    }

    [Fact]
    public async Task LoadAsync_WithoutRemoteOrCache_ThrowsFailure()
    {
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = new(http, _cacheDir, TestStrings.Instance);

        Exception failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            catalog.LoadAsync()
        );

        Assert.Equal(TestStrings.Instance.GetString("Catalog_FetchFailed"), failure.Message);
    }

    [Fact]
    public async Task LoadAsync_WithCorruptCache_ThrowsFailure()
    {
        await File.WriteAllTextAsync(Path.Combine(_cacheDir, "versions.jsonc"), "{ corrupt");
        using HttpClient http = CreateClient(_ => NotFound());
        GameVersionCatalog catalog = new(http, _cacheDir, TestStrings.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => catalog.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_WithCanceledToken_ThrowsOperationCanceled()
    {
        using HttpClient http = CreateClient(_ => Ok("{}"));
        GameVersionCatalog catalog = new(http, _cacheDir, TestStrings.Instance);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => catalog.LoadAsync(cts.Token));
    }

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
