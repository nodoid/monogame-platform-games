using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Climber;

public enum Tile : byte { Empty, Ladder, LadderBroken, Oil }

/// <summary>
/// A girder: a straight run with a surface that may slope.
///
/// The original's girders tilt, and hazards accelerate down them. Chapter 13
/// explains why this book models them as beams with a linear surface rather
/// than as solid tiles: a sloped surface cannot be expressed in an eight-pixel
/// grid without either a staircase you can feel or a tile type per angle.
///
/// A beam owns the direction hazards roll along it. On a sloped beam that is
/// downhill; on a flat one it is set by the stage.
/// </summary>
public readonly struct Beam
{
    public readonly float X0, X1;      // pixel extent, inclusive of X0
    public readonly float Y0, Y1;      // surface height at each end
    public readonly int Direction;     // +1 rolls right, -1 rolls left
    public readonly bool Conveyor;
    public readonly int BeltDir;

    public Beam(float x0, float x1, float y0, float y1, int direction,
                bool conveyor = false, int beltDir = 0)
    {
        X0 = x0; X1 = x1; Y0 = y0; Y1 = y1;
        Direction = direction;
        Conveyor = conveyor;
        BeltDir = beltDir;
    }

    public bool SpansX(float x) => x >= X0 - 1f && x <= X1 + 1f;

    /// <summary>Surface height at a horizontal position, linearly interpolated.</summary>
    public float SurfaceAt(float x)
    {
        if (X1 <= X0) return Y0;
        float t = Math.Clamp((x - X0) / (X1 - X0), 0f, 1f);
        return Y0 + (Y1 - Y0) * t;
    }

    /// <summary>Pixels of fall per pixel travelled. Positive means downhill to the right.</summary>
    public float Gradient => X1 > X0 ? (Y1 - Y0) / (X1 - X0) : 0f;

    /// <summary>The way a rolling hazard goes: downhill, or the stage's choice if flat.</summary>
    public int RollDirection => MathF.Abs(Y1 - Y0) < 0.5f ? Direction : (Y1 > Y0 ? 1 : -1);
}

/// <summary>
/// One stage of the climber: a list of sloped beams, a tile grid holding only
/// the things that are not beams - ladders and the oil drum - and the markers
/// the play screen turns into entities.
/// </summary>
public sealed class ClimberLevel
{
    public const int Tile8 = 8;
    public const int Cols = 28;
    public const int Rows = 32;
    public const int HudRows = 3;

    public const int PixelWidth = Cols * Tile8;    // 224
    public const int PixelHeight = Rows * Tile8;   // 256

    /// <summary>How close under the feet a beam must be to count as ground.</summary>
    public const float SnapTolerance = 4f;

    private readonly Tile[,] _grid = new Tile[Cols, Rows];

    public string Name { get; private set; }
    public int Metres { get; private set; }
    public int StartBonus { get; private set; }
    public List<Beam> Beams { get; } = new();

    /// <summary>
    /// Beams that move: the 75m elevators. The play screen clears and refills
    /// this every update before anything queries the ground, so a rider is
    /// standing on the platform's position this frame rather than last frame's.
    /// </summary>
    public List<Beam> Dynamic { get; } = new();

    public Vector2 PlayerStart { get; private set; }
    public Vector2 GorillaPos { get; private set; }
    public Vector2 PrincessPos { get; private set; }
    public Vector2 BarrelSpawn { get; private set; }
    public List<Vector2> HammerSpots { get; } = new();
    public List<Vector2> FireballSpots { get; } = new();
    public List<Vector2> Rivets { get; } = new();
    public List<Vector2> PieSpawns { get; } = new();
    public List<Vector2> SpringSpawns { get; } = new();
    public List<(Vector2 Bottom, float Height, float Speed)> Elevators { get; } = new();
    public Vector2 OilDrum { get; private set; }
    public bool HasOilDrum { get; private set; }

    // --------------------------------------------------------------- tiles ---
    public Tile At(int col, int row)
        => col < 0 || row < 0 || col >= Cols || row >= Rows ? Tile.Empty : _grid[col, row];

    public void Set(int col, int row, Tile t)
    {
        if (col >= 0 && row >= 0 && col < Cols && row < Rows) _grid[col, row] = t;
    }

    public bool IsLadder(int col, int row)
    {
        var t = At(col, row);
        return t == Tile.Ladder || t == Tile.LadderBroken;
    }

    public bool IsLadderAtPixel(float x, float y) => IsLadder((int)(x / Tile8), (int)(y / Tile8));

