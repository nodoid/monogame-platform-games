using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Retro.Engine;

/// <summary>
/// A strip of equal-sized frames packed left to right in one texture, advanced
/// on a wall-clock timer rather than per update. Tying animation to seconds and
/// not to frames means the walk cycle looks the same whether the device is
/// managing 60fps or dropping to 40.
/// </summary>
public sealed class Animation
{
    private readonly Texture2D _sheet;
    private readonly int _frameWidth, _frameHeight, _count;
    private readonly float _frameTime;
    private readonly bool _loop;

    private float _elapsed;
    private int _frame;
    private int _first, _last;

    public Animation(Texture2D sheet, int frameWidth, int frameHeight, float fps = 10f, bool loop = true)
    {
        _sheet = sheet;
        _frameWidth = frameWidth;
        _frameHeight = frameHeight;
        _count = Math.Max(1, sheet.Width / frameWidth);
        _frameTime = 1f / MathF.Max(0.01f, fps);
        _loop = loop;
        _first = 0;
        _last = _count - 1;
    }

    /// <summary>
    /// Restrict playback to part of the strip. One sheet often holds several
    /// states - idle, three run frames, a jump pose - and cycling the lot would
    /// make the character flick into a jump every fourth step.
    /// </summary>
    public Animation SetRange(int first, int last)
    {
        _first = Math.Clamp(first, 0, _count - 1);
        _last = Math.Clamp(last, _first, _count - 1);
        if (_frame < _first || _frame > _last) { _frame = _first; _elapsed = 0f; }
        return this;
    }

    public int Frame
    {
        get => _frame;
        set { _frame = Math.Clamp(value, 0, _count - 1); _elapsed = 0f; }
    }

    public int Count => _count;
    public bool Finished => !_loop && _frame >= _last;
    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;

    public void Reset() { _frame = _first; _elapsed = 0f; }

    public void Update(float dt)
    {
        if (_last <= _first || Finished) return;
        _elapsed += dt;
        while (_elapsed >= _frameTime)
        {
            _elapsed -= _frameTime;
            _frame++;
            if (_frame > _last) _frame = _loop ? _first : _last;
        }
    }

    public Rectangle Source => new(_frame * _frameWidth, 0, _frameWidth, _frameHeight);

    /// <summary>Draws with <paramref name="origin"/> at the sprite's feet-centre,
    /// which is the anchor platform games nearly always want.</summary>
    public void Draw(SpriteBatch batch, Vector2 footCentre, bool flip, Color tint)
    {
        var dest = new Rectangle((int)(footCentre.X - _frameWidth / 2f),
                                 (int)(footCentre.Y - _frameHeight),
                                 _frameWidth, _frameHeight);
        batch.Draw(_sheet, dest, Source, tint, 0f, Vector2.Zero,
                   flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }

    public void DrawAt(SpriteBatch batch, Vector2 topLeft, bool flip, Color tint)
    {
        var dest = new Rectangle((int)topLeft.X, (int)topLeft.Y, _frameWidth, _frameHeight);
        batch.Draw(_sheet, dest, Source, tint, 0f, Vector2.Zero,
                   flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }
}
