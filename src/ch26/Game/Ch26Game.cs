using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter26;

/// <summary>
/// Chapter 26 - Polishing the Climber.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch26Game : Retro.Climber.ClimberGame
{
    public Ch26Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH26 POLISH";
        Dbg.Hitboxes = true;
        Dbg.JumpArc = true;
        Dbg.Timing = true;
    }
}
