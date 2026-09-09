using Microsoft.Xna.Framework;
using Retro.Engine;

namespace Chapter35;

/// <summary>
/// Chapter 35 - The Screen Goal and the Bell.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class Ch35Game : Retro.Hunch.HunchGame
{
    public Ch35Game(IPlatformService platform) : base(platform)
    {
        Dbg.Reset();
        Dbg.Caption = "CH35 THE BELL";
        Dbg.Hitboxes = true;
    }
}
