using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter34;

/// <summary>
/// Chapter 34 - Projectiles and Patrolling Guards.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch34Game : Retro.Hunch.HunchGame
{
    public Ch34Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH34 PROJECTILES";
        Dbg.AudioMeter = true;
        Dbg.Hitboxes = true;
    }
}
