using StarCrew.Launcher.Services;
using Velopack.Exceptions;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>VelopackUpdateClient 的单元测试：只覆盖不触网的路径，真实更新走 vpk 打包后手动验证。</summary>
public sealed class VelopackUpdateClientTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WithBlankUrl_ThrowsArgumentException(string candidate)
    {
        Assert.Throws<ArgumentException>(() => new VelopackUpdateClient(candidate));
    }

    [Fact]
    public void Ctor_WithNullUrl_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new VelopackUpdateClient(null!));
    }

    [Fact]
    public void IsInstalled_InDev_ReturnsFalse()
    {
        VelopackUpdateClient client = new("https://example.com/updates");

        Assert.False(client.IsInstalled);
    }

    [Fact]
    public void CurrentVersion_InDev_ReturnsNull()
    {
        VelopackUpdateClient client = new("https://example.com/updates");

        Assert.Null(client.CurrentVersion);
    }

    [Fact]
    public async Task CheckForUpdates_InDev_ThrowsNotInstalled()
    {
        VelopackUpdateClient client = new("https://example.com/updates");

        await Assert.ThrowsAsync<NotInstalledException>(() => client.CheckForUpdatesAsync(default));
    }

    [Fact]
    public async Task Download_WithoutCheck_ThrowsInvalidOperation()
    {
        VelopackUpdateClient client = new("https://example.com/updates");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.DownloadUpdatesAsync("9.9.9", null, default)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Download_WithBlankVersion_ThrowsArgumentException(string version)
    {
        VelopackUpdateClient client = new("https://example.com/updates");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.DownloadUpdatesAsync(version, null, default)
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Prepare_WithBlankVersion_ThrowsArgumentException(string version)
    {
        VelopackUpdateClient client = new("https://example.com/updates");

        Assert.Throws<ArgumentException>(() => client.PrepareUpdateForRestart(version));
    }

    [Fact]
    public void Prepare_WithoutCheck_ThrowsInvalidOperation()
    {
        VelopackUpdateClient client = new("https://example.com/updates");

        Assert.Throws<InvalidOperationException>(() => client.PrepareUpdateForRestart("9.9.9"));
    }
}
