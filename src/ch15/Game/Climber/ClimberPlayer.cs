using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Retro.Climber;

public enum JackState { Walk, Jump, Climb, Dying, Won }

/// <summary>
/// The climber's player character, walking on sloped girders.
///
/// CHAPTER 15 VERSION - walking and climbing. The jump arrives in chapter 16.
///
/// Two decisions dominate the finished file.
///
/// The jump is *committed*: once it leaves the girder the horizontal speed is
/// fixed and the button does nothing. Chapter 16 works through the arithmetic -
/// an apex of 25 pixels against a girder gap of 32 means you cannot jump up a
/// level, and that inequality is what makes the game a climber.
///
/// And the character is seated on a beam surface rather than snapped to a tile
/// grid. Walking sets y from the beam under the feet every frame, so a slope is
/// walked rather than stepped, and chapter 14 explains why that is a different
/// model from the runner's tile collision rather than a refinement of it.
/// </summary>
public sealed class ClimberPlayer
{
    // The numbers live in ClimberTuning so chapter 13's viewer can read them
    // without compiling this class. See ClimberTuning for the reasoning.
    public const float WalkSpeed = ClimberTuning.WalkSpeed;
    public const float ClimbSpeed = ClimberTuning.ClimbSpeed;
    public const float Gravity = ClimberTuning.Gravity;
    public const float JumpVelocity = -ClimberTuning.JumpSpeed;
    public const float FatalFall = ClimberTuning.FatalFall;
    public const float HammerSeconds = ClimberTuning.HammerSeconds;

    public const int HalfWidth = 5;
    public const int Height = 16;

    public Vector2 Position;                 // feet centre
    public Vector2 Velocity;
    public JackState State = JackState.Walk;
    public bool FacingLeft;
    public float HammerTime;
    public bool Alive = true;

    private readonly ClimberLevel _level;
    private readonly Animation _run, _climb, _hammer, _dead;
    private float _fellFrom;
    private float _deathTimer;
    private float _lastGroundX;

    public bool HasHammer => HammerTime > 0f;
    public float DeathTimer => _deathTimer;

    public ClimberPlayer(ClimberLevel level, Assets assets)
    {
        _level = level;
        // Frames: 0 idle, 1-3 run cycle, 4 jump pose.
        _run = new Animation(assets.Texture("jack_run"), 12, 16, 12f).SetRange(1, 3);
        _climb = new Animation(assets.Texture("jack_climb"), 12, 16, 8f);
        _hammer = new Animation(assets.Texture("jack_hammer"), 12, 16, 6f);
        _dead = new Animation(assets.Texture("jack_dead"), 12, 16, 4f);
        Position = level.PlayerStart;
    }

    public Rectangle Bounds => new((int)Position.X - HalfWidth, (int)Position.Y - Height,
                                   HalfWidth * 2, Height);

    /// <summary>
    /// The hammer's own box, live only on the downswing. A weapon that is always
    /// active is a shield; one that is active half the time is a timing problem.
    /// </summary>
    public Rectangle? HammerBounds
    {
        get
        {
            if (!HasHammer || _hammer.Frame != 1) return null;
            int w = 10;
            int x = FacingLeft ? (int)Position.X - HalfWidth - w : (int)Position.X + HalfWidth;
            return new Rectangle(x, (int)Position.Y - Height + 4, w, 12);
        }
    }

    public bool OnGround() => _level.TryGetGround(Position.X, Position.Y, out _, out _);

    private bool OnLadder() => _level.IsLadderAtPixel(Position.X, Position.Y - 4);
    private bool LadderBelow() => _level.IsLadderAtPixel(Position.X, Position.Y + 4);

    public void Kill()
    {
        if (State == JackState.Dying) return;
        State = JackState.Dying;
        Velocity = Vector2.Zero;
        _deathTimer = 0f;
        _dead.Reset();
        HammerTime = 0f;
    }

    public void GiveHammer()
    {
        HammerTime = HammerSeconds;
        if (State == JackState.Climb) State = JackState.Walk;
        _hammer.Reset();
    }

    public void Update(float dt, InputState input, out bool footstep, out bool jumped, out bool landed)
    {
        footstep = jumped = landed = false;

        if (State == JackState.Dying)
        {
            _deathTimer += dt;
            _dead.Update(dt);
            return;
        }
        if (State == JackState.Won) { _run.SetRange(0, 0); return; }

        if (HammerTime > 0f)
        {
            HammerTime -= dt;
            _hammer.Update(dt);
        }

        bool left = input.Down(Btn.Left), right = input.Down(Btn.Right);
        bool up = input.Down(Btn.Up), down = input.Down(Btn.Down);

        switch (State)
        {
            case JackState.Climb: UpdateClimb(dt, up, down, left, right); break;
            case JackState.Jump: UpdateAirborne(dt, ref landed); break;
            default: UpdateWalk(dt, input, left, right, up, down, ref footstep, ref jumped); break;
        }

        Position.X = Math.Clamp(Position.X, HalfWidth, ClimberLevel.PixelWidth - HalfWidth);
        if (Position.Y > ClimberLevel.PixelHeight + 32) Kill();
    }

