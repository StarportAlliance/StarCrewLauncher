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
    private static readonly Assembly AppAssembly = typeof(GameLauncher).Assembly;

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
}
