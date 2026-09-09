using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Retro.Engine;

/// <summary>Drawn over the frozen game beneath it.</summary>
public sealed class PauseScreen : IScreen, IOverlayScreen
{
    private readonly RetroGame _game;
    private readonly Action _quit;
    private float _t;

    public PauseScreen(RetroGame game, Action quitToTitle)
    {
        _game = game;
        _quit = quitToTitle;
    }

    public void Enter() { _game.Audio.Duck(0.15f); _t = 0f; }
    public void Leave() { _game.Audio.Unduck(); }

    public void Update(float dt)
    {
        _t += dt;
        if (_t < 0.25f) return;   // swallow the touch that opened the menu
        if (_game.Input.Pressed(Btn.Pause) || _game.Input.Pressed(Btn.Confirm)) _game.Screens.Pop();
        else if (_game.Input.Pressed(Btn.Back)) _quit?.Invoke();
    }

    public void Draw(SpriteBatch batch)
    {
        int w = _game.VirtualWidth, h = _game.VirtualHeight;
        batch.Draw(_game.Assets.Pixel, new Rectangle(0, 0, w, h), Theme.Overlay * Theme.OverlayAlpha);
        _game.Font.DrawCentred(batch, "PAUSED", w / 2, h / 2 - 16, Theme.Ink);
        _game.Font.DrawCentred(batch, "TAP TO RESUME", w / 2, h / 2 + 4, Theme.Accent);
    }
}

/// <summary>
/// The ranked table. Used both as an attract-mode page and as the screen shown
/// after a run ends.
/// </summary>
public sealed class HighScoreScreen : IScreen
{
    private readonly RetroGame _game;
    private readonly HighScoreTable _table;
    private readonly Action _onDone;
    private readonly string _title;
    private readonly int _highlight;
    private readonly float _autoAdvance;
    private float _t;

    public HighScoreScreen(RetroGame game, HighScoreTable table, string title,
                           Action onDone, int highlightRank = -1, float autoAdvance = 0f)
    {
        _game = game;
        _table = table;
        _title = title;
        _onDone = onDone;
        _highlight = highlightRank;
        _autoAdvance = autoAdvance;
    }

    public void Enter() { _t = 0f; }
    public void Leave() { }

    public void Update(float dt)
    {
        _t += dt;
        if (_t > 0.4f && _game.Input.Pressed(Btn.Confirm)) _onDone?.Invoke();
        else if (_autoAdvance > 0f && _t > _autoAdvance) _onDone?.Invoke();
    }

    public void Draw(SpriteBatch batch)
    {
        int w = _game.VirtualWidth;
        var f = _game.Font;
        f.DrawCentred(batch, _title, w / 2, 18, Theme.Accent);

        int y = 44;
        for (int i = 0; i < _table.Entries.Count; i++)
        {
            var e = _table.Entries[i];
            // Blink the entry the player just earned so they can find it.
            bool lit = i == _highlight && ((int)(_t * 6) & 1) == 0;
            var col = i == _highlight ? (lit ? Theme.Ink : Theme.Warn)
                                      : (i == 0 ? Theme.Accent : Theme.Ink);
            f.Draw(batch, (i + 1).ToString("D2"), new Vector2(24, y), Theme.InkDim);
            f.Draw(batch, e.Name, new Vector2(48, y), col);
            f.DrawRight(batch, e.Score.ToString("D6"), w - 40, y, col);
            y += 14;
        }
        if (_table.Entries.Count == 0)
            f.DrawCentred(batch, "NO SCORES YET", w / 2, 70, Theme.InkDim);

        if (((int)(_t * 2) & 1) == 0)
            f.DrawCentred(batch, "TAP TO CONTINUE", w / 2, _game.VirtualHeight - 28, Theme.Ink);
    }
}

