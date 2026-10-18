using System.Runtime.CompilerServices;
using Velopack;

namespace StarCrew.Launcher.Tests;

internal static class VelopackTestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize() => VelopackApp.Build().Run();
}
