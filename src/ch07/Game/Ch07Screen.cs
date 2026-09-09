using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter07;

/// <summary>
/// Chapter 7 - Collision Fundamentals.
///
/// Drive a box around a tile map. The fire button switches between two
/// resolution strategies:
///
///   SEPARATE  move in x, resolve, then move in y, resolve.
///   COMBINED  move in both, then push out along the smallest overlap.
///
/// COMBINED is the one everybody writes first and it looks fine until you run
/// along a floor into a wall: the smallest overlap is then vertical, so the box
/// is pushed *up* and pops on top of the wall. Hold right against the tall block
/// with COMBINED selected and watch it climb. This is the single most common
/// platformer bug there is, and separating the axes is the entire fix.
/// </summary>
public sealed class Ch07Screen : IScreen
{
    private const int W = 224, H = 256;
    private const int Tile = 16, Cols = 14, Rows = 10;
    private const int MapTop = 60;
    private const float Speed = 70f, Gravity = 420f, Jump = -170f;

    private static readonly string[] Map =
    {
        "..............",
        "..............",
        ".....###......",
        "..............",
        "...#..........",
        "...#.......##.",
        "...#..###..##.",
        "...#.......##.",
        "..............",
        "##############",
    };

    private readonly RetroGame _game;
    private Vector2 _pos = new(40, MapTop + 100);
    private Vector2 _vel;
    private bool _separate = true;
    private int _popups;

    public Ch07Screen(RetroGame game) => _game = game;

    public void Enter() { }
    public void Leave() { }

    private static bool Solid(int c, int r)
        => c >= 0 && r >= 0 && c < Cols && r < Rows && Map[r][c] == '#';

    private static bool SolidAt(float x, float y)
        => Solid((int)(x / Tile), (int)((y - MapTop) / Tile));

    private bool Overlaps(Vector2 p)
    {
        var b = Box(p);
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (Map[r][c] == '#' &&
                    b.Intersects(new Rectangle(c * Tile, MapTop + r * Tile, Tile, Tile)))
                    return true;
        return false;
    }

    private static Rectangle Box(Vector2 p) => new((int)p.X - 5, (int)p.Y - 12, 10, 12);

    public void Update(float dt)
    {
        if (_game.Input.Pressed(Btn.Pause)) { _separate = !_separate; _popups = 0; }

        float mx = 0f;
        if (_game.Input.Down(Btn.Left)) mx -= 1f;
        if (_game.Input.Down(Btn.Right)) mx += 1f;

        _vel.X = mx * Speed;
        _vel.Y = Math.Min(400f, _vel.Y + Gravity * dt);

        bool grounded = SolidAt(_pos.X, _pos.Y + 1) ||
                        SolidAt(_pos.X - 4, _pos.Y + 1) || SolidAt(_pos.X + 4, _pos.Y + 1);
        if (grounded && _vel.Y > 0f) _vel.Y = 0f;
        if (grounded && _game.Input.PressedRecently(Btn.Jump))
        {
            _game.Input.ConsumeBuffer(Btn.Jump);
            _vel.Y = Jump;
        }

        float beforeY = _pos.Y;

        if (_separate)
        {
            _pos.X += _vel.X * dt;
            if (Overlaps(_pos))
            {
                int dir = Math.Sign(_vel.X) == 0 ? 1 : Math.Sign(_vel.X);
                while (Overlaps(_pos)) _pos.X -= dir;
                _vel.X = 0f;
            }
            _pos.Y += _vel.Y * dt;
            if (Overlaps(_pos))
            {
                int dir = Math.Sign(_vel.Y) == 0 ? 1 : Math.Sign(_vel.Y);
                while (Overlaps(_pos)) _pos.Y -= dir;
                _vel.Y = 0f;
            }
        }
        else
        {
            _pos += _vel * dt;
            if (Overlaps(_pos))
            {
                // Push out along whichever axis has the smaller escape distance.
                float bestX = 0f, bestY = 0f;
                for (int d = 1; d <= 16; d++)
                {
                    if (bestX == 0f && !Overlaps(_pos + new Vector2(-d * Math.Sign(_vel.X == 0 ? 1 : _vel.X), 0)))
                        bestX = d;
                    if (bestY == 0f && !Overlaps(_pos + new Vector2(0, -d * Math.Sign(_vel.Y == 0 ? 1 : _vel.Y))))
                        bestY = d;
                }
                if (bestY != 0f && (bestX == 0f || bestY <= bestX))
                {
                    _pos.Y -= bestY * Math.Sign(_vel.Y == 0 ? 1 : _vel.Y);
                    _vel.Y = 0f;
                }
                else if (bestX != 0f)
                {
                    _pos.X -= bestX * Math.Sign(_vel.X == 0 ? 1 : _vel.X);
                    _vel.X = 0f;
                }
            }
        }

        // Count the pops: a rise of more than a pixel while moving sideways only.
        if (_pos.Y < beforeY - 1.5f && Math.Abs(_vel.Y) < 1f) _popups++;

        _pos.X = Math.Clamp(_pos.X, 6, Cols * Tile - 6);
        if (_pos.Y > MapTop + Rows * Tile + 40) { _pos = new Vector2(40, MapTop + 40); _vel = Vector2.Zero; }
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        f.DrawCentred(batch, "COLLISION SANDBOX", W / 2, 6, Theme.Accent);
        f.DrawCentred(batch, _separate ? "AXIS SEPARATED" : "COMBINED PUSH-OUT", W / 2, 20,
                      _separate ? Theme.Good : Theme.Warn);
        f.DrawCentred(batch, "PAUSE BUTTON SWITCHES", W / 2, 32, Theme.InkDim);

        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (Map[r][c] == '#')
                {
                    batch.Draw(px, new Rectangle(c * Tile, MapTop + r * Tile, Tile, Tile),
                               new Color(176, 180, 194));
                    batch.Draw(px, new Rectangle(c * Tile, MapTop + r * Tile, Tile, 1),
                               new Color(120, 126, 148));
                }

        var b = Box(_pos);
        batch.Draw(px, b, Theme.Warn);
        batch.Draw(px, new Rectangle(b.X + 2, b.Y + 2, b.Width - 4, 3), Color.White);

        f.Draw(batch, "POPS " + _popups, new Vector2(10, H - 44),
               _popups > 0 ? Theme.Warn : Theme.Good);
        f.Draw(batch, "RUN RIGHT INTO THE WALL", new Vector2(10, H - 32), Theme.InkDim);
        f.Draw(batch, "WITH COMBINED SELECTED", new Vector2(10, H - 22), Theme.InkDim);
    }
}
