using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using StarportAlliance.StarCrewLauncher.Models;

namespace StarportAlliance.StarCrewLauncher.Services;

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
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        if (command.StandardInput is not null)
        {
            process.StartInfo.RedirectStandardInput = true;
        }

        List<string> outputLines = [];
        List<string> errorLines = [];
        object syncRoot = new();

        async Task PumpAsync(StreamReader reader, bool isOutput)
        {
            try
            {
                while (await reader.ReadLineAsync(cancellationToken) is { } line)
                {
                    lock (syncRoot)
                    {
                        (isOutput ? outputLines : errorLines).Add(line);
                    }

                    if (isOutput)
                    {
                        output?.Report(line);
                    }

                    // 输出含账号名与二维码，故记录输出时统一过脱敏后再落盘
                    if (command.LogOutput && _logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("{Output}", LogRedaction.Scrub(line));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 进程被取消，读取随之中断
            }
            catch (ObjectDisposedException)
            {
                // 进程已退出并释放流
            }
            catch (IOException)
            {
                // 同上
            }
        }

        try
        {
            if (!process.Start())
            {
                _logger.LogError("外部程序 {Program} 启动失败", program);
                return new ExecutableOutcome(-1, [], [_strings.GetString("Runner_StartFailed")]);
            }

            Task outputPump = PumpAsync(process.StandardOutput, isOutput: true);
            Task errorPump = PumpAsync(process.StandardError, isOutput: false);

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

            // 进程退出后读取任务会读到 EOF，等它读完才能保证输出不丢行
            await Task.WhenAll(outputPump, errorPump);
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
            catch (InvalidOperationException)
            {
                // 进程已退出
            }

            await WaitForExitIgnoringFailureAsync(process);

            _logger.LogWarning("外部程序 {Program} 已取消", program);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "外部程序 {Program} 执行异常", program);
            return new ExecutableOutcome(-1, [], [ex.Message]);
        }

        _logger.LogInformation(
            "外部程序 {Program} 退出，代码 {ExitCode}",
            program,
            process.ExitCode
        );

        string[] capturedOutput;
        string[] capturedErrors;
        lock (syncRoot)
        {
            capturedOutput = [.. outputLines];
            capturedErrors = [.. errorLines];
        }

        if (process.ExitCode != 0 && command.LogOutput)
        {
            string tail = string.Join(
                Environment.NewLine,
                (capturedErrors.Length > 0 ? capturedErrors : capturedOutput)
                    .TakeLast(FailureTailLines)
                    .Select(LogRedaction.Scrub)
            );
            _logger.LogError("外部程序 {Program} 失败，末尾输出：{Tail}", program, tail);
        }

        return new ExecutableOutcome(process.ExitCode, capturedOutput, capturedErrors);
    }

    private static async Task WaitForExitIgnoringFailureAsync(Process process)
    {
        try
        {
            using CancellationTokenSource grace = new(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(grace.Token);
        }
        catch (OperationCanceledException)
        {
            // 宽限期结束仍未退出
        }
        catch (InvalidOperationException)
        {
            // 进程没有启动或已释放
        }
    }
}
