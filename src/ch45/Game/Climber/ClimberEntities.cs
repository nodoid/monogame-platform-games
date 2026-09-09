using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Climber;

/// <summary>
/// A rolling barrel.
///
/// On a sloping girder a barrel accelerates downhill, which is the behaviour the
/// original is remembered for and the reason chapter 13 changed this book's
/// girders from tiles to beams. It reaches the low end faster than the player
/// can walk, falls to the girder below, and adopts that one's slope - so the
/// zigzag descent comes out of the geometry rather than out of a rule.
///
/// The other thing that makes barrels feel alive is the ladder decision: rolled
/// once per ladder, and biased upward when the player is close and below, which
/// is what turns randomness into something that reads as intent.
/// </summary>
public sealed class Barrel
{
    public const float RollSpeed = 40f;
    public const float SlopeBoost = 190f;     // extra px/s per unit of gradient
    public const float LadderSpeed = 52f;
    public const float Gravity = 460f;
    public const int Size = 16;

    public Vector2 Position;
    public bool Alive = true;
    public bool Falling;
    public bool OnLadder;
    public bool Wild;
    public bool ScoredJump;
    public int Direction = 1;

    private readonly ClimberLevel _level;
    private readonly Animation _anim;
    private float _vy;
    private int _lastLadderCol = -1;

    public Barrel(ClimberLevel level, Assets assets, Vector2 pos, int direction)
    {
        _level = level;
        _anim = new Animation(assets.Texture("barrel"), Size, Size, 12f);
        Position = pos;
        Direction = direction;
    }

    public Rectangle Bounds => new((int)Position.X - 7, (int)Position.Y - 14, 14, 14);

    public void Update(float dt, Random rng, float playerX, float playerY, float ladderChance)
    {
        _anim.Update(dt);

        if (OnLadder)
        {
            Position.Y += LadderSpeed * dt;
            if (!_level.IsLadderAtPixel(Position.X, Position.Y + 2))
            {
                OnLadder = false;
                Falling = true;
                _vy = 0f;
            }
            return;
        }

        if (Falling)
        {
            float prevY = Position.Y;
            _vy += Gravity * dt;
            Position.Y += _vy * dt;
            if (_level.TryGetBeamBelow(Position.X, prevY, out var landed, out float surface) &&
                Position.Y >= surface)
            {
                Position.Y = surface;
                Falling = false;
                _vy = 0f;
                Direction = landed.RollDirection;
            }
            else if (Position.Y > ClimberLevel.PixelHeight + Size)
            {
                Alive = false;
            }
            return;
        }

        if (!_level.TryGetGround(Position.X, Position.Y, out var beam, out float y))
        {
            Falling = true;
            _vy = 0f;
            return;
        }

        Position.Y = y;
        Direction = beam.RollDirection;

        // Downhill is faster. A barrel on a steep girder outruns the player.
        float gradient = beam.Gradient * Direction;         // positive when descending
        float speed = (RollSpeed + MathF.Max(0f, gradient) * SlopeBoost) * (Wild ? 1.3f : 1f);
        Position.X += Direction * speed * dt;

        if (Position.X < -Size || Position.X > ClimberLevel.PixelWidth + Size) { Alive = false; return; }

        // Ladder decision, taken once per ladder.
        int col = (int)(Position.X / ClimberLevel.Tile8);
        if (col != _lastLadderCol && _level.IsLadderAtPixel(Position.X, Position.Y + 4))
        {
            _lastLadderCol = col;
            float chance = ladderChance;
            // Bias towards the ladder nearest the player, so the hazard is aimed.
            if (Math.Abs(playerX - Position.X) < 40f && playerY > Position.Y) chance += 0.25f;
            if (rng.NextDouble() < chance)
            {
                OnLadder = true;
                Wild = true;
                Position.X = col * ClimberLevel.Tile8 + ClimberLevel.Tile8 / 2f;
            }
        }
    }

    public void Draw(SpriteBatch batch)
        => _anim.Draw(batch, Position, Direction < 0, Wild ? new Color(255, 200, 140) : Color.White);
}

/// <summary>
/// A fireball - the original's "Fire". It patrols the girder it stands on and
/// will take a ladder towards the player, so it removes the option of standing
/// still rather than ever catching you in a straight line.
/// </summary>
public sealed class Fireball
{
    public const float Speed = 26f;
    public Vector2 Position;
    public bool Alive = true;

    private readonly ClimberLevel _level;
    private readonly Animation _anim;
    private int _dir = 1;
    private float _think;
    private bool _climbing;
    private int _climbDir;

