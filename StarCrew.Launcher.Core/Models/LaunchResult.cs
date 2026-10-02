namespace StarCrew.Launcher.Models;

/// <summary>游戏启动结果，成功时携带启动方式，失败时携带原因。</summary>
/// <param name="IsSuccess">是否已发起启动。</param>
/// <param name="Method">实际生效的启动方式。</param>
/// <param name="Message">面向用户展示的结果描述。</param>
internal sealed record LaunchResult(bool IsSuccess, LaunchMethod Method, string Message);
