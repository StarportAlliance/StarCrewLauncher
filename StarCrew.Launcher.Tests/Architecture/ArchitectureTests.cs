using System.Reflection;
using NetArchTest.Rules;
using StarCrew.Launcher.Services;

namespace StarCrew.Launcher.Tests.Architecture;

/// <summary>
/// 架构防腐测试：AI 增量开发最容易烂的是分层，单元测试拦不住，只能靠这类规则测试。
/// 新增代码如果破坏分层，这里会红，而不是等到屎山堆成才发现。
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Assembly AppAssembly = typeof(AppUpdater).Assembly;

    [Fact]
    public void Services_AreSealed()
    {
        TestResult result = Types
            .InAssembly(AppAssembly)
            .That()
            .AreClasses()
            .And()
            .ResideInNamespace("StarCrew.Launcher.Services")
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, "服务层类型必须 sealed，禁止随手加继承体系。");
    }

    [Fact]
    public void Services_DoNotDependOnXaml()
    {
        TestResult result = Types
            .InAssembly(AppAssembly)
            .That()
            .ResideInNamespace("StarCrew.Launcher.Services")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.UI.Xaml")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "服务层不许依赖 WinUI XAML：UI 逻辑漏进服务层就测不了了。"
        );
    }

    [Fact]
    public void Models_DoNotDependOnServices()
    {
        TestResult result = Types
            .InAssembly(AppAssembly)
            .That()
            .ResideInNamespace("StarCrew.Launcher.Models")
            .ShouldNot()
            .HaveDependencyOn("StarCrew.Launcher.Services")
            .GetResult();

        Assert.True(result.IsSuccessful, "模型层必须保持纯净，不许反向依赖服务层。");
    }

    [Fact]
    public void Models_DoNotDependOnViewModels()
    {
        TestResult result = Types
            .InAssembly(AppAssembly)
            .That()
            .ResideInNamespace("StarCrew.Launcher.Models")
            .ShouldNot()
            .HaveDependencyOn("StarCrew.Launcher.ViewModels")
            .GetResult();

        Assert.True(result.IsSuccessful, "模型层必须保持纯净，不许反向依赖视图模型层。");
    }

    [Fact]
    public void Services_DoNotDependOnViewModels()
    {
        TestResult result = Types
            .InAssembly(AppAssembly)
            .That()
            .ResideInNamespace("StarCrew.Launcher.Services")
            .ShouldNot()
            .HaveDependencyOn("StarCrew.Launcher.ViewModels")
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            "服务层不许反向依赖视图模型层：依赖方向只能是 ViewModel → Service。"
        );
    }

    [Fact]
    public void ViewModels_DoNotDependOnXaml()
    {
        TestResult result = Types
            .InAssembly(AppAssembly)
            .That()
            .ResideInNamespace("StarCrew.Launcher.ViewModels")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.UI.Xaml")
            .GetResult();

        Assert.True(result.IsSuccessful, "视图模型不许依赖 WinUI XAML：绑定与弹窗归界面层。");
    }

    [Fact]
    public void ViewModels_AreSealed()
    {
        TestResult result = Types
            .InAssembly(AppAssembly)
            .That()
            .AreClasses()
            .And()
            .ResideInNamespace("StarCrew.Launcher.ViewModels")
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, "视图模型必须 sealed，禁止随手加继承体系。");
    }
}
