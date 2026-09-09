using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter42;

/// <summary>
/// Chapter 42 - Polishing the Runner.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch42Game : Retro.Hunch.HunchGame
{
    public Ch42Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH42 POLISH";
        Dbg.Hitboxes = true;
        Dbg.JumpArc = true;
        Dbg.Timing = true;
    }
}