    private void UpdateWalk(float dt, InputState input, bool left, bool right, bool up, bool down,
                            ref bool footstep, ref bool jumped)
    {
        if (!_level.TryGetGround(Position.X, Position.Y, out var beam, out float surface))
        {
            State = JackState.Jump;
            _fellFrom = Position.Y;
            return;
        }

        // Seat the feet on the beam. On a slope this is what makes the character
        // walk down the girder rather than off the end of the first tile.
        Position.Y = surface;
        _lastGroundX = Position.X;

        float move = 0f;
        if (left && !right) { move = -1f; FacingLeft = true; }
        else if (right && !left) { move = 1f; FacingLeft = false; }

        // A conveyor drags whether or not the player is walking.
        float belt = beam.Conveyor ? beam.BeltDir * 22f : 0f;
        Position.X += (move * WalkSpeed + belt) * dt;

        if (move != 0f)
        {
            int before = _run.Frame;
            _run.SetRange(1, 3);
            _run.Update(dt);
            if (_run.Frame != before && (_run.Frame == 1 || _run.Frame == 3)) footstep = true;
        }
        else { _run.SetRange(0, 0); _run.Frame = 0; }

        // Ladders. Carrying the hammer blocks climbing - the cost of the power-up.
        if (!HasHammer)
        {
            if (up && OnLadder()) { EnterClimb(); return; }
            if (down && LadderBelow()) { EnterClimb(); Position.Y += 2f; return; }
        }

        // Added in a later chapter.
    }

    private void EnterClimb()
    {
        State = JackState.Climb;
        Velocity = Vector2.Zero;
        int col = (int)(Position.X / ClimberLevel.Tile8);
        Position.X = col * ClimberLevel.Tile8 + ClimberLevel.Tile8 / 2f;
        _climb.Reset();
    }

    private void UpdateClimb(float dt, bool up, bool down, bool left, bool right)
    {
        float move = 0f;
        if (up && !down) move = -1f;
        else if (down && !up) move = 1f;

        if (move != 0f)
        {
            float next = Position.Y + move * ClimbSpeed * dt;

            bool ladderHere = _level.IsLadderAtPixel(Position.X, next - 4);
            bool ladderBelow = _level.IsLadderAtPixel(Position.X, next + 4);

            if (!ladderHere && !ladderBelow)
            {
                State = JackState.Walk;
                return;
            }
            if (move > 0 && _level.TryGetGround(Position.X, next, out _, out float s) && !ladderBelow)
            {
                Position.Y = s;
                State = JackState.Walk;
                return;
            }
            Position.Y = next;
            _climb.Update(dt);
        }

        // Stepping sideways off a ladder is allowed wherever there is beam to
        // stand on; it removes the fiddly vertical correction a touch pad is bad at.
        if ((left || right) && OnGround())
        {
            FacingLeft = left;
            State = JackState.Walk;
        }
    }

    private void UpdateAirborne(float dt, ref bool landed)
    {
        float prevY = Position.Y;
        Velocity.Y += Gravity * dt;
        Position += Velocity * dt;

        if (Velocity.Y > 0f &&
            _level.TryGetBeamBelow(Position.X, prevY, out _, out float surface) &&
            Position.Y >= surface)
        {
            Position.Y = surface;
            float drop = Position.Y - _fellFrom;
            Velocity = Vector2.Zero;
            State = JackState.Walk;
            landed = true;
            if (drop > FatalFall) Kill();
        }
    }

    public void Draw(SpriteBatch batch)
    {
        var tint = Color.White;
        if (State == JackState.Dying) { _dead.Draw(batch, Position, FacingLeft, tint); return; }
        if (HasHammer)
        {
            // Flash the last two seconds so the loss can be planned.
            if (HammerTime < 2f && ((int)(HammerTime * 8) & 1) == 0) tint = Theme.Warn;
            _hammer.Draw(batch, Position, FacingLeft, tint);
            return;
        }
        if (State == JackState.Climb) { _climb.Draw(batch, Position, false, tint); return; }
        if (State == JackState.Jump)
        {
            _run.SetRange(4, 4); _run.Frame = 4;
            _run.Draw(batch, Position, FacingLeft, tint);
            return;
        }
        _run.Draw(batch, Position, FacingLeft, tint);
    }
}
