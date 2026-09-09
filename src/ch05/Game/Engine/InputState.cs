using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Retro.Engine;

/// <summary>
/// Collapses keyboard, gamepad and the on-screen pad into one set of logical
/// buttons, and remembers last frame's state so we can ask for edges.
///
/// Edge detection matters more than it looks. "Jump if the button is down"
/// makes a held button re-trigger every frame; "jump if the button went down
/// this frame" is what a player actually means. And because a touch can be
/// missed on the exact frame a player lands, we also keep a short buffer -
/// see <see cref="PressedRecently"/>.
/// </summary>
public sealed class InputState
{
    private const int Count = 9;
    private readonly bool[] _now = new bool[Count];
    private readonly bool[] _was = new bool[Count];
    private readonly float[] _pressedAt = new float[Count];

    private float _time;

    public VirtualPad Pad { get; }
    public TouchCollection Touches { get; private set; }

    public InputState(VirtualPad pad)
    {
        Pad = pad;
        for (int i = 0; i < Count; i++) _pressedAt[i] = float.NegativeInfinity;
    }

    public void Update(float dt)
    {
        _time += dt;
        Array.Copy(_now, _was, Count);

        Touches = TouchPanel.GetState();
        Pad.Update(Touches);

        var k = Keyboard.GetState();
        var g = GamePad.GetState(PlayerIndex.One);

        Set(Btn.Left,    Pad.Left    || k.IsKeyDown(Keys.Left)  || k.IsKeyDown(Keys.A) || g.DPad.Left  == ButtonState.Pressed || g.ThumbSticks.Left.X < -0.4f);
        Set(Btn.Right,   Pad.Right   || k.IsKeyDown(Keys.Right) || k.IsKeyDown(Keys.D) || g.DPad.Right == ButtonState.Pressed || g.ThumbSticks.Left.X >  0.4f);
        Set(Btn.Up,      Pad.Up      || k.IsKeyDown(Keys.Up)    || k.IsKeyDown(Keys.W) || g.DPad.Up    == ButtonState.Pressed || g.ThumbSticks.Left.Y >  0.4f);
        Set(Btn.Down,    Pad.Down    || k.IsKeyDown(Keys.Down)  || k.IsKeyDown(Keys.S) || g.DPad.Down  == ButtonState.Pressed || g.ThumbSticks.Left.Y < -0.4f);
        Set(Btn.Jump,    Pad.Jump    || k.IsKeyDown(Keys.Space) || k.IsKeyDown(Keys.Z) || g.Buttons.A == ButtonState.Pressed);
        Set(Btn.Action,  Pad.Jump    || k.IsKeyDown(Keys.X)     || g.Buttons.X == ButtonState.Pressed);
        Set(Btn.Pause,   Pad.Pause   || k.IsKeyDown(Keys.P)     || g.Buttons.Start == ButtonState.Pressed);
        Set(Btn.Confirm, Pad.Jump    || k.IsKeyDown(Keys.Enter) || k.IsKeyDown(Keys.Space) || g.Buttons.A == ButtonState.Pressed);
        Set(Btn.Back,    k.IsKeyDown(Keys.Escape) || g.Buttons.Back == ButtonState.Pressed);

        // Any screen touch outside the pad also counts as Confirm, so a title
        // screen does not force the player to find a specific button.
        if (Pad.AnyLooseTouch) Set(Btn.Confirm, true);
    }

    private void Set(Btn b, bool down)
    {
        int i = (int)b;
        if (down && !_now[i]) _pressedAt[i] = _time;
        _now[i] = down;
    }

    public bool Down(Btn b) => _now[(int)b];
    public bool Pressed(Btn b) => _now[(int)b] && !_was[(int)b];
    public bool Released(Btn b) => !_now[(int)b] && _was[(int)b];

    /// <summary>
    /// True if the button went down within the last <paramref name="window"/>
    /// seconds. This is jump buffering: a player who taps a few frames before
    /// touching the ground still gets the jump, which removes almost all of the
    /// "the game ignored me" complaints from touch controls.
    /// </summary>
    public bool PressedRecently(Btn b, float window = 0.12f)
        => _time - _pressedAt[(int)b] <= window;

    /// <summary>Consume a buffered press so it cannot fire twice.</summary>
    public void ConsumeBuffer(Btn b) => _pressedAt[(int)b] = float.NegativeInfinity;

    public void Clear()
    {
        Array.Clear(_now); Array.Clear(_was);
        for (int i = 0; i < Count; i++) _pressedAt[i] = float.NegativeInfinity;
    }
}
