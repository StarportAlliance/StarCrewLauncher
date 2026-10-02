using AwesomeAssertions;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Tests.Models;

/// <summary>启动结果记录的行为契约：值语义 + 全量解构，UI 展示依赖它。</summary>
public sealed class LaunchResultTests
{
    [Fact]
    public void SameValues_AreEqual()
    {
        LaunchResult first = new(true, LaunchMethod.SteamProtocol, "ok");
        LaunchResult second = new(true, LaunchMethod.SteamProtocol, "ok");

        first.Should().Be(second);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        LaunchResult success = new(true, LaunchMethod.DirectExe, "ok");
        LaunchResult failure = new(false, LaunchMethod.None, "no");

        success.Should().NotBe(failure);
    }

    [Fact]
    public void Deconstruct_ExposesAllParts()
    {
        LaunchResult result = new(true, LaunchMethod.DirectExe, "done");

        (bool isSuccess, LaunchMethod method, string message) = result;

        isSuccess.Should().BeTrue();
        method.Should().Be(LaunchMethod.DirectExe);
        message.Should().Be("done");
    }
}
