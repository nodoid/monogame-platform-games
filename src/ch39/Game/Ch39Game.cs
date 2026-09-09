using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter39;

/// <summary>
/// Chapter 39 - Parallax, Backgrounds and Atmosphere.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch39Game : Retro.Hunch.HunchGame
{
    public Ch39Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH39 PARALLAX";
        Dbg.Camera = true;
    }
}