    public Fireball(ClimberLevel level, Assets assets, Vector2 pos)
    {
        _level = level;
        _anim = new Animation(assets.Texture("fireball"), 16, 16, 8f);
        Position = pos;
    }

    public Rectangle Bounds => new((int)Position.X - 6, (int)Position.Y - 13, 12, 13);

    public void Update(float dt, Random rng, Vector2 player)
    {
        _anim.Update(dt);
        _think -= dt;

        if (_climbing)
        {
            Position.Y += _climbDir * Speed * dt;
            bool stillLadder = _level.IsLadderAtPixel(Position.X, Position.Y - 4) ||
                               _level.IsLadderAtPixel(Position.X, Position.Y + 2);
            bool grounded = _level.TryGetGround(Position.X, Position.Y, out _, out float gy);
            if (!stillLadder || (_climbDir > 0 && grounded))
            {
                _climbing = false;
                if (grounded) Position.Y = gy;
            }
            return;
        }

        if (!_level.TryGetGround(Position.X, Position.Y, out _, out float surface))
        {
            // Walked off the end: turn round and step back on.
            _dir = -_dir;
            Position.X += _dir * 3f;
            return;
        }
        Position.Y = surface;
        Position.X += _dir * Speed * dt;

        // Turn before stepping off, not after.
        if (!_level.TryGetGround(Position.X + _dir * 6, Position.Y, out _, out _) ||
            Position.X < 8 || Position.X > ClimberLevel.PixelWidth - 8)
        {
            _dir = -_dir;
            Position.X += _dir * 2f;
        }

        if (_think <= 0f)
        {
            _think = 0.6f + (float)rng.NextDouble() * 0.9f;
            if (_level.IsLadderAtPixel(Position.X, Position.Y + 4) && player.Y > Position.Y + 8 &&
                rng.NextDouble() < 0.5)
            { _climbing = true; _climbDir = 1; }
            else if (_level.IsLadderAtPixel(Position.X, Position.Y - 8) && player.Y < Position.Y - 8 &&
                     rng.NextDouble() < 0.4)
            { _climbing = true; _climbDir = -1; }
            else if (rng.NextDouble() < 0.3)
            { _dir = player.X < Position.X ? -1 : 1; }
        }
    }

    public void Draw(SpriteBatch batch) => _anim.Draw(batch, Position, _dir < 0, Color.White);
}

/// <summary>
/// A cement pie from the 50m factory. It rides the conveyor it was born on and
/// is carried by the belt rather than moving under its own power, so reversing a
/// belt reverses the traffic.
/// </summary>
public sealed class Pie
{
    public Vector2 Position;
    public bool Alive = true;

    private readonly ClimberLevel _level;
    private readonly Animation _anim;
    private int _dir;

    public Pie(ClimberLevel level, Assets assets, Vector2 pos, int dir)
    {
        _level = level;
        _anim = new Animation(assets.Texture("pie"), 16, 12, 6f);
        Position = pos;
        _dir = dir;
    }

    public Rectangle Bounds => new((int)Position.X - 7, (int)Position.Y - 11, 14, 11);

    public void Update(float dt)
    {
        _anim.Update(dt);
        if (_level.TryGetGround(Position.X, Position.Y, out var beam, out float y))
        {
            Position.Y = y;
            if (beam.Conveyor) _dir = beam.BeltDir;
            Position.X += _dir * 34f * dt;
        }
        else
        {
            Position.Y += 120f * dt;
        }
        if (Position.X < -20 || Position.X > ClimberLevel.PixelWidth + 20 ||
            Position.Y > ClimberLevel.PixelHeight + 20) Alive = false;
    }

    public void Draw(SpriteBatch batch) => _anim.Draw(batch, Position, _dir < 0, Color.White);
}

/// <summary>
/// A spring from the 75m stage. It bounces along the top girder and then falls
/// down the right-hand side, which makes the right-hand ladder the most
/// dangerous place on the stage at a predictable rhythm.
/// </summary>
public sealed class Spring
{
    public const float Travel = 46f;
    public Vector2 Position;
    public bool Alive = true;

    private readonly ClimberLevel _level;
    private readonly Texture2D _tex;
    private float _t;
    private readonly float _baseY;
    private bool _falling;
    private float _vy;

    public Spring(ClimberLevel level, Assets assets, Vector2 pos)
    {
        _level = level;
        _tex = assets.Texture("spring");
        Position = pos;
        _baseY = pos.Y;
    }

