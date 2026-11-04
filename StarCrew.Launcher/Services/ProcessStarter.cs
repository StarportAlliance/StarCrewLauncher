using System.Diagnostics;

namespace StarCrew.Launcher.Services;

/// <summary>基于 <see cref="Process" /> 的外部进程启动器。</summary>
internal sealed class ProcessStarter : IProcessStarter
{
    public bool TryStart(string fileName, string workingDirectory, out string? error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(workingDirectory);
        error = null;

        try
        {
            ProcessStartInfo startInfo = new()
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
