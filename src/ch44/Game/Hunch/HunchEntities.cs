using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

/// <summary>
/// A hanging rope, simulated as a rigid pendulum.
///
/// The equation is the textbook one - angular acceleration is -(g/L)*sin(theta)
/// - and it is worth using the real thing rather than a sine wave, because a
/// real pendulum is slow at the ends of its arc and fast through the bottom.
/// That timing is what the player learns to read, and a cosmetic sine loses it.
/// Damping stops a rope swinging forever; pumping lets a player add energy back.
/// </summary>
public sealed class Rope
{
    private const float G = 900f;
    private const float Damping = 0.35f;
    private const float PumpForce = 2.6f;
    private const float MaxAngle = 1.25f;      // about 72 degrees

    public Vector2 Top { get; }
    public float Length { get; }
    public float Angle { get; private set; }
    public float AngularVelocity { get; private set; }
    public bool Held { get; private set; }

    private readonly Texture2D _tex;

    public Rope(Texture2D ropeTexture, RopeAnchor anchor)
    {
        _tex = ropeTexture;
        Top = anchor.Top;
        Length = anchor.Length;
    }

    public Vector2 EndPoint => Top + new Vector2(MathF.Sin(Angle), MathF.Cos(Angle)) * Length;

    /// <summary>Generous: a rope you nearly reached should catch you.</summary>
    public Rectangle GrabBox
    {
        get
        {
            var e = EndPoint;
            return new Rectangle((int)e.X - 8, (int)e.Y - 26, 16, 30);
        }
    }

    public void Grab(float horizontalSpeed)
    {
        Held = true;
        // Convert the arriving horizontal speed into angular speed. Divide by
        // length because a long rope turns the same linear speed into a slower
        // sweep - which is exactly how it should feel.
        AngularVelocity += horizontalSpeed / Length;
    }

    public void Release() => Held = false;

    /// <summary>Tangential velocity at the current angle - the launch vector.</summary>
    public Vector2 ReleaseVelocity()
    {
        float speed = AngularVelocity * Length;
        return new Vector2(MathF.Cos(Angle), -MathF.Sin(Angle)) * speed;
    }

    public void Update(float dt, float pump)
    {
        float accel = -(G / Length) * MathF.Sin(Angle);
        accel -= Damping * AngularVelocity;
        if (Held) accel += pump * PumpForce;

        AngularVelocity += accel * dt;
        Angle += AngularVelocity * dt;

        if (Angle > MaxAngle) { Angle = MaxAngle; AngularVelocity = MathF.Min(0f, AngularVelocity); }
        if (Angle < -MaxAngle) { Angle = -MaxAngle; AngularVelocity = MathF.Max(0f, AngularVelocity); }

        if (!Held)
        {
            // Settle an untouched rope so screens do not start in chaos.
            AngularVelocity *= 1f - MathF.Min(1f, dt * 0.8f);
        }
    }

    public void Draw(SpriteBatch batch)
    {
        int segments = (int)(Length / 8f);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            var p = Top + new Vector2(MathF.Sin(Angle), MathF.Cos(Angle)) * (Length * t);
            batch.Draw(_tex, new Rectangle((int)p.X - 2, (int)p.Y - 4, 4, 8), Color.White);
        }
    }
}

/// <summary>An arrow from a slit in the far wall. Fast, flat, and telegraphed by sound.</summary>
public sealed class Arrow
{
    public const float Speed = 118f;
    public Vector2 Position;
    public int Direction;
    public bool Alive = true;

    private readonly Texture2D _tex;

    public Arrow(Texture2D tex, Vector2 pos, int direction)
    {
        _tex = tex; Position = pos; Direction = direction;
    }

    public Rectangle Bounds => new((int)Position.X + 2, (int)Position.Y + 1, 12, 4);

    public void Update(float dt)
    {
        Position.X += Direction * Speed * dt;
        if (Position.X < -20 || Position.X > HunchLevel.PixelWidth + 20) Alive = false;
    }

    public void Draw(SpriteBatch batch)
        => batch.Draw(_tex, new Rectangle((int)Position.X, (int)Position.Y, 16, 6), null,
                      Color.White, 0f, Vector2.Zero,
                      Direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
}

/// <summary>A guard pacing a fixed beat. He never chases - his only job is to
/// occupy a piece of ground on a schedule the player can learn.</summary>
public sealed class Guard
{
    public const float Speed = 26f;
    public Vector2 Position;
    private readonly HunchLevel _level;
    private readonly Animation _anim;
    private int _dir = -1;
    private readonly float _minX, _maxX;