    public Rectangle Bounds => new((int)Position.X - 6, (int)Position.Y - 12, 12, 12);

    public void Update(float dt)
    {
        if (_falling)
        {
            _vy += 420f * dt;
            Position.Y += _vy * dt;
            if (Position.Y > ClimberLevel.PixelHeight + 20) Alive = false;
            return;
        }

        _t += dt;
        Position.X -= Travel * dt;
        // A bounce every third of a second: the arc is what the player reads.
        Position.Y = _baseY - MathF.Abs(MathF.Sin(_t * 9f)) * 14f;

        // Fall off the end of the run it is bouncing along, not off the screen.
        if (!_level.TryGetGround(Position.X, _baseY, out _, out _))
        {
            _falling = true;
            _vy = 0f;
        }
    }

    public void Draw(SpriteBatch batch)
    {
        int squash = Position.Y > _baseY - 2f ? 2 : 0;
        batch.Draw(_tex, new Rectangle((int)Position.X - 6, (int)Position.Y - 12 + squash,
                                       12, 12 - squash), Color.White);
    }
}

/// <summary>
/// A 75m elevator: a platform that runs up or down a shaft and wraps around.
/// It contributes a beam to <see cref="ClimberLevel.Dynamic"/> every frame, which
/// is how a rider ends up standing on this frame's position rather than last
/// frame's.
/// </summary>
public sealed class Elevator
{
    public const float Width = 24f;

    public Vector2 Position;
    private readonly float _bottom, _top, _speed;

    public Elevator(Vector2 bottom, float height, float speed)
    {
        Position = bottom;
        _bottom = bottom.Y;
        _top = bottom.Y - height;
        _speed = speed;
    }

    public void Update(float dt)
    {
        Position.Y -= _speed * dt;
        if (_speed > 0f && Position.Y < _top) Position.Y = _bottom;
        if (_speed < 0f && Position.Y > _bottom) Position.Y = _top;
    }

    public Beam AsBeam()
        => new(Position.X - Width / 2f, Position.X + Width / 2f, Position.Y, Position.Y, 1);

    public void Draw(SpriteBatch batch, Texture2D tiles)
    {
        for (float x = Position.X - Width / 2f; x < Position.X + Width / 2f; x += 8)
            batch.Draw(tiles, new Rectangle((int)x, (int)Position.Y, 8, 8),
                       new Rectangle(0, 0, 8, 8), Color.White);
    }
}

/// <summary>A hammer waiting to be collected. Bobs so it reads as a pickup.</summary>
public sealed class Pickup
{
    public Vector2 Position;
    public bool Taken;
    private readonly Texture2D _tex;
    private float _t;

    public Pickup(Texture2D tex, Vector2 pos) { _tex = tex; Position = pos; }

    public Rectangle Bounds => new((int)Position.X, (int)Position.Y - 12, 12, 12);

    public void Update(float dt) => _t += dt;

    public void Draw(SpriteBatch batch)
    {
        if (Taken) return;
        int bob = (int)(MathF.Sin(_t * 4f) * 1.5f);
        batch.Draw(_tex, new Rectangle((int)Position.X, (int)Position.Y - 12 + bob, 12, 12),
                   Color.White);
    }
}

/// <summary>The gorilla: he beats his chest, and every few seconds sends a barrel down.</summary>
public sealed class GorillaActor
{
    public Vector2 Position;
    private readonly Animation _anim;
    private float _throwTimer;

    public GorillaActor(Assets assets, Vector2 pos)
    {
        _anim = new Animation(assets.Texture("gorilla"), 32, 32, 3f);
        Position = pos;
    }

    public bool Update(float dt, float interval)
    {
        _anim.Update(dt);
        _throwTimer += dt;
        if (_throwTimer >= interval) { _throwTimer = 0f; return true; }
        return false;
    }

    public void Draw(SpriteBatch batch) => _anim.DrawAt(batch, Position, false, Color.White);
}

/// <summary>The goal, waving at the top of the structure.</summary>
public sealed class Princess
{
    public Vector2 Position;
    private readonly Animation _anim;
    public Princess(Assets assets, Vector2 pos)
    {
        _anim = new Animation(assets.Texture("princess"), 16, 16, 2f);
        Position = pos;
    }
    public Rectangle Bounds => new((int)Position.X, (int)Position.Y, 16, 16);
    public void Update(float dt) => _anim.Update(dt);
    public void Draw(SpriteBatch batch) => _anim.DrawAt(batch, Position, false, Color.White);
}
