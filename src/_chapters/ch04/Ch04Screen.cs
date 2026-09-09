using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter04;

/// <summary>
/// Chapter 4 - Sprites, Sheets and the Content Pipeline.
///
/// A sheet browser. Left and right change sheet, up and down change the zoom,
/// and the jump button toggles between point and linear sampling.
///
/// The sampling toggle is the one to spend time on. At 6x zoom, linear filtering
/// turns a hand-placed 5-pixel eye into a grey smudge. Everything else in this
/// book assumes SamplerState.PointClamp, and this is why.
/// </summary>
public sealed class Ch04Screen : IScreen
{
    private const int W = 224, H = 256;

    private static readonly (string Name, int FrameW, int FrameH)[] Sheets =
    {
        ("jack_run", 12, 16), ("jack_climb", 12, 16), ("jack_hammer", 12, 16),
        ("barrel", 16, 16), ("fireball", 16, 16), ("gorilla", 32, 32),
        ("princess", 16, 16), ("tiles", 8, 8), ("font", 8, 8),
    };

    private readonly RetroGame _game;
    private int _index;
    private int _zoom = 4;
    private bool _linear;
    private float _t;

    public Ch04Screen(RetroGame game) => _game = game;

    public void Enter() { }
    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if (_game.Input.Pressed(Btn.Right)) _index = (_index + 1) % Sheets.Length;
        if (_game.Input.Pressed(Btn.Left)) _index = (_index + Sheets.Length - 1) % Sheets.Length;
        if (_game.Input.Pressed(Btn.Up)) _zoom = Math.Min(8, _zoom + 1);
        if (_game.Input.Pressed(Btn.Down)) _zoom = Math.Max(1, _zoom - 1);
        if (_game.Input.Pressed(Btn.Jump)) _linear = !_linear;
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var (name, fw, fh) = Sheets[_index];
        var tex = _game.Assets.Texture(name);

        f.DrawCentred(batch, "SHEET BROWSER", W / 2, 6, Theme.Accent);
        f.DrawCentred(batch, name.ToUpperInvariant(), W / 2, 20, Theme.Ink);
        f.DrawCentred(batch, $"{tex.Width} X {tex.Height}   FRAME {fw} X {fh}", W / 2, 32, Theme.InkDim);

        int frames = Math.Max(1, tex.Width / fw);

        // The whole sheet, zoomed, with frame boundaries drawn on top. Seeing the
        // cell grid is how you catch a sheet that is one pixel out.
        int dw = tex.Width * _zoom, dh = tex.Height * _zoom;
        int ox = W / 2 - dw / 2, oy = 56;

        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                    _linear ? SamplerState.LinearClamp : SamplerState.PointClamp);
        batch.Draw(tex, new Rectangle(ox, oy, dw, dh), Color.White);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        var px = _game.Assets.Pixel;
        for (int i = 0; i <= frames && name != "font"; i++)
            batch.Draw(px, new Rectangle(ox + i * fw * _zoom, oy, 1, dh), Theme.DbgSurface * 0.55f);

        // One animating frame at a fixed 4x, to prove the cells line up.
        int frame = (int)(_t * 8) % frames;
        int ay = oy + dh + 14;
        f.Draw(batch, "FRAME " + frame, new Vector2(12, ay), Theme.InkDim);
        batch.Draw(tex, new Rectangle(W / 2 - fw * 2, ay + 12, fw * 4, fh * 4),
                   new Rectangle(frame * fw, 0, fw, fh), Color.White);

        f.Draw(batch, "L R  SHEET", new Vector2(12, H - 44), Theme.InkDim);
        f.Draw(batch, "U D  ZOOM " + _zoom, new Vector2(12, H - 34), Theme.InkDim);
        f.Draw(batch, "FIRE FILTER " + (_linear ? "LINEAR" : "POINT"), new Vector2(12, H - 24),
               _linear ? Theme.Warn : Theme.Good);
    }
}
