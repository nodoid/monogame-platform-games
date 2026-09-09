using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;

namespace Retro.Engine;

/// <summary>
/// The on-screen controls. Deliberately laid out in *device* pixels rather than
/// virtual ones: a thumb is roughly 9mm wide whatever resolution the panel is,
/// so control size must follow the physical screen, not the game's 224x256
/// pretend screen. Everything else in the game works the opposite way round.
///
/// Two more decisions worth stating plainly:
///  - Hit areas are larger than the drawn art. Players aim at what they see and
///    miss low; a generous pad forgives that and nobody ever notices.
///  - The d-pad reports Left/Right independently of Up/Down, so a diagonal
///    press on a ladder does not silently cancel the climb.
/// </summary>
public sealed class VirtualPad
{
    public bool Enabled { get; set; } = true;
    public bool ShowDpadVertical { get; set; } = true;

    public bool Left { get; private set; }
    public bool Right { get; private set; }
    public bool Up { get; private set; }
    public bool Down { get; private set; }
    public bool Jump { get; private set; }
    public bool Pause { get; private set; }
    public bool AnyLooseTouch { get; private set; }

    private Rectangle _dpad, _jump, _pause;
    private Matrix _toPad = Matrix.Identity;
    private Matrix _toDevice = Matrix.Identity;
    private Rectangle _hitLeft, _hitRight, _hitUp, _hitDown, _hitJump;
    private int _screenW, _screenH;

    /// <summary>Height in device pixels the pad wants reserved at the bottom.</summary>
    public int ReservedHeight { get; private set; }

    /// <summary>
    /// Lay the pad out. When the picture is turned a quarter turn the pad turns
    /// with it, because the player has turned the device - so the layout is
    /// computed in the turned space and mapped back when it is drawn.
    /// </summary>
    public void Layout(int screenWidth, int screenHeight, bool landscape, bool rotated = false)
    {
        if (rotated)
        {
            // Turned space: width and height swap, and the transform puts it
            // back on the physical surface.
            _toDevice = Matrix.CreateRotationZ(MathHelper.PiOver2) *
                        Matrix.CreateTranslation(screenWidth, 0f, 0f);
            _toPad = Matrix.Invert(_toDevice);
            (screenWidth, screenHeight) = (screenHeight, screenWidth);
            landscape = screenWidth >= screenHeight;
        }
        else
        {
            _toDevice = Matrix.Identity;
            _toPad = Matrix.Identity;
        }

        _screenW = screenWidth;
        _screenH = screenHeight;

        // Size the controls from the shorter screen edge so they stay thumb-sized
        // in both orientations. 26% of the short edge lands close to a 12mm pad
        // on everything from a small phone to a tablet.
        int unit = (int)(Math.Min(screenWidth, screenHeight) * 0.26f);
        unit = Math.Clamp(unit, 90, 320);

        int margin = unit / 6;
        int padY = screenHeight - unit - margin;

        _dpad = new Rectangle(margin, padY, unit, unit);
        _jump = new Rectangle(screenWidth - unit - margin, padY, unit, unit);
        _pause = new Rectangle(screenWidth - unit / 3 - margin, margin, unit / 3, unit / 3);

        // Hit areas: the d-pad quadrants are widened by a third and the jump
        // button by a quarter, all clipped to sensible bounds.
        int t = unit / 3;
        int grow = t / 3;
        _hitLeft  = Inflate(new Rectangle(_dpad.X,           _dpad.Y + t,     t, t), grow);
        _hitRight = Inflate(new Rectangle(_dpad.X + 2 * t,   _dpad.Y + t,     t, t), grow);
        _hitUp    = Inflate(new Rectangle(_dpad.X + t,       _dpad.Y,         t, t), grow);
        _hitDown  = Inflate(new Rectangle(_dpad.X + t,       _dpad.Y + 2 * t, t, t), grow);
        _hitJump  = Inflate(_jump, unit / 8);

        ReservedHeight = landscape ? 0 : unit + margin * 2;
    }

    private static Rectangle Inflate(Rectangle r, int by)
        => new(r.X - by, r.Y - by, r.Width + by * 2, r.Height + by * 2);

    public void Update(TouchCollection touches)
    {
        Left = Right = Up = Down = Jump = Pause = AnyLooseTouch = false;
        if (!Enabled) return;

        foreach (var t in touches)
        {
            if (t.State == TouchLocationState.Released || t.State == TouchLocationState.Invalid)
                continue;
            var local = Vector2.Transform(t.Position, _toPad);
            var p = new Point((int)local.X, (int)local.Y);

            bool used = false;
            if (_hitLeft.Contains(p))  { Left = true;  used = true; }
            if (_hitRight.Contains(p)) { Right = true; used = true; }
            if (ShowDpadVertical && _hitUp.Contains(p))   { Up = true;   used = true; }
            if (ShowDpadVertical && _hitDown.Contains(p)) { Down = true; used = true; }
            if (_hitJump.Contains(p))  { Jump = true;  used = true; }
            if (_pause.Contains(p))    { Pause = true; used = true; }
            if (!used) AnyLooseTouch = true;
        }

        // A simultaneous left+right is physically possible with two thumbs and
        // makes the player stand still, which reads as a bug. Latest wins.
        if (Left && Right) { Left = Right = false; }
    }

    /// <summary>The transform that maps pad space on to the physical surface.</summary>
    public Matrix DeviceTransform => _toDevice;

    public void Draw(SpriteBatch batch, Texture2D pixel, float alpha = Theme.PadAlpha)
    {
        if (!Enabled) return;
        // Drawn in the theme's ink rather than white, so the pad is visible on a
        // pale ground as well as a dark one.
        var dim = Theme.PadInk * alpha;
        var lit = Theme.PadInk * MathF.Min(1f, alpha * 2.4f);

        int t = _dpad.Width / 3;
        Box(batch, pixel, new Rectangle(_dpad.X, _dpad.Y + t, t, t), Left ? lit : dim);
        Box(batch, pixel, new Rectangle(_dpad.X + 2 * t, _dpad.Y + t, t, t), Right ? lit : dim);
        if (ShowDpadVertical)
        {
            Box(batch, pixel, new Rectangle(_dpad.X + t, _dpad.Y, t, t), Up ? lit : dim);
            Box(batch, pixel, new Rectangle(_dpad.X + t, _dpad.Y + 2 * t, t, t), Down ? lit : dim);
        }
        Box(batch, pixel, new Rectangle(_dpad.X + t, _dpad.Y + t, t, t), dim);

        Box(batch, pixel, _jump, Jump ? lit : dim);
        Box(batch, pixel, _pause, dim);
    }

    private static void Box(SpriteBatch b, Texture2D px, Rectangle r, Color c)
    {
        int e = Math.Max(2, r.Width / 16);
        b.Draw(px, new Rectangle(r.X, r.Y, r.Width, e), c);
        b.Draw(px, new Rectangle(r.X, r.Bottom - e, r.Width, e), c);
        b.Draw(px, new Rectangle(r.X, r.Y, e, r.Height), c);
        b.Draw(px, new Rectangle(r.Right - e, r.Y, e, r.Height), c);
        b.Draw(px, new Rectangle(r.X + e, r.Y + e, r.Width - e * 2, r.Height - e * 2),
               c * 0.35f);
    }
}
