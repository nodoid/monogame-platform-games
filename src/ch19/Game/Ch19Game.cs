using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter19;

/// <summary>
/// Chapter 19 - The Hammer and Power-Ups.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch19Game : Retro.Climber.ClimberGame
{
    public Ch19Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH19 HAMMER BOX";
        Dbg.Hitboxes = true;
    }
}
