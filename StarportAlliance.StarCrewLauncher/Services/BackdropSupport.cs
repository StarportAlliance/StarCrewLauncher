using Microsoft.UI.Composition.SystemBackdrops;

namespace StarportAlliance.StarCrewLauncher.Services;

internal sealed class BackdropSupport : IBackdropSupport
{
    public bool IsMicaSupported => MicaController.IsSupported();
}
