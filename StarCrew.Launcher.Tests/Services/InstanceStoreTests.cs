using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Services;

public sealed class InstanceStoreTests : IDisposable
{
    private readonly string _root;
    private readonly string _filePath;

    public InstanceStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _filePath = Path.Combine(_root, "instances.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Load_WithMissingFile_ReturnsEmptyState()
    {
        InstanceStore store = new(_filePath);

        LauncherState state = store.Load();

        Assert.Equal(1, state.Version);
        Assert.Null(state.LastInstanceId);
        Assert.Null(state.SteamAccountName);
        Assert.Empty(state.Instances);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        InstanceStore store = new(_filePath);
        Guid id = Guid.NewGuid();
        DateTimeOffset created = DateTimeOffset.UtcNow.AddDays(-1);
        GameInstance instance = new(
            id,
            "游戏 A",
            Path.Combine(_root, "a"),
            945360,
            945361,
            990035194164819049L,
            "2026.9.1",
            InstanceState.Ready,
            created,
            null
        );
        LauncherState toSave = new(1, id, null, [instance]);

        store.Save(toSave);
        LauncherState loaded = store.Load();

        Assert.Single(loaded.Instances);
        Assert.Equal(instance, loaded.Instances[0]);
        Assert.Equal(id, loaded.LastInstanceId);
    }

    [Fact]
    public void Load_WithCorruptFile_ReturnsEmptyAndLeavesBackup()
    {
        File.WriteAllText(_filePath, "{ this is not valid json");
        InstanceStore store = new(_filePath);

        LauncherState state = store.Load();

        Assert.Equal(LauncherState.Empty, state);
        Assert.True(File.Exists(_filePath + ".bak"));
    }

    [Fact]
    public void Save_CreatesDirectoryIfMissing()
    {
        string nested = Path.Combine(_root, "deep", "instances.json");
        InstanceStore store = new(nested);

        store.Save(LauncherState.Empty);

        Assert.True(File.Exists(nested));
        LauncherState loaded = store.Load();
        Assert.Equal(1, loaded.Version);
        Assert.Empty(loaded.Instances);
        Assert.Null(loaded.LastInstanceId);
    }

    [Fact]
    public void Load_WithValidJson_ReadsAllValuesIncludingLongManifestId()
    {
        string json = """
            {
              "version": 1,
              "lastInstanceId": null,
              "steamAccountName": null,
              "instances": [
                {
                  "id": "00000000-0000-0000-0000-000000000001",
                  "name": "A",
                  "installDirectory": "C:\\games\\a",
                  "appId": 945360,
                  "depotId": 945361,
                  "manifestId": 990035194164819049,
                  "gameVersionLabel": "2026.9.1",
                  "state": "ready",
                  "createdAt": "2026-10-01T00:00:00Z",
                  "lastLaunchedAt": null
                }
              ]
            }
            """;
        File.WriteAllText(_filePath, json);
        InstanceStore store = new(_filePath);

        LauncherState state = store.Load();

        Assert.Single(state.Instances);
        Assert.Equal(990035194164819049L, state.Instances[0].ManifestId);
        Assert.Equal(InstanceState.Ready, state.Instances[0].State);
    }
}
