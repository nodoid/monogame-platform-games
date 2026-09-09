using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Hunch;

public enum RunState { Run, Air, Swing, Dying, Won }

/// <summary>
/// The runner's player character.
///
/// Where the climber's jump is deliberately rigid, this one is deliberately
/// forgiving, because the game asks for precision instead of planning:
///
///  - Variable height. Releasing the button early cuts the upward velocity, so
///    a tap is a hop and a hold is a full leap. One button, two jumps.
///  - Coyote time. For 90ms after walking off an edge the jump still works.
///    Players genuinely believe they pressed it in time; the recording says
///    otherwise, and the recording is not the one buying the game.
///  - Jump buffering, in <see cref="InputState"/>: a press up to 120ms before
///    landing is remembered and fires on touchdown.
///  - Apex hang. Gravity is reduced near the top of the arc, which gives the
///    player a beat to read where they will land.
///
/// None of these change what is possible - the gaps are all still cleared by the
/// same nominal arc. They change how often a player who did the right thing gets
/// the right result, which is a completely different quantity.
/// </summary>
public sealed class HunchRunner
{
    public const float MaxRun = 80f;
    public const float RunAccel = 460f;
    public const float Friction = 560f;
    public const float AirControl = 0.55f;
    public const float Gravity = 660f;
    public const float ApexGravity = 420f;      // softer near the top of the arc
    public const float ApexBand = 40f;          // |vy| under this counts as apex
    public const float JumpVelocity = -216f;
    public const float CutMultiplier = 0.42f;   // applied when the button is released early
    public const float CoyoteTime = 0.09f;
    public const float MaxFall = 420f;

    public const int HalfWidth = 4;
    public const int Height = 16;

    public Vector2 Position;
    public Vector2 Velocity;
    public RunState State = RunState.Run;
    public bool FacingLeft;
    public Rope AttachedRope;

    private readonly HunchLevel _level;
    private readonly Animation _run, _jump, _swing, _dead;
    private float _coyote;
    private bool _jumpHeld;
    private float _stepTimer;
    private float _deathTimer;

    public float DeathTimer => _deathTimer;

    public HunchRunner(HunchLevel level, Assets assets)
    {
        _level = level;
        _run = new Animation(assets.Texture("quasi_run"), 14, 16, 12f);
        _jump = new Animation(assets.Texture("quasi_jump"), 14, 16, 1f);
        _swing = new Animation(assets.Texture("quasi_swing"), 14, 16, 6f);
        _dead = new Animation(assets.Texture("quasi_dead"), 14, 16, 3f);
        Position = level.PlayerStart;
    }

    public Rectangle Bounds => new((int)Position.X - HalfWidth, (int)Position.Y - Height,
                                   HalfWidth * 2, Height);

    /// <summary>A slightly smaller box for lethal contact - see Chapter 42.</summary>
    public Rectangle HurtBox => new((int)Position.X - HalfWidth + 1, (int)Position.Y - Height + 3,
                                    HalfWidth * 2 - 2, Height - 4);

    public bool Grounded =>
        _level.IsSolidAtPixel(Position.X, Position.Y + 1) ||
        _level.IsSolidAtPixel(Position.X - HalfWidth + 1, Position.Y + 1) ||
        _level.IsSolidAtPixel(Position.X + HalfWidth - 1, Position.Y + 1);

    public void Kill()
    {
        if (State == RunState.Dying) return;
        State = RunState.Dying;
        Velocity = new Vector2(0, -90f);
        AttachedRope?.Release();
        AttachedRope = null;
        _deathTimer = 0f;
        _dead.Reset();
    }

    public void Update(float dt, InputState input, out bool step, out bool jumped, out bool landed)
    {
        step = jumped = landed = false;

        if (State == RunState.Dying)
        {
            _deathTimer += dt;
            Velocity.Y += Gravity * dt;
            Position += Velocity * dt;
            _dead.Update(dt);
            return;
        }
        if (State == RunState.Won) { _run.SetRange(0, 0); return; }

        if (State == RunState.Swing) { UpdateSwing(dt, input, ref jumped); return; }

        bool left = input.Down(Btn.Left), right = input.Down(Btn.Right);
        float target = 0f;
        if (left && !right) { target = -MaxRun; FacingLeft = true; }
        else if (right && !left) { target = MaxRun; FacingLeft = false; }

        float accel = (Grounded ? RunAccel : RunAccel * AirControl) * dt;
        if (target != 0f)
            Velocity.X = MoveTowards(Velocity.X, target, accel);
        else
            Velocity.X = MoveTowards(Velocity.X, 0f, Friction * dt * (Grounded ? 1f : 0.35f));

        // ---- gravity, with an easier apex ----------------------------------
        float g = MathF.Abs(Velocity.Y) < ApexBand ? ApexGravity : Gravity;
        Velocity.Y = MathF.Min(MaxFall, Velocity.Y + g * dt);

        // ---- coyote time ----------------------------------------------------
        if (Grounded) _coyote = CoyoteTime;
        else _coyote = MathF.Max(0f, _coyote - dt);

        // ---- jump -----------------------------------------------------------
        if (input.PressedRecently(Btn.Jump) && _coyote > 0f)
        {
            input.ConsumeBuffer(Btn.Jump);
            Velocity.Y = JumpVelocity;
            _coyote = 0f;
            _jumpHeld = true;
            State = RunState.Air;
            jumped = true;
        }
        // Variable height: let go early and the rise is cut short.
        if (_jumpHeld && !input.Down(Btn.Jump))
        {
            _jumpHeld = false;
            if (Velocity.Y < 0f) Velocity.Y *= CutMultiplier;
        }

        MoveAndCollide(dt, ref landed);

        if (Grounded && Velocity.Y >= 0f)
        {
            State = RunState.Run;
            if (MathF.Abs(Velocity.X) > 8f)
            {
                _run.Update(dt);
                _stepTimer -= dt;
                if (_stepTimer <= 0f) { _stepTimer = 0.18f; step = true; }
            }
            else _run.Frame = 0;
        }
        else State = RunState.Air;

        if (Position.Y > HunchLevel.PixelHeight + 24) Kill();
    }

