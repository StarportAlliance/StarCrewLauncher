using System.Runtime.CompilerServices;
using Velopack;

namespace StarCrew.Launcher.Tests;

/// <summary>测试宿主启动钩子：Velopack 要求先跑 VelopackApp.Build().Run() 初始化定位器，
/// 生产环境在 App 构造器里做，测试宿主在这里补上，否则连 UpdateManager 都构造不出来。</summary>
internal static class VelopackTestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize() => VelopackApp.Build().Run();
}
