using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

/// <summary>实例页的视图模型：实例列表、更新/删除/重命名。</summary>
internal sealed partial class InstancesViewModel : ObservableObject
{
    private readonly IInstanceStore _store;
    private readonly IInstanceLauncher _launcher;
    private readonly IExternalLauncher _externalLauncher;
    private readonly INotificationService _notifications;
    private readonly INavigationService _navigation;

    public InstancesViewModel(
        IInstanceStore store,
        IInstanceLauncher launcher,
        IExternalLauncher externalLauncher,
        INotificationService notifications,
        INavigationService navigation
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(externalLauncher);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(navigation);
        _store = store;
        _launcher = launcher;
        _externalLauncher = externalLauncher;
        _notifications = notifications;
        _navigation = navigation;

        Items = [];
        HasInstances = false;
        Reload();
    }

    /// <summary>全部实例的卡片视图模型。</summary>
    [ObservableProperty]
    public partial IReadOnlyList<InstanceItemViewModel> Items { get; set; }

    [ObservableProperty]
    public partial bool HasInstances { get; set; }

    public event EventHandler<GameInstance>? RenameRequested;

    public event EventHandler<GameInstance>? DeleteRequested;

    public event EventHandler<GameInstance>? DetailsRequested;

    /// <summary>按存储里的状态重新生成卡片列表。</summary>
    public void Reload()
    {
        LauncherState state = _store.Load();
        List<InstanceItemViewModel> list = [];
        foreach (GameInstance instance in state.Instances)
        {
            InstanceItemViewModel item = new(
                instance,
                _launcher,
                _externalLauncher,
                _notifications
            );
            item.RenameRequested += (_, inst) => RenameRequested?.Invoke(this, inst);
            item.DeleteRequested += (_, inst) => DeleteRequested?.Invoke(this, inst);
            item.DetailsRequested += (_, inst) => DetailsRequested?.Invoke(this, inst);
            list.Add(item);
        }

        Items = list;
        HasInstances = list.Count > 0;
    }

    [RelayCommand]
    private void GoDownload() => _navigation.NavigateTo(NavigationTarget.Download);

    /// <summary>删除实例；deleteFiles 指定是否把该实例的游戏文件一并从磁盘删除。</summary>
    public void ConfirmDelete(GameInstance instance, bool deleteFiles)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (deleteFiles && Directory.Exists(instance.InstallDirectory))
        {
            try
            {
                Directory.Delete(instance.InstallDirectory, recursive: true);
            }
            catch (Exception ex)
            {
                _notifications.Notify($"删除失败：{ex.Message}", null, NotificationSeverity.Error);
                return;
            }
        }

        LauncherState state = _store.Load();
        _store.Save(
            state with
            {
                Instances = state.Instances.Where(i => i.Id != instance.Id).ToList(),
                LastInstanceId = state.LastInstanceId == instance.Id ? null : state.LastInstanceId,
            }
        );
        Reload();
        _notifications.Notify(
            $"实例「{instance.Name}」已从列表移除。",
            null,
            NotificationSeverity.Informational
        );
    }

    /// <summary>重命名实例；非法名称会被打回并提示原因。</summary>
    public void ConfirmRename(GameInstance instance, string newName)
    {
        ArgumentNullException.ThrowIfNull(instance);
        string? candidate = newName?.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            _notifications.Notify("重命名失败：实例名不能为空。", null, NotificationSeverity.Error);
            return;
        }

        LauncherState state = _store.Load();
        bool duplicate = state.Instances.Any(i =>
            i.Id != instance.Id && i.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)
        );
        if (duplicate)
        {
            _notifications.Notify(
                "重命名失败：已存在同名的实例。",
                null,
                NotificationSeverity.Error
            );
            return;
        }

        _store.Save(
            state with
            {
                Instances = state
                    .Instances.Select(i => i.Id == instance.Id ? i with { Name = candidate } : i)
                    .ToList(),
            }
        );
        Reload();
        _notifications.Notify(
            $"实例已重命名为「{candidate}」。",
            null,
            NotificationSeverity.Success
        );
    }
}