    private static float MoveTowards(float v, float target, float delta)
        => MathF.Abs(target - v) <= delta ? target : v + MathF.Sign(target - v) * delta;

    /// <summary>
    /// Axis-separated collision: move in x and resolve, then move in y and
    /// resolve. Doing both at once and resolving the combined overlap is what
    /// produces the classic bug where running into a wall while falling snaps
    /// you on top of it.
    /// </summary>
    private void MoveAndCollide(float dt, ref bool landed)
    {
        Position.X += Velocity.X * dt;
        if (SolidAtBody(Position.X, Position.Y))
        {
            int dir = MathF.Sign(Velocity.X) == 0 ? 1 : MathF.Sign(Velocity.X);
            while (SolidAtBody(Position.X, Position.Y)) Position.X -= dir;
            Velocity.X = 0f;
        }
        Position.X = Math.Clamp(Position.X, HalfWidth, HunchLevel.PixelWidth - HalfWidth);

        float prevY = Position.Y;
        Position.Y += Velocity.Y * dt;
        if (Velocity.Y > 0f && Grounded)
        {
            // Land on the *top* of the tile the feet have entered. Grounded
            // samples one pixel below the feet, so the tile to stand on is the
            // one at Position.Y + 1; snapping to the row below that would push
            // the runner a tile into the stone, and it would sink a tile a
            // frame until it dropped out of the world.
            Position.Y = MathF.Floor((Position.Y + 1) / HunchLevel.Tile8) * HunchLevel.Tile8;
            if (Velocity.Y > 40f) landed = true;
            Velocity.Y = 0f;
        }
        else if (Velocity.Y < 0f && SolidAtBody(Position.X, Position.Y))
        {
            Position.Y = prevY;
            Velocity.Y = 0f;
        }
    }

    private bool SolidAtBody(float x, float y)
    {
        // Sample the four corners of the body box, one pixel inside.
        return _level.IsSolidAtPixel(x - HalfWidth + 1, y - Height + 1) ||
               _level.IsSolidAtPixel(x + HalfWidth - 1, y - Height + 1) ||
               _level.IsSolidAtPixel(x - HalfWidth + 1, y - 2) ||
               _level.IsSolidAtPixel(x + HalfWidth - 1, y - 2);
    }

    // ------------------------------------------------------------- ropes -----
    public bool TryGrab(Rope rope)
    {
        if (State == RunState.Swing || State == RunState.Dying) return false;
        if (!rope.GrabBox.Intersects(Bounds)) return false;

        AttachedRope = rope;
        // Hand the rope the horizontal speed we arrived with, so a fast approach
        // becomes a wide swing. This is the whole reason a rope feels good.
        rope.Grab(Velocity.X);
        State = RunState.Swing;
        _swing.Reset();
        return true;
    }

    private void UpdateSwing(float dt, InputState input, ref bool jumped)
    {
        var rope = AttachedRope;
        if (rope == null) { State = RunState.Air; return; }

        // Pumping: pushing in the direction of travel adds energy, exactly like
        // a child on a swing. It lets a player who under-shot recover.
        float pump = 0f;
        if (input.Down(Btn.Left)) pump -= 1f;
        if (input.Down(Btn.Right)) pump += 1f;
        rope.Update(dt, pump);

        Position = rope.EndPoint;
        FacingLeft = rope.AngularVelocity < 0f;
        _swing.Update(dt);

        if (input.Pressed(Btn.Jump) || input.Pressed(Btn.Confirm))
        {
            Velocity = rope.ReleaseVelocity() + new Vector2(0f, -90f);
            rope.Release();
            AttachedRope = null;
            State = RunState.Air;
            _jumpHeld = false;
            jumped = true;
        }
    }

    public void Draw(SpriteBatch batch)
    {
        switch (State)
        {
            case RunState.Dying: _dead.Draw(batch, Position, FacingLeft, Color.White); break;
            case RunState.Swing: _swing.Draw(batch, Position, FacingLeft, Color.White); break;
            case RunState.Air: _jump.Draw(batch, Position, FacingLeft, Color.White); break;
            default: _run.Draw(batch, Position, FacingLeft, Color.White); break;
        }
    }
}