    // --------------------------------------------------------------- beams ---
    /// <summary>
    /// The beam supporting a point, if any. A beam counts when the point is
    /// within <see cref="SnapTolerance"/> above its surface and not far below.
    /// </summary>
    public bool TryGetGround(float x, float y, out Beam beam, out float surfaceY)
    {
        bool found = false;
        beam = default;
        surfaceY = 0f;
        float best = float.MaxValue;

        for (int pass = 0; pass < 2; pass++)
        {
            var list = pass == 0 ? Beams : Dynamic;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (!b.SpansX(x)) continue;
                float s = b.SurfaceAt(x);
                float gap = s - y;                   // positive: surface below the point
                if (gap < -SnapTolerance || gap > SnapTolerance) continue;
                if (MathF.Abs(gap) < best)
                {
                    best = MathF.Abs(gap);
                    beam = b;
                    surfaceY = s;
                    found = true;
                }
            }
        }
        return found;
    }

    /// <summary>The first beam surface strictly below a point - what a fall lands on.</summary>
    public bool TryGetBeamBelow(float x, float y, out Beam beam, out float surfaceY)
    {
        bool found = false;
        beam = default;
        surfaceY = 0f;
        float best = float.MaxValue;

        for (int pass = 0; pass < 2; pass++)
        {
            var list = pass == 0 ? Beams : Dynamic;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (!b.SpansX(x)) continue;
                float s = b.SurfaceAt(x);
                if (s < y - 0.5f) continue;
                if (s - y < best) { best = s - y; beam = b; surfaceY = s; found = true; }
            }
        }
        return found;
    }

    public bool IsSolidAtPixel(float x, float y) => TryGetGround(x, y, out _, out _);

    /// <summary>Conveyor drift under a point, in pixels per second.</summary>
    public float ConveyorDriftAt(float x, float y)
    {
        if (!TryGetGround(x, y, out var b, out _)) return 0f;
        return b.Conveyor ? b.BeltDir * 22f : 0f;
    }

    // -------------------------------------------------------------- build ---
    public static ClimberLevel Create(string name, int metres, int bonus)
        => new() { Name = name, Metres = metres, StartBonus = bonus };

    public ClimberLevel Beam_(float x0, float x1, float y0, float y1, int dir,
                           bool conveyor = false, int beltDir = 0)
    {
        Beams.Add(new Beam(x0, x1, y0, y1, dir, conveyor, beltDir));
        return this;
    }

    /// <summary>
    /// A ladder run between two beam surfaces. A broken ladder is a stub rising
    /// from the lower girder that stops short - you can climb on to it and then
    /// you are stuck, which costs time rather than a life.
    /// </summary>
    public ClimberLevel Ladder_(int col, float topY, float bottomY, bool broken = false)
    {
        int r0 = (int)MathF.Floor(topY / Tile8);
        int r1 = (int)MathF.Ceiling(bottomY / Tile8) - 1;
        for (int r = r0; r <= r1; r++)
        {
            if (broken && r < r1 - 1) continue;          // the top of a broken run is missing
            Set(col, r, broken ? Tile.LadderBroken : Tile.Ladder);
        }
        return this;
    }

    public ClimberLevel Player(float x, float y) { PlayerStart = new Vector2(x, y); return this; }
    public ClimberLevel Gorilla(float x, float y) { GorillaPos = new Vector2(x, y); return this; }
    public ClimberLevel Girl(float x, float y) { PrincessPos = new Vector2(x, y); return this; }
    public ClimberLevel Barrels(float x, float y) { BarrelSpawn = new Vector2(x, y); return this; }
    public ClimberLevel Hammer(float x, float y) { HammerSpots.Add(new Vector2(x, y)); return this; }
    public ClimberLevel Fire(float x, float y) { FireballSpots.Add(new Vector2(x, y)); return this; }
    public ClimberLevel Rivet(float x, float y) { Rivets.Add(new Vector2(x, y)); return this; }
    public ClimberLevel Pies(float x, float y) { PieSpawns.Add(new Vector2(x, y)); return this; }
    public ClimberLevel Springs(float x, float y) { SpringSpawns.Add(new Vector2(x, y)); return this; }
    public ClimberLevel Oil(float x, float y) { OilDrum = new Vector2(x, y); HasOilDrum = true; return this; }

    public ClimberLevel Elevator(float x, float bottomY, float height, float speed)
    {
        Elevators.Add((new Vector2(x, bottomY), height, speed));
        return this;
    }

    // --------------------------------------------------------------- draw ---
    /// <summary>
    /// Beams are drawn as a run of 8x8 tiles stepped to follow the surface, so a
    /// shallow slope reads as a tilted girder without needing rotated art.
    /// </summary>
    public void Draw(SpriteBatch batch, Texture2D tiles, float conveyorPhase, float slopeScale = 1f)
    {
        foreach (var b in Beams)
        {
            int index = b.Conveyor ? 3 + (int)(conveyorPhase % 2f) : 0;
            for (float x = b.X0; x <= b.X1; x += Tile8)
            {
                float flat = (b.Y0 + b.Y1) * 0.5f;
                float sy = flat + (b.SurfaceAt(x) - flat) * slopeScale;
                batch.Draw(tiles, new Rectangle((int)x, (int)MathF.Round(sy), Tile8, Tile8),
                           new Rectangle(index * Tile8, 0, Tile8, Tile8), Color.White);
            }
        }

        for (int r = HudRows; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var t = _grid[c, r];
                if (t == Tile.Empty) continue;
                int index = t switch
                {
                    Tile.Ladder => 1,
                    Tile.LadderBroken => 2,
                    _ => 6
                };
                batch.Draw(tiles, new Rectangle(c * Tile8, r * Tile8, Tile8, Tile8),
                           new Rectangle(index * Tile8, 0, Tile8, Tile8), Color.White);
            }
        }

        foreach (var v in Rivets)
            batch.Draw(tiles, new Rectangle((int)v.X, (int)v.Y - 8, Tile8, Tile8),
                       new Rectangle(5 * Tile8, 0, Tile8, Tile8), Color.White);
    }
}
