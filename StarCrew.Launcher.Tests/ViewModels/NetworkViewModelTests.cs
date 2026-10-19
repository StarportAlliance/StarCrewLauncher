using NSubstitute;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class NetworkViewModelTests
{
    [Fact]
    public void Ctor_WithNullSettings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() =>
            new NetworkViewModel(null!, TestStrings.Instance)
        );

    [Theory]
    [InlineData("Official", 0)]
    [InlineData("GhProxy", 1)]
    [InlineData("Custom", 2)]
    public void Ctor_ReadsInitialIndexFromSettings(string acceleratorName, int expected)
    {
        DownloadAccelerator accelerator = Enum.Parse<DownloadAccelerator>(acceleratorName);
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        settings.Accelerator.Returns(accelerator);
        settings.CustomPrefix.Returns("https://acc.example.com");

        NetworkViewModel vm = new(settings, TestStrings.Instance);

        Assert.Equal(expected, vm.SelectedIndex);
        Assert.Equal("https://acc.example.com", vm.CustomPrefix);
        Assert.Equal(3, vm.Options.Count);
    }

    [Theory]
    [InlineData(0, "Official")]
    [InlineData(1, "GhProxy")]
    [InlineData(2, "Custom")]
    [InlineData(99, "Custom")]
    public void SelectedIndex_WritesToSettings(int index, string expectedName)
    {
        DownloadAccelerator expected = Enum.Parse<DownloadAccelerator>(expectedName);
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        _ = new NetworkViewModel(settings, TestStrings.Instance) { SelectedIndex = index };

        Assert.Equal(expected, settings.Accelerator);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void CustomVisible_FollowsSelectedIndex(int index, bool expected)
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        settings.Accelerator.Returns(DownloadAccelerator.GhProxy);
        settings.CustomPrefix.Returns(string.Empty);
        NetworkViewModel vm = new(settings, TestStrings.Instance)
        {
            // 先切到一个不同的下标，避免“写入的值与当前相同但没有引发通知”的空操作
            SelectedIndex = index == 2 ? 0 : 2,
        };
        List<string?> changed = [];
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.SelectedIndex = index;

        Assert.Equal(expected, vm.CustomVisible);
        Assert.Contains(nameof(NetworkViewModel.CustomVisible), changed);
    }

    [Theory]
    [InlineData(0, "官方直连")]
    [InlineData(1, "gh-proxy.org（第三方加速）")]
    [InlineData(2, "自定义前缀")]
    [InlineData(99, "自定义前缀")]
    public void SelectedOptionText_FollowsSelectedIndex(int index, string expected)
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        NetworkViewModel vm = new(settings, TestStrings.Instance) { SelectedIndex = index };

        Assert.Equal(expected, vm.SelectedOptionText);
    }

    [Fact]
    public void CustomPrefix_WritesToSettings()
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        _ = new NetworkViewModel(settings, TestStrings.Instance)
        {
            CustomPrefix = "https://acc.example.com",
        };

        Assert.Equal("https://acc.example.com", settings.CustomPrefix);
    }
}
