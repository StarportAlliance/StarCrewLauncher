using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>Steam 定位器的只读测试：只读注册表与文件系统，无任何副作用。</summary>
public sealed class SteamGameLocatorTests
{
    [Fact]
    public void AmongUsAppId_MatchesStorePage()
    {
        Assert.Equal(945360, SteamGameLocator.AmongUsAppId);
    }

    [Fact]
    public void FindGameExe_ReturnsNullOrExistingFile()
    {
        SteamGameLocator locator = new();

        string? exe = locator.FindGameExe();

        Assert.True(
            exe is null || File.Exists(exe),
            $"返回的路径必须真实存在，实际返回：{exe ?? "<null>"}。"
        );
    }
}
