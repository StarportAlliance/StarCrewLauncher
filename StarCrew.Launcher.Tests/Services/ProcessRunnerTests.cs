using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class ProcessRunnerTests
{
    [Fact]
    public async Task RunAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        ProcessRunner runner = new(TestStrings.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunAsync(null!));
    }

    [Fact]
    public async Task RunAsync_WithCanceledToken_ThrowsOperationCanceled()
    {
        ProcessRunner runner = new(TestStrings.Instance);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            runner.RunAsync(
                new ExecutableCommand("cmd.exe", ["/c", "echo", "hi"], null),
                null,
                cts.Token
            )
        );
    }

    [Fact]
    public async Task RunAsync_WithEcho_ReturnsZeroAndCapturesOutput()
    {
        ProcessRunner runner = new(TestStrings.Instance);
        List<string> seen = [];
        IProgress<string> output = new Progress<string>(seen.Add);

        ExecutableOutcome outcome = await runner.RunAsync(
            new ExecutableCommand("cmd.exe", ["/c", "echo", "hello-runner"], null),
            output
        );

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains(
            "hello-runner",
            string.Join("\n", outcome.OutputLines),
            StringComparison.Ordinal
        );
        Assert.Contains("hello-runner", string.Join("\n", seen), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WithMissingExe_ReturnsNegativeOne()
    {
        ProcessRunner runner = new(TestStrings.Instance);
        string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".exe");

        ExecutableOutcome outcome = await runner.RunAsync(new ExecutableCommand(missing, [], null));

        Assert.Equal(-1, outcome.ExitCode);
        Assert.NotEmpty(outcome.OutputLines);
    }

    [Fact]
    public async Task RunAsync_WithStdin_ForwardsInputToProcess()
    {
        ProcessRunner runner = new(TestStrings.Instance);

        ExecutableOutcome outcome = await runner.RunAsync(
            new ExecutableCommand("cmd.exe", ["/c", "findstr", "stdin"], "hello-stdin\n")
        );

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains(
            "hello-stdin",
            string.Join("\n", outcome.OutputLines),
            StringComparison.Ordinal
        );
    }

    [Fact]
    public async Task RunAsync_WhenCanceled_KillsProcessAndThrows()
    {
        ProcessRunner runner = new(TestStrings.Instance);
        using CancellationTokenSource cts = new();
        Task<ExecutableOutcome> running = runner.RunAsync(
            new ExecutableCommand("ping.exe", ["-n", "30", "127.0.0.1"], null),
            null,
            cts.Token
        );
        await Task.Delay(500);
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
    }
}
