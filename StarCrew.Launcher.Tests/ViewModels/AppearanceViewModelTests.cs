using NSubstitute;
using StarCrew.Launcher.Models;
using StarCrew.Launcher.Services;
using StarCrew.Launcher.ViewModels;

namespace StarCrew.Launcher.Tests.ViewModels;

public sealed class AppearanceViewModelTests
{
    private static AppearanceViewModel Create(IThemeSettings? theme = null) =>
        new(theme ?? Substitute.For<IThemeSettings>());

    [Fact]
    public void Ctor_WithNullThemeSettings_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => new AppearanceViewModel(null!));

    [Theory]
    [InlineData("System", 0)]
    [InlineData("Light", 1)]
    [InlineData("Dark", 2)]
    public void SelectedThemeIndex_ReadsFromSettings(string themeName, int expected)
    {
        IThemeSettings settings = Substitute.For<IThemeSettings>();
        settings.Theme.Returns(Enum.Parse<AppTheme>(themeName));

        AppearanceViewModel vm = Create(theme: settings);

        Assert.Equal(expected, vm.SelectedThemeIndex);
    }

    [Theory]
    [InlineData(0, "System")]
    [InlineData(1, "Light")]
    [InlineData(2, "Dark")]
    [InlineData(99, "System")]
    public void SelectedThemeIndex_WritesToSettings(int index, string expectedName)
    {
        IThemeSettings settings = Substitute.For<IThemeSettings>();
        AppearanceViewModel vm = Create(theme: settings);

        vm.SelectedThemeIndex = index;

        settings.Received(1).Theme = Enum.Parse<AppTheme>(expectedName);
    }
}
