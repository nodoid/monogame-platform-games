using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter38;

/// <summary>
/// Chapter 38 - Screen Design as Level Design.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch38Game : Retro.Hunch.HunchGame
{
    public Ch38Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH38 SCREEN DESIGN";
        Dbg.JumpArc = true;
        Dbg.TileGrid = true;
    }
}
