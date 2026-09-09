using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter33;

/// <summary>
/// Chapter 33 - Hazards and Obstacles.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch33Game : Retro.Hunch.HunchGame
{
    public Ch33Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH33 HAZARD BOXES";
        Dbg.Hitboxes = true;
    }
}
