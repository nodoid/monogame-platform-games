using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

/// <summary>
/// Three depth layers behind the ramparts.
///
/// On a flick-screen game the parallax does not scroll with a camera - there
/// isn't one. Instead each layer is offset by the screen number, by a different
/// amount. The result is that walking from screen four to screen five moves the
/// near hills a long way and the far towers hardly at all, so the castle reads
/// as one continuous place rather than fifteen unrelated pictures.
/// </summary>
public sealed class Parallax
{
    private readonly Texture2D _sky, _far, _near;

    public Parallax(Assets assets)
    {
        _sky = assets.Texture("sky");
        _far = assets.Texture("bg_far");
        _near = assets.Texture("bg_near");
    }

    public void Draw(SpriteBatch batch, int screenIndex, float drift, int width, int height)
    {
        batch.Draw(_sky, new Rectangle(0, 0, width, height), Color.White);
        DrawStrip(batch, _far, (int)(screenIndex * 46 + drift * 0.25f), height - 96, width);
        DrawStrip(batch, _near, (int)(screenIndex * 104 + drift * 0.6f), height - 64, width);
    }

    private static void DrawStrip(SpriteBatch batch, Texture2D tex, int offset, int y, int width)
    {
        int ox = ((offset % tex.Width) + tex.Width) % tex.Width;
        int drawn = 0;
        while (drawn < width)
        {
            int take = Math.Min(tex.Width - ox, width - drawn);
            batch.Draw(tex, new Rectangle(drawn, y, take, tex.Height),
                       new Rectangle(ox, 0, take, tex.Height), Color.White);
            drawn += take;
            ox = 0;
        }
    }
}
