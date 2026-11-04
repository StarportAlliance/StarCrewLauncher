using System.Diagnostics;
using Microsoft.Extensions.Logging;
using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>基于 <see cref="Process" /> 的外部程序执行器。</summary>
internal sealed class ProcessRunner : IDepotDownloaderRunner
{
    private const int FailureTailLines = 100;

    private readonly IAppStrings _strings;
    private readonly ILogger<ProcessRunner> _logger;

    public ProcessRunner(IAppStrings strings, ILogger<ProcessRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(logger);
        _strings = strings;
        _logger = logger;
    }

    public async Task<ExecutableOutcome> RunAsync(
        ExecutableCommand command,
        IProgress<string>? output = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        string program = Path.GetFileName(command.FilePath);
        _logger.LogInformation("启动外部程序 {Program}", program);

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
            // 命令行含明文密码，输出含账号名与二维码，故记录输出时统一过脱敏后再落盘
            if (command.LogOutput && _logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("{Output}", LogRedaction.Scrub(e.Data));
            }
        }

        process.OutputDataReceived += CaptureLine;
        process.ErrorDataReceived += CaptureLine;

        try
        {
            if (!process.Start())
            {
                _logger.LogError("外部程序 {Program} 启动失败", program);
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
                    // 进程较快退出
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
                // 这还杀不掉就不管了
            }

            _logger.LogWarning("外部程序 {Program} 已取消", program);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "外部程序 {Program} 执行异常", program);
            return new ExecutableOutcome(-1, [ex.Message]);
        }

        _logger.LogInformation(
            "外部程序 {Program} 退出，代码 {ExitCode}",
            program,
            process.ExitCode
        );
        if (process.ExitCode != 0 && command.LogOutput)
        {
            _logger.LogError(
                "外部程序 {Program} 失败，末尾输出：{Tail}",
                program,
                string.Join(
                    Environment.NewLine,
                    lines.TakeLast(FailureTailLines).Select(LogRedaction.Scrub)
                )
            );
        }

        return new ExecutableOutcome(process.ExitCode, lines);
    }
}
