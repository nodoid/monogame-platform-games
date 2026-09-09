using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

public enum HTile : byte { Empty, Walkway, Stone, Spikes }

public readonly struct RopeAnchor
{
    public readonly Vector2 Top;
    public readonly float Length;
    public RopeAnchor(Vector2 top, float length) { Top = top; Length = length; }
}

/// <summary>
/// One flick-screen of the runner: a 32x24 grid of 8-pixel tiles, plus the
/// hazards and the bell.
///
/// The runner keeps the original's screen-at-a-time structure rather than
/// scrolling. It is not a limitation we are stuck with - Chapter 28 builds the
/// scrolling camera as well - but a single fixed screen is what lets each layout
/// be *composed*: the designer knows exactly what the player can see when they
/// commit to a jump, which is what makes a one-idea-per-screen game teachable.
/// </summary>
public sealed class HunchLevel
{
    public const int Tile8 = 8;
    public const int Cols = 32;
    public const int Rows = 24;
    public const int HudRows = 2;
    public const int PixelWidth = Cols * Tile8;    // 256
    public const int PixelHeight = Rows * Tile8;   // 192

    private readonly HTile[,] _grid = new HTile[Cols, Rows];

    public string Name { get; private set; }
    public int TimeLimit { get; private set; }
    public Vector2 PlayerStart { get; private set; }
    public Vector2 BellPos { get; private set; }
    public List<RopeAnchor> Ropes { get; } = new();
    public List<Vector2> FirePits { get; } = new();
    public List<Vector2> Guards { get; } = new();
    public List<Vector2> ArrowSlits { get; } = new();
    public List<Vector2> FireBalls { get; } = new();

    public HTile At(int c, int r)
        => c < 0 || r < 0 || c >= Cols || r >= Rows ? HTile.Empty : _grid[c, r];

    public bool IsSolid(int c, int r)
    {
        var t = At(c, r);
        return t == HTile.Walkway || t == HTile.Stone;
    }

    public bool IsSolidAtPixel(float x, float y) => IsSolid((int)(x / Tile8), (int)(y / Tile8));
    public bool IsSpikeAtPixel(float x, float y) => At((int)(x / Tile8), (int)(y / Tile8)) == HTile.Spikes;

    public static HunchLevel Parse(string name, int time, string[] rows)
    {
        var lvl = new HunchLevel { Name = name, TimeLimit = time };
        for (int r = 0; r < Math.Min(Rows, rows.Length); r++)
        {
            var line = rows[r];
            for (int c = 0; c < Math.Min(Cols, line.Length); c++)
            {
                float px = c * Tile8, py = r * Tile8;
                switch (line[c])
                {
                    case '=': lvl._grid[c, r] = HTile.Walkway; break;
                    case '#': lvl._grid[c, r] = HTile.Stone; break;
                    case '^': lvl._grid[c, r] = HTile.Spikes; break;
                    case 'P': lvl.PlayerStart = new Vector2(px + 4, py + Tile8); break;
                    case 'B': lvl.BellPos = new Vector2(px, py); break;
                    case 'F': lvl.FirePits.Add(new Vector2(px, py)); break;
                    case 'g': lvl.Guards.Add(new Vector2(px + 8, py + Tile8)); break;
                    case 'a': lvl.ArrowSlits.Add(new Vector2(px, py)); break;
                    case 'x': lvl.FireBalls.Add(new Vector2(px + 4, py + Tile8)); break;
                    case 'r':
                        // A rope hangs from its anchor down to just above head
                        // height over the gap it crosses.
                        lvl.Ropes.Add(new RopeAnchor(new Vector2(px + 4, py), 72f));
                        break;
                }
            }
        }
        return lvl;
    }

    public void Draw(SpriteBatch batch, Texture2D tiles)
    {
        for (int r = HudRows; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var t = _grid[c, r];
                if (t == HTile.Empty) continue;
                int index = t switch
                {
                    HTile.Walkway => 2,
                    HTile.Stone => 0,
                    _ => 0
                };
                batch.Draw(tiles, new Rectangle(c * Tile8, r * Tile8, Tile8, Tile8),
                           new Rectangle(index * Tile8, 0, Tile8, Tile8), Color.White);
            }
        }
        // Battlement teeth along the top edge of every walkway run.
        for (int c = 0; c < Cols; c++)
        {
            for (int r = HudRows; r < Rows; r++)
            {
                if (_grid[c, r] == HTile.Walkway && c % 3 == 0)
                    batch.Draw(tiles, new Rectangle(c * Tile8, r * Tile8 - Tile8, Tile8, Tile8),
                               new Rectangle(Tile8, 0, Tile8, Tile8), Color.White * 0.9f);
            }
        }
    }
}
