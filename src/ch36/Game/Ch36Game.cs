using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter36;

/// <summary>
/// Chapter 36 - The Relentless Timer.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch36Game : Retro.Hunch.HunchGame
{
    public Ch36Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH36 THE CLOCK";
        Dbg.Timing = true;
    }
}
