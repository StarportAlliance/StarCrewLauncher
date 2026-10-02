using NSubstitute;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

/// <summary>Steam 定位器的单元测试：环境接缝用替身，无真实注册表与文件副作用。</summary>
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
        SteamGameLocator locator = new(new WindowsSteamEnvironment());

        string? exe = locator.FindGameExe();

        Assert.True(
            exe is null || File.Exists(exe),
            $"返回的路径必须真实存在，实际返回：{exe ?? "<null>"}。"
        );
    }

    [Fact]
    public void Ctor_WithNullEnvironment_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SteamGameLocator(null!));
    }

    [Fact]
    public void FindGameExe_NoSteamDirs_ReturnsNull()
    {
        ISteamEnvironment environment = Substitute.For<ISteamEnvironment>();
        environment.GetRegistrySteamPaths().Returns(Array.Empty<string?>());
        environment.GetProgramFilesX86().Returns(@"C:\NoSuch\PFx86");
        environment.GetProgramFiles().Returns(@"C:\NoSuch\PF");
        environment.DirectoryExists(Arg.Any<string>()).Returns(false);
        SteamGameLocator locator = new(environment);

        string? exe = locator.FindGameExe();

        Assert.Null(exe);
    }

    [Fact]
    public void FindGameExe_VdfMissing_FallsBackToMainLibrary()
    {
        string steamDir = Path.Combine("C:", "Steam");
        string exe = Path.Combine(steamDir, "steamapps", "common", "Among Us", "Among Us.exe");
        string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        ISteamEnvironment environment = Substitute.For<ISteamEnvironment>();
        environment.GetRegistrySteamPaths().Returns(new string?[] { steamDir });
        environment.GetProgramFilesX86().Returns(@"C:\NoSuch\PFx86");
        environment.GetProgramFiles().Returns(@"C:\NoSuch\PF");
        environment.DirectoryExists(steamDir).Returns(true);
        environment.DirectoryExists(Arg.Is<string>(p => p != steamDir)).Returns(false);
        environment.FileExists(vdf).Returns(false);
        environment.FileExists(exe).Returns(true);
        environment.FileExists(Arg.Is<string>(p => p != vdf && p != exe)).Returns(false);
        SteamGameLocator locator = new(environment);

        string? found = locator.FindGameExe();

        Assert.Equal(exe, found);
    }

    [Fact]
    public void FindGameExe_VdfMissingWithoutExe_ReturnsNull()
    {
        string steamDir = Path.Combine("C:", "Steam");
        string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        ISteamEnvironment environment = Substitute.For<ISteamEnvironment>();
        environment.GetRegistrySteamPaths().Returns(new string?[] { steamDir });
        environment.GetProgramFilesX86().Returns(@"C:\NoSuch\PFx86");
        environment.GetProgramFiles().Returns(@"C:\NoSuch\PF");
        environment.DirectoryExists(steamDir).Returns(true);
        environment.DirectoryExists(Arg.Is<string>(p => p != steamDir)).Returns(false);
        environment.FileExists(vdf).Returns(false);
        environment.FileExists(Arg.Is<string>(p => p != vdf)).Returns(false);
        SteamGameLocator locator = new(environment);

        string? found = locator.FindGameExe();

        Assert.Null(found);
    }

    [Fact]
    public void FindGameExe_VdfWithMultipleLibraries_FindsExeInSecondLibrary()
    {
        string steamDir = Path.Combine("C:", "Steam");
        string lib1 = Path.Combine("D:", "Lib1");
        string lib2 = Path.Combine("E:", "Lib2");
        string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        string exeInLib2 = Path.Combine(lib2, "steamapps", "common", "Among Us", "Among Us.exe");
        string vdfContent =
            "\"libraryfolders\"\n{\n"
            + $"\"0\"\n{{\n\"path\"\t\t\"{steamDir}\"\n}}\n"
            + $"\"1\"\n{{\n\"path\"\t\t\"{lib1}\"\n}}\n"
            + $"\"2\"\n{{\n\"path\"\t\t\"{lib2}\"\n}}\n}}";
        ISteamEnvironment environment = Substitute.For<ISteamEnvironment>();
        environment.GetRegistrySteamPaths().Returns(new string?[] { steamDir });
        environment.GetProgramFilesX86().Returns(@"C:\NoSuch\PFx86");
        environment.GetProgramFiles().Returns(@"C:\NoSuch\PF");
        environment.DirectoryExists(Arg.Any<string>()).Returns(true);
        environment.FileExists(vdf).Returns(true);
        environment.FileExists(exeInLib2).Returns(true);
        environment.FileExists(Arg.Is<string>(p => p != vdf && p != exeInLib2)).Returns(false);
        environment.ReadAllTextOrNull(vdf).Returns(vdfContent);
        SteamGameLocator locator = new(environment);

        string? found = locator.FindGameExe();

        Assert.Equal(exeInLib2, found);
    }

    [Fact]
    public void FindGameExe_DuplicateLibraries_ReturnsDeduplicatedResult()
    {
        string steamDir = Path.Combine("C:", "Steam");
        string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        string exe = Path.Combine(steamDir, "steamapps", "common", "Among Us", "Among Us.exe");
        string vdfContent =
            "\"libraryfolders\"\n{\n"
            + $"\"0\"\n{{\n\"path\"\t\t\"{steamDir}\"\n}}\n"
            + $"\"1\"\n{{\n\"path\"\t\t\"{steamDir}\"\n}}\n}}";
        ISteamEnvironment environment = Substitute.For<ISteamEnvironment>();
        environment
            .GetRegistrySteamPaths()
            .Returns(new string?[] { steamDir, steamDir, null, "  " });
        environment.GetProgramFilesX86().Returns(@"C:\NoSuch\PFx86");
        environment.GetProgramFiles().Returns(@"C:\NoSuch\PF");
        environment
            .DirectoryExists(Arg.Any<string>())
            .Returns(call => call.Arg<string>() == steamDir);
        environment.FileExists(exe).Returns(true);
        environment.FileExists(vdf).Returns(true);
        environment.FileExists(Arg.Is<string>(p => p != exe && p != vdf)).Returns(false);
        environment.ReadAllTextOrNull(vdf).Returns(vdfContent);
        SteamGameLocator locator = new(environment);

        string? found = locator.FindGameExe();

        Assert.Equal(exe, found);
    }

    [Fact]
    public void FindGameExe_VdfUnreadable_ReturnsNull()
    {
        string steamDir = Path.Combine("C:", "Steam");
        string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        ISteamEnvironment environment = Substitute.For<ISteamEnvironment>();
        environment.GetRegistrySteamPaths().Returns(new string?[] { steamDir });
        environment.GetProgramFilesX86().Returns(@"C:\NoSuch\PFx86");
        environment.GetProgramFiles().Returns(@"C:\NoSuch\PF");
        environment.DirectoryExists(steamDir).Returns(true);
        environment.DirectoryExists(Arg.Is<string>(p => p != steamDir)).Returns(false);
        environment.FileExists(vdf).Returns(true);
        environment.FileExists(Arg.Is<string>(p => p != vdf)).Returns(false);
        environment.ReadAllTextOrNull(vdf).Returns((string?)null);
        SteamGameLocator locator = new(environment);

        string? found = locator.FindGameExe();

        Assert.Null(found);
    }

    [Fact]
    public void FindGameExe_EnvironmentThrows_ReturnsNull()
    {
        string steamDir = Path.Combine("C:", "Steam");
        string vdf = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        ISteamEnvironment environment = Substitute.For<ISteamEnvironment>();
        environment.GetRegistrySteamPaths().Returns(new string?[] { steamDir });
        environment.GetProgramFilesX86().Returns(@"C:\NoSuch\PFx86");
        environment.GetProgramFiles().Returns(@"C:\NoSuch\PF");
        environment.DirectoryExists(steamDir).Returns(true);
        environment.DirectoryExists(Arg.Is<string>(p => p != steamDir)).Returns(false);
        environment.FileExists(vdf).Returns(true);
        environment.FileExists(Arg.Is<string>(p => p != vdf)).Returns(false);
        environment.ReadAllTextOrNull(vdf).Returns(_ => throw new IOException("disk gone"));
        SteamGameLocator locator = new(environment);

        string? found = locator.FindGameExe();

        Assert.Null(found);
    }
}
