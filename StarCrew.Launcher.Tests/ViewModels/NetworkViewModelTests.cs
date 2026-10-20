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
    [InlineData(1, "第三方加速")]
    [InlineData(2, "自定义")]
    [InlineData(99, "自定义")]
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

    [Theory]
    [InlineData("https://acc.example.com")]
    [InlineData("https://acc.example.com/path")]
    [InlineData("https://acc.example.com:8443")]
    public void CustomPrefix_WhenValid_PersistsValue(string prefix)
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        NetworkViewModel vm = new(settings, TestStrings.Instance)
        {
            SelectedIndex = 2,
            CustomPrefix = prefix,
        };

        Assert.Equal(prefix, settings.CustomPrefix);
        Assert.True(vm.CustomPrefixErrorVisible is false);
    }

    [Theory]
    [InlineData("http://acc.example.com")]
    [InlineData("acc.example.com")]
    [InlineData("https://")]
    [InlineData("ftp://acc.example.com")]
    public void CustomPrefix_WhenInvalid_KeepsLastValidValueInSettings(string prefix)
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        NetworkViewModel vm = new(settings, TestStrings.Instance)
        {
            SelectedIndex = 2,
            CustomPrefix = "https://acc.example.com",
        };

        vm.CustomPrefix = prefix;

        Assert.Equal("https://acc.example.com", settings.CustomPrefix);
        Assert.True(vm.CustomPrefixErrorVisible);
    }

    [Fact]
    public void CustomPrefix_WhenEmpty_IsValidAndClearsSettings()
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        NetworkViewModel vm = new(settings, TestStrings.Instance)
        {
            SelectedIndex = 2,
            CustomPrefix = "https://acc.example.com",
        };

        vm.CustomPrefix = string.Empty;

        Assert.Equal(string.Empty, settings.CustomPrefix);
        Assert.False(vm.CustomPrefixErrorVisible);
    }

    [Fact]
    public void CustomPrefix_TrimsSurroundingWhitespace()
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        NetworkViewModel vm = new(settings, TestStrings.Instance) { SelectedIndex = 2 };

        vm.CustomPrefix = "  https://acc.example.com  ";

        Assert.Equal("https://acc.example.com", vm.CustomPrefix);
        Assert.Equal("https://acc.example.com", settings.CustomPrefix);
    }

    [Fact]
    public void CustomPrefixErrorVisible_OnlyWhenCustomSelected()
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        NetworkViewModel vm = new(settings, TestStrings.Instance)
        {
            SelectedIndex = 2,
            CustomPrefix = "not-a-url",
        };

        Assert.True(vm.CustomPrefixErrorVisible);

        vm.SelectedIndex = 0;

        Assert.False(vm.CustomPrefixErrorVisible);
        Assert.True(vm.CustomPrefixHelpVisible);
    }

    [Fact]
    public void PreviewText_WhenInvalidInput_KeepsLastValidPreview()
    {
        INetworkSettings settings = Substitute.For<INetworkSettings>();
        NetworkViewModel vm = new(settings, TestStrings.Instance)
        {
            SelectedIndex = 2,
            CustomPrefix = "https://acc.example.com",
        };
        string valid = vm.PreviewText;

        vm.CustomPrefix = "not-a-url";

        Assert.Equal(valid, vm.PreviewText);
    }
}
