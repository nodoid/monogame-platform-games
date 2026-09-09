using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter18;

/// <summary>
/// Chapter 18 - Enemy Variety Across Stages.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch18Game : Retro.Climber.ClimberGame
{
    public Ch18Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH18 ENEMIES";
        Dbg.Hitboxes = true;
    }
}
