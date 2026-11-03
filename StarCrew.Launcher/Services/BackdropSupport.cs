using Microsoft.UI.Composition.SystemBackdrops;

namespace StarCrew.Launcher.Services;

internal sealed class BackdropSupport : IBackdropSupport
{
    public bool IsMicaSupported => MicaController.IsSupported();
}
