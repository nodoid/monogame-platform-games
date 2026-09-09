using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter32;

/// <summary>
/// Chapter 32 - Rope Swings and Traversal Gadgets.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch32Game : Retro.Hunch.HunchGame
{
    public Ch32Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH32 ROPE ARCS";
        Dbg.Hitboxes = true;
        Dbg.Paths = true;
    }
}