    public Guard(HunchLevel level, Assets assets, Vector2 pos, float range = 44f)
    {
        _level = level;
        _anim = new Animation(assets.Texture("guard"), 16, 16, 5f);
        Position = pos;
        _minX = MathF.Max(10f, pos.X - range);
        _maxX = MathF.Min(HunchLevel.PixelWidth - 10f, pos.X + range);
    }

    public Rectangle Bounds => new((int)Position.X - 5, (int)Position.Y - 15, 10, 15);

    public void Update(float dt)
    {
        _anim.Update(dt);
        Position.X += _dir * Speed * dt;
        // Turn at the patrol limit or at the edge of the floor, whichever first.
        if (Position.X <= _minX || Position.X >= _maxX ||
            !_level.IsSolidAtPixel(Position.X + _dir * 7, Position.Y + 1))
        {
            _dir = -_dir;
            Position.X += _dir * 2f;
        }
    }

    public void Draw(SpriteBatch batch) => _anim.Draw(batch, Position, _dir > 0, Color.White);
}

/// <summary>
/// A bouncing fireball - the original's signature moving hazard.
///
/// It travels at a constant horizontal speed and bounces off the walkway on a
/// fixed period, which makes it a *timing* problem rather than a chase: the
/// player has to pass under it at the top of a bounce or wait for it to go by.
/// It is faster than a knight and slower than an arrow, which puts it in the one
/// speed band nothing else in the game occupies.
/// </summary>
public sealed class FireBall
{
    public const float Speed = 74f;
    public const float BounceHeight = 26f;
    public const float BouncePeriod = 0.72f;

    public Vector2 Position;
    public bool Alive = true;

    private readonly HunchLevel _level;
    private readonly Animation _anim;
    private readonly int _dir;
    private float _t;

    public FireBall(HunchLevel level, Assets assets, Vector2 pos, int dir)
    {
        _level = level;
        _anim = new Animation(assets.Texture("fireball_h"), 12, 12, 10f);
        Position = pos;
        _dir = dir;
        _t = 0f;
    }

    public Rectangle Bounds => new((int)Position.X - 4, (int)Position.Y - 10, 8, 9);

    public void Update(float dt)
    {
        _t += dt;
        _anim.Update(dt);
        Position.X += _dir * Speed * dt;

        // A half-sine bounce: on the ground at the ends of each period, at the
        // top in the middle. The player reads the arc, not the position.
        float phase = (_t % BouncePeriod) / BouncePeriod;
        float lift = MathF.Sin(phase * MathF.PI) * BounceHeight;

        float ground = HunchLevel.PixelHeight;
        for (int r = HunchLevel.HudRows; r < HunchLevel.Rows; r++)
        {
            if (_level.IsSolid((int)(Position.X / HunchLevel.Tile8), r))
            { ground = r * HunchLevel.Tile8; break; }
        }
        Position.Y = ground - lift;

        if (Position.X < -20 || Position.X > HunchLevel.PixelWidth + 20 ||
            Position.Y > HunchLevel.PixelHeight + 24) Alive = false;
    }

    public void Draw(SpriteBatch batch) => _anim.Draw(batch, Position, _dir < 0, Color.White);
}

/// <summary>A brazier. Static, lethal, and animated so it never reads as scenery.</summary>
public sealed class FirePit
{
    public Vector2 Position;
    private readonly Animation _anim;

    public FirePit(Assets assets, Vector2 pos)
    {
        _anim = new Animation(assets.Texture("firepit"), 16, 16, 10f);
        Position = pos;
    }

    public Rectangle Bounds => new((int)Position.X + 2, (int)Position.Y + 4, 12, 12);

    public void Update(float dt) => _anim.Update(dt);
    public void Draw(SpriteBatch batch) => _anim.DrawAt(batch, Position, false, Color.White);
}

/// <summary>The bell at the end of every screen: the goal, and the reward.</summary>
public sealed class Bell
{
    public Vector2 Position;
    public bool Rung;

    private readonly Animation _anim;
    private float _ring;

    public Bell(Assets assets, Vector2 pos)
    {
        _anim = new Animation(assets.Texture("bell"), 16, 16, 8f);
        _anim.SetRange(0, 0);
        Position = pos;
    }

    public Rectangle Bounds => new((int)Position.X - 2, (int)Position.Y, 20, 18);

    public void Ring()
    {
        if (Rung) return;
        Rung = true;
        _ring = 1.2f;
        _anim.SetRange(1, 2);
    }

    public void Update(float dt)
    {
        if (_ring > 0f)
        {
            _ring -= dt;
            _anim.Update(dt);
            if (_ring <= 0f) _anim.SetRange(0, 0);
        }
    }

    public void Draw(SpriteBatch batch) => _anim.DrawAt(batch, Position, false, Color.White);
}
