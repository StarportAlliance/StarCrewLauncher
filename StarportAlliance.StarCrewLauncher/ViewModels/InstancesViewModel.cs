using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StarportAlliance.StarCrewLauncher.Models;
using StarportAlliance.StarCrewLauncher.Services;

namespace StarportAlliance.StarCrewLauncher.ViewModels;

internal sealed partial class InstancesViewModel : ObservableObject
{
    private readonly IInstanceStore _store;
    private readonly IInstanceLauncher _launcher;
    private readonly IExternalLauncher _externalLauncher;
    private readonly INotificationService _notifications;
    private readonly INavigationService _navigation;
    private readonly IAppStrings _strings;

    public InstancesViewModel(
        IInstanceStore store,
        IInstanceLauncher launcher,
        IExternalLauncher externalLauncher,
        INotificationService notifications,
        INavigationService navigation,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(externalLauncher);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(strings);
        _store = store;
        _launcher = launcher;
        _externalLauncher = externalLauncher;
        _notifications = notifications;
        _navigation = navigation;
        _strings = strings;

        Items = [];
        HasInstances = false;
        Reload();
    }

    [ObservableProperty]
    public partial IReadOnlyList<InstanceItemViewModel> Items { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoInstances))]
    public partial bool HasInstances { get; set; }

    public bool HasNoInstances => !HasInstances;

    public event EventHandler<GameInstance>? RenameRequested;

    public event EventHandler<GameInstance>? DeleteRequested;

    public event EventHandler<GameInstance>? DetailsRequested;

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
                _notifications,
                _strings
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

    public async Task ConfirmDeleteAsync(GameInstance instance, bool deleteFiles)
    {
        ArgumentNullException.ThrowIfNull(instance);

        LauncherState state = _store.Load();
        LauncherState updated = state with
        {
            Instances = state.Instances.Where(i => i.Id != instance.Id).ToList(),
            LastInstanceId = state.LastInstanceId == instance.Id ? null : state.LastInstanceId,
        };
        // 先落盘再删文件：保存失败时中止，避免记录指向已删目录
        if (!_store.Save(updated))
        {
            _notifications.Notify(
                _strings.GetString("Instances_StateSaveFailed"),
                null,
                NotificationSeverity.Error
            );
            return;
        }

        Reload();

        if (deleteFiles && Directory.Exists(instance.InstallDirectory))
        {
            try
            {
                await Task.Run(() => Directory.Delete(instance.InstallDirectory, recursive: true));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _notifications.Notify(
                    _strings.Format("Instances_DeleteFailedFormat", ex.Message),
                    null,
                    NotificationSeverity.Error
                );
                return;
            }
        }

        _notifications.Notify(
            _strings.Format("Instances_RemovedFormat", instance.Name),
            null,
            NotificationSeverity.Informational
        );
    }

    public void ConfirmRename(GameInstance instance, string newName)
    {
        ArgumentNullException.ThrowIfNull(instance);
        string? candidate = newName?.Trim();
        if (string.IsNullOrWhiteSpace(candidate))
        {
            _notifications.Notify(
                _strings.GetString("Instances_RenameEmpty"),
                null,
                NotificationSeverity.Error
            );
            return;
        }

        LauncherState state = _store.Load();
        bool duplicate = state.Instances.Any(i =>
            i.Id != instance.Id && i.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)
        );
        if (duplicate)
        {
            _notifications.Notify(
                _strings.GetString("Instances_RenameDuplicate"),
                null,
                NotificationSeverity.Error
            );
            return;
        }

        _store.Save(
            state with
            {
                Instances =
                [
                    .. state.Instances.Select(i =>
                        i.Id == instance.Id ? i with { Name = candidate } : i
                    ),
                ],
            }
        );
        Reload();
        _notifications.Notify(
            _strings.Format("Instances_RenamedFormat", candidate),
            null,
            NotificationSeverity.Success
        );
    }
}
