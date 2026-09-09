using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Retro.Engine;

namespace Chapter01;

/// <summary>
/// Chapter 1 - Why These Two Games.
///
/// The smallest thing that is still honestly a MonoGame app: it opens, it loads
/// two files, it draws, it responds to a touch. What it draws is the point of the
/// chapter - the same castle wall shown first as a tall screen and then as a wide
/// one, so you can see for yourself that the shape of the screen decides what
/// kind of game can be played on it.
/// </summary>
public class Ch01Game : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly IPlatformService _platform;

    private SpriteBatch _batch;
    private Assets _assets;
    private BitmapFont _font;

    private bool _showTall = true;
    private float _hold;
    private TouchCollection _touches;
    private bool _wasTouched;

    public Ch01Game(IPlatformService platform)
    {
        _platform = platform ?? new NullPlatformService();
        _graphics = new GraphicsDeviceManager(this) { IsFullScreen = true };
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    protected override void LoadContent()
    {
        Theme.ApplyLight();
        _batch = new SpriteBatch(GraphicsDevice);
        _assets = new Assets(GraphicsDevice);
        _font = new BitmapFont(_assets.Texture("font"));
    }

    protected override void Update(GameTime gameTime)
    {
        _touches = TouchPanel.GetState();
        bool touched = _touches.Count > 0;
        if (touched && !_wasTouched) _showTall = !_showTall;
        _wasTouched = touched;

        _hold += (float)gameTime.ElapsedGameTime.TotalSeconds;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Theme.Background);
        _batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        int sw = GraphicsDevice.PresentationParameters.BackBufferWidth;
        int sh = GraphicsDevice.PresentationParameters.BackBufferHeight;

        // Pick a scale that makes the demo readable on any panel.
        int scale = Math.Max(2, Math.Min(sw / 240, sh / 320));

        int vw = _showTall ? 224 : 256;
        int vh = _showTall ? 256 : 192;
        int bx = (sw - vw * scale) / 2;
        int by = (sh - vh * scale) / 2;

        // The frame: this is the play field the game would get.
        DrawBox(new Rectangle(bx - scale, by - scale, vw * scale + scale * 2, vh * scale + scale * 2),
                Theme.InkDim * 0.6f, scale);

        var tiles = _assets.Texture("tiles");
        int t = 8 * scale;

        // Six girders in the tall shape, two long walkways in the wide one. Same
        // tiles, same pixel budget, completely different game.
        if (_showTall)
        {
            for (int row = 0; row < 6; row++)
            {
                int y = by + (24 + row * 36) * scale;
                for (int x = 0; x < vw; x += 8)
                    _batch.Draw(tiles, new Rectangle(bx + x * scale, y, t, t),
                                new Rectangle(0, 0, 8, 8), Color.White);
                int lx = row % 2 == 0 ? 40 : 160;
                for (int k = 1; k <= 4; k++)
                    _batch.Draw(tiles, new Rectangle(bx + lx * scale, y - k * t, t, t),
                                new Rectangle(8, 0, 8, 8), Color.White);
            }
        }
        else
        {
            for (int x = 0; x < vw; x += 8)
            {
                if (x is >= 96 and < 128) continue;              // a gap to jump
                _batch.Draw(tiles, new Rectangle(bx + x * scale, by + 140 * scale, t, t),
                            new Rectangle(0, 0, 8, 8), Color.White);
            }
        }

        int cx = bx + vw * scale / 2;
        _font.DrawCentred(_batch, _showTall ? "PORTRAIT" : "LANDSCAPE", cx, by + 8 * scale,
                          Theme.Accent, scale);
        _font.DrawCentred(_batch, _showTall ? "224 X 256" : "256 X 192", cx, by + 20 * scale,
                          Color.White, Math.Max(1, scale / 2));
        _font.DrawCentred(_batch, _showTall ? "A CLIMBER" : "A RUNNER", cx,
                          by + (vh - 24) * scale, Color.White, Math.Max(1, scale / 2));

        if (((int)(_hold * 2) & 1) == 0)
            _font.DrawCentred(_batch, "TAP TO SWAP", sw / 2, sh - 40, Theme.InkDim,
                              Math.Max(1, scale / 2));

        // Capture builds mark the demo field so the book's figure can be
        // trimmed to it; this chapter has no virtual screen to ask.
        if (Dbg.CaptureFrame)
            Dbg.Box(_batch, _assets.Pixel,
                    new Rectangle(bx - scale * 2, by - scale * 2,
                                  vw * scale + scale * 4, vh * scale + scale * 4),
                    Dbg.CaptureMark);

        _batch.End();
        base.Draw(gameTime);
    }

    private void DrawBox(Rectangle r, Color c, int thickness)
    {
        var px = _assets.Pixel;
        _batch.Draw(px, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        _batch.Draw(px, new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness), c);
        _batch.Draw(px, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        _batch.Draw(px, new Rectangle(r.Right - thickness, r.Y, thickness, r.Height), c);
    }
}
