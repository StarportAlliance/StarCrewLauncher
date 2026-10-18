using CommunityToolkit.Mvvm.Input;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.ViewModels;

internal sealed partial class InstanceItemViewModel
{
    private readonly IInstanceLauncher _launcher;
    private readonly IExternalLauncher _externalLauncher;
    private readonly INotificationService _notifications;
    private readonly IAppStrings _strings;

    public InstanceItemViewModel(
        GameInstance instance,
        IInstanceLauncher launcher,
        IExternalLauncher externalLauncher,
        INotificationService notifications,
        IAppStrings strings
    )
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(externalLauncher);
        ArgumentNullException.ThrowIfNull(notifications);
        ArgumentNullException.ThrowIfNull(strings);
        Instance = instance;
        _launcher = launcher;
        _externalLauncher = externalLauncher;
        _notifications = notifications;
        _strings = strings;
    }

    public GameInstance Instance { get; private set; }

    public string Name => Instance.Name;

    public string Summary =>
        _strings.Format(
            "Instances_SummaryFormat",
            Instance.GameVersionLabel,
            Instance.State == InstanceState.Ready
                ? _strings.GetString("Instances_StateReady")
                : _strings.GetString("Instances_StateNotReady"),
            Instance.InstallDirectory
        );

    public bool CanLaunch => Instance.State == InstanceState.Ready;

    public event EventHandler<GameInstance>? RenameRequested;

    public event EventHandler<GameInstance>? DeleteRequested;

    public event EventHandler<GameInstance>? DetailsRequested;

    [RelayCommand]
    private async Task LaunchAsync()
    {
        if (!CanLaunch)
        {
            _notifications.Notify(
                _strings.GetString("Instances_NotReadyToLaunch"),
                null,
                NotificationSeverity.Warning
            );
            return;
        }

        try
        {
            LaunchOutcome outcome = await _launcher.LaunchAsync(Instance);
            _notifications.Notify(
                outcome.Message,
                null,
                outcome.IsSuccess ? NotificationSeverity.Success : NotificationSeverity.Error
            );
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        try
        {
            await _externalLauncher.OpenFolderAsync(Instance.InstallDirectory);
        }
        catch (Exception ex)
        {
            _notifications.Notify(
                _strings.Format("Instances_OpenFolderFailedFormat", ex.Message),
                null,
                NotificationSeverity.Error
            );
        }
    }

    [RelayCommand]
    private void RequestRename() => RenameRequested?.Invoke(this, Instance);

    [RelayCommand]
    private void RequestDelete() => DeleteRequested?.Invoke(this, Instance);

    [RelayCommand]
    private void RequestDetails() => DetailsRequested?.Invoke(this, Instance);
}
