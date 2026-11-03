namespace StarCrew.Launcher.Services;

/// <summary>系统窗口材质支持能力。</summary>
internal interface IBackdropSupport
{
    /// <summary>当前系统是否支持 Mica 材质。</summary>
    bool IsMicaSupported { get; }
}