/// <summary>
/// Arcade initial entry: up and down change the letter under the cursor, left
/// and right move it, the action button confirms.
///
/// The timeout is not a nicety. A cabinet could not rely on the player still
/// being there, and neither can a phone - a call arrives, the app is backgrounded
/// mid-entry, and without an auto-confirm the score would be lost.
/// </summary>
public sealed class NameEntryScreen : IScreen
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.- ";

    private readonly RetroGame _game;
    private readonly HighScoreTable _table;
    private readonly HighScoreStore _store;
    private readonly int _score, _stage;
    private readonly Action<int> _onDone;
    private readonly int _length;

    private readonly int[] _letters;
    private int _cursor;
    private float _blink, _repeat, _timeout;

    public NameEntryScreen(RetroGame game, HighScoreTable table, HighScoreStore store,
                           int score, int stage, Action<int> onDone)
    {
        _game = game;
        _table = table;
        _store = store;
        _score = score;
        _stage = stage;
        _onDone = onDone;
        _length = table.MaxNameLength;
        _letters = new int[_length];
    }

    public void Enter() { _timeout = 0f; _cursor = 0; }
    public void Leave() { }

    public void Update(float dt)
    {
        _blink += dt;
        _timeout += dt;
        _repeat -= dt;

        bool up = _game.Input.Pressed(Btn.Up), down = _game.Input.Pressed(Btn.Down);
        bool held = _game.Input.Down(Btn.Up) || _game.Input.Down(Btn.Down);
        if (held && _repeat <= 0f)
        {
            _repeat = 0.14f;
            up = _game.Input.Down(Btn.Up);
            down = _game.Input.Down(Btn.Down);
        }
        else if (!held) _repeat = 0.35f;

        if (up) Step(+1);
        if (down) Step(-1);
        if (_game.Input.Pressed(Btn.Right)) { _cursor = Math.Min(_length - 1, _cursor + 1); Blip(); }
        if (_game.Input.Pressed(Btn.Left)) { _cursor = Math.Max(0, _cursor - 1); Blip(); }

        if (_game.Input.Pressed(Btn.Jump) || _game.Input.Pressed(Btn.Confirm))
        {
            if (_cursor < _length - 1) { _cursor++; Blip(); }
            else Commit();
        }

        if (_timeout > 25f) Commit();
    }

    private void Step(int dir)
    {
        _letters[_cursor] = (_letters[_cursor] + dir + Alphabet.Length) % Alphabet.Length;
        _timeout = 0f;
        Blip();
    }

    private void Blip() { _game.Audio.Play("blip", 0.5f, priority: 2, minGap: 0.02f); _timeout = 0f; }

    private void Commit()
    {
        var name = new char[_length];
        for (int i = 0; i < _length; i++) name[i] = Alphabet[_letters[i]];
        int rank = _table.Insert(new string(name).Trim(), _score, _stage);
        _store.Save(_table);
        _game.Audio.Play("select", 0.8f, priority: 6);
        _onDone?.Invoke(rank);
    }

    public void Draw(SpriteBatch batch)
    {
        int w = _game.VirtualWidth;
        var f = _game.Font;
        f.DrawCentred(batch, "NEW HIGH SCORE", w / 2, 30, Theme.Accent);
        f.DrawCentred(batch, _score.ToString("D6"), w / 2, 48, Theme.Ink);
        f.DrawCentred(batch, "ENTER YOUR NAME", w / 2, 76, Theme.InkDim);

        int cellW = BitmapFont.Advance * 2;
        int x0 = w / 2 - (_length * cellW) / 2;
        for (int i = 0; i < _length; i++)
        {
            bool sel = i == _cursor;
            bool on = !sel || ((int)(_blink * 4) & 1) == 0;
            var col = sel ? Theme.Accent : Theme.Ink;
            if (on) f.Draw(batch, Alphabet[_letters[i]].ToString(),
                           new Vector2(x0 + i * cellW, 100), col, 2);
            if (sel)
                batch.Draw(_game.Assets.Pixel,
                           new Rectangle(x0 + i * cellW, 118, BitmapFont.Advance * 2 - 2, 2),
                           Theme.Accent);
        }

        f.DrawCentred(batch, "UP DOWN  PICK", w / 2, 146, Theme.InkDim);
        f.DrawCentred(batch, "FIRE     NEXT", w / 2, 158, Theme.InkDim);

        float left = MathF.Max(0f, 25f - _timeout);
        if (left < 10f)
            f.DrawCentred(batch, ((int)left).ToString(), w / 2, 178, Theme.Warn);
    }
}
