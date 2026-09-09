namespace Retro.Engine;

public enum OrientationMode { Portrait, Landscape, Sensor }

/// <summary>
/// The one thing the shared game code genuinely cannot do for itself.
///
/// MonoGame exposes GraphicsDeviceManager.SupportedOrientations, but on both
/// platforms the real decision is made by something outside the framework - an
/// Activity's RequestedOrientation on Android, the window scene's geometry
/// preferences on iOS 16 and later. Rather than litter the games with #if
/// ANDROID, the shared code asks for an orientation through this interface and
/// each platform head supplies the implementation.
///
/// The runner needs this at runtime: it plays in landscape and then rotates to
/// portrait for the high score table.
/// </summary>
public interface IPlatformService
{
    OrientationMode Orientation { get; }
    void SetOrientation(OrientationMode mode);

    /// <summary>Insets in device pixels that are unsafe to draw in (notches, home bar).</summary>
    (int Left, int Top, int Right, int Bottom) SafeInsets { get; }
}

/// <summary>Used on desktop during development, where nothing rotates.</summary>
public sealed class NullPlatformService : IPlatformService
{
    public OrientationMode Orientation { get; private set; } = OrientationMode.Sensor;
    public void SetOrientation(OrientationMode mode) => Orientation = mode;
    public (int, int, int, int) SafeInsets => (0, 0, 0, 0);
}
