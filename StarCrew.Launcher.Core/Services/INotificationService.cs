using StarCrew.Launcher.Models;

namespace StarCrew.Launcher.Services;

/// <summary>应用内通知接缝：视图模型经此发 Toast，不直接接触 UI。</summary>
internal interface INotificationService
{
    /// <summary>有通知待展示；事件在发起通知的线程上触发。</summary>
    event EventHandler<NotificationMessage>? Requested;

    /// <summary>发起一条通知，展示由界面层负责。</summary>
    void Notify(string title, string message, NotificationSeverity severity);
}
