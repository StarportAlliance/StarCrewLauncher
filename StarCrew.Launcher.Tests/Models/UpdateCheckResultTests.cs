using AwesomeAssertions;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Tests.Models;

/// <summary>更新检查结果记录的行为契约：值语义 + 全量解构，UI 展示依赖它。</summary>
public sealed class UpdateCheckResultTests
{
    [Fact]
    public void SameValues_AreEqual()
    {
        UpdateCheckResult first = new(UpdateState.ReadyToRestart, "0.4.0", "ok");
        UpdateCheckResult second = new(UpdateState.ReadyToRestart, "0.4.0", "ok");

        first.Should().Be(second);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        UpdateCheckResult ready = new(UpdateState.ReadyToRestart, "0.4.0", "ok");
        UpdateCheckResult failed = new(UpdateState.Failed, null, "no");

        ready.Should().NotBe(failed);
    }

    [Fact]
    public void Deconstruct_ExposesAllParts()
    {
        UpdateCheckResult result = new(UpdateState.UpToDate, null, "done");

        (UpdateState state, string? version, string message) = result;

        state.Should().Be(UpdateState.UpToDate);
        version.Should().BeNull();
        message.Should().Be("done");
    }
}
