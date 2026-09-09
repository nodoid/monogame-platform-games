using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Retro.Engine;

public interface IScreen
{
    void Enter();
    void Leave();
    void Update(float dt);
    void Draw(SpriteBatch batch);
}

/// <summary>
/// A stack of screens with a fade between them.
///
/// A stack rather than a single "current screen" because pause is genuinely a
/// screen on top of gameplay: the game underneath keeps its whole state and
/// simply stops updating. The fade is not decoration - it hides the frame where
/// a level is torn down and rebuilt, which would otherwise show as a flicker.
/// </summary>
public sealed class ScreenManager
{
    private readonly List<IScreen> _stack = new();
    private readonly Texture2D _pixel;
    private int _width, _height;

    private Action _pending;
    private float _fade;         // 0 = clear, 1 = black
    private int _fadeDir;        // -1 fading in, +1 fading out
    private float _fadeSpeed = 3.5f;

    public ScreenManager(Texture2D pixel, int virtualWidth, int virtualHeight)
    {
        _pixel = pixel;
        _width = virtualWidth;
        _height = virtualHeight;
    }

    /// <summary>Called when the game changes virtual resolution (an orientation flip).</summary>
    public void SetSize(int width, int height) { _width = width; _height = height; }

    public IScreen Current => _stack.Count > 0 ? _stack[^1] : null;
    public bool Busy => _fadeDir != 0;
    public int Depth => _stack.Count;

    public void Replace(IScreen screen, bool fade = true) => Transition(() =>
    {
        while (_stack.Count > 0) Pop(false);
        Push(screen, false);
    }, fade);

    public void Push(IScreen screen, bool fade = true)
    {
        if (!fade) { _stack.Add(screen); screen.Enter(); return; }
        Transition(() => { _stack.Add(screen); screen.Enter(); }, true);
    }

    public void Pop(bool fade = true)
    {
        if (!fade)
        {
            if (_stack.Count == 0) return;
            var top = _stack[^1];
            _stack.RemoveAt(_stack.Count - 1);
            top.Leave();
            return;
        }
        Transition(() => Pop(false), true);
    }

    private void Transition(Action action, bool fade)
    {
        if (!fade) { action(); return; }

        // A transition already under way owns the next screen. Screens keep
        // updating through the fade (so animations do not freeze), so a screen
        // that hands over on a timer would otherwise ask again on every frame
        // of that fade - and a "lose a life" hand-over would spend all three.
        if (_fadeDir != 0) return;

        _pending = action;
        _fadeDir = 1;
    }

    public void Update(float dt)
    {
        if (_fadeDir != 0)
        {
            _fade += _fadeDir * _fadeSpeed * dt;
            if (_fadeDir > 0 && _fade >= 1f)
            {
                _fade = 1f;
                _pending?.Invoke();
                _pending = null;
                _fadeDir = -1;
            }
            else if (_fadeDir < 0 && _fade <= 0f)
            {
                _fade = 0f;
                _fadeDir = 0;
            }
            // Screens still update through a fade so animations do not freeze.
        }
        Current?.Update(dt);
    }

    public void Draw(SpriteBatch batch)
    {
        // Only the top screen draws, except that a pause overlay wants the game
        // visible behind it - screens that need that push themselves as overlays.
        for (int i = 0; i < _stack.Count; i++)
        {
            if (i == _stack.Count - 1 || _stack[i + 1] is IOverlayScreen)
                _stack[i].Draw(batch);
        }
        if (_fade > 0f)
            batch.Draw(_pixel, new Rectangle(0, 0, _width, _height), Theme.Fade * _fade);
    }
}

/// <summary>Marker: this screen draws over the one beneath instead of replacing it.</summary>
public interface IOverlayScreen { }
