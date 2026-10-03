namespace StarCrew.Launcher.Models;

/// <summary>游戏启动结果：成功带启动方式，失败带原因。</summary>
internal sealed record LaunchResult(bool IsSuccess, LaunchMethod Method, string Message);
