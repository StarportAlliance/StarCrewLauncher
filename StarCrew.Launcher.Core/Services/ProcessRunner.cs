using System.Diagnostics;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>基于 <see cref="Process" /> 的外部程序执行器。</summary>
internal sealed class ProcessRunner : IDepotDownloaderRunner
{
    private readonly IAppStrings _strings;

    public ProcessRunner(IAppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        _strings = strings;
    }

    /// <inheritdoc />
    public async Task<ExecutableOutcome> RunAsync(
        ExecutableCommand command,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        using Process process = new();
        process.StartInfo.FileName = command.FilePath;
        foreach (string argument in command.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        if (command.StandardInput is not null)
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        List<string> lines = [];
        void CaptureLine(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            lines.Add(e.Data);
            output?.Report(e.Data);
        }

        process.OutputDataReceived += CaptureLine;
        process.ErrorDataReceived += CaptureLine;

        try
        {
            if (!process.Start())
            {
                return new ExecutableOutcome(-1, [_strings.GetString("Runner_StartFailed")]);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (command.StandardInput is not null)
            {
                try
                {
                    await process.StandardInput.WriteAsync(
                        command.StandardInput.AsMemory(),
                        cancellationToken
                    );
                    process.StandardInput.Close();
                }
                catch (ObjectDisposedException)
                {
                    // 进程较快退出，没关系。
                }
            }

            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception)
            {
                // 无能为力时放弃
            }

            throw;
        }
        catch (Exception ex)
        {
            return new ExecutableOutcome(-1, [ex.Message]);
        }

        return new ExecutableOutcome(process.ExitCode, lines);
    }
}
