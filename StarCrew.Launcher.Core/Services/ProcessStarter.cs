using System.Diagnostics;

namespace StarCrew.Launcher.Services;

/// <summary>基于 <see cref="Process" /> 的生产实现，失败时返回 false 并带出错误信息。</summary>
internal sealed class ProcessStarter : IProcessStarter
{
    /// <inheritdoc />
    public bool TryStart(string fileName, string workingDirectory, out string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(workingDirectory);
        error = null;

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
            };
            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
