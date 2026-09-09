using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Retro.Climber;

/// <summary>
/// The host used by chapters 13 to 16: portrait, 224x256, one screen, no menus.
/// The full <c>ClimberGame</c> with its title screen, sessions and high score table
/// arrives in chapter 23.
/// </summary>
public class ClimberSandboxGame : RetroGame
{
    public const int VW = 224, VH = 256;

    private readonly System.Func<RetroGame, IScreen> _factory;

    public ClimberSandboxGame(IPlatformService platform, System.Func<RetroGame, IScreen> factory)
        : base(platform, VW, VH)
    {
        _factory = factory;
    }

    protected override void OnLoaded()
    {
        LockOrientation(OrientationMode.Portrait);
        Screens.Replace(_factory(this), fade: false);
    }
}
