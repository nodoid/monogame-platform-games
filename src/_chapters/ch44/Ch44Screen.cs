using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter44;

/// <summary>
/// Chapter 44 - Settings, Saves and Shared Score Data.
///
/// The settings screen both games share. Up and down pick a row, left and right
/// change it, fire activates it.
///
/// Two things here are worth more than they look. Volume is stored in the same
/// file as the scores, so there is one save to version and one save to write
/// atomically rather than two that can disagree. And the volume change is
/// applied to the live AudioManager as you drag it - a settings screen you
/// cannot hear is a settings screen nobody can set correctly.
/// </summary>
public sealed class Ch44Screen : IScreen
{
    private const int W = 224, H = 256;

    private enum Row { Music, Effects, ResetClimber, ResetRunner, Where }

    private readonly RetroGame _game;
    private readonly HighScoreTable _climber = new(3);
    private readonly HighScoreTable _hunch = new(8);
    private readonly HighScoreStore _climberStore = new("girderjack.scores");
    private readonly HighScoreStore _hunchStore = new("bellringer.scores");

    private Row _row = Row.Music;
    private string _message = "";
    private float _messageTime;
    private float _preview;

    public Ch44Screen(RetroGame game) => _game = game;

    public void Enter()
    {
        _climberStore.Load(_climber);
        _hunchStore.Load(_hunch);
        _game.Audio.MusicVolume = _climberStore.MusicVolume;
        _game.Audio.EffectVolume = _climberStore.EffectVolume;
        _game.Audio.PlayMusic("music_stage");
    }

    public void Leave()
    {
        _game.Audio.StopMusic();
        SaveBoth();
    }

    private void SaveBoth()
    {
        _climberStore.MusicVolume = _hunchStore.MusicVolume = _game.Audio.MusicVolume;
        _climberStore.EffectVolume = _hunchStore.EffectVolume = _game.Audio.EffectVolume;
        _climberStore.Save(_climber);
        _hunchStore.Save(_hunch);
    }

    private void Say(string message)
    {
        _message = message;
        _messageTime = 2.5f;
    }

    public void Update(float dt)
    {
        _messageTime = MathF.Max(0f, _messageTime - dt);
        _preview = MathF.Max(0f, _preview - dt);

        if (_game.Input.Pressed(Btn.Down)) _row = (Row)(((int)_row + 1) % 5);
        if (_game.Input.Pressed(Btn.Up)) _row = (Row)(((int)_row + 4) % 5);

        float delta = 0f;
        if (_game.Input.Down(Btn.Left)) delta = -dt * 0.8f;
        if (_game.Input.Down(Btn.Right)) delta = dt * 0.8f;

        switch (_row)
        {
            case Row.Music:
                if (delta != 0f)
                {
                    _game.Audio.MusicVolume = Math.Clamp(_game.Audio.MusicVolume + delta, 0f, 1f);
                    SaveBoth();
                }
                break;

            case Row.Effects:
                if (delta != 0f)
                {
                    _game.Audio.EffectVolume = Math.Clamp(_game.Audio.EffectVolume + delta, 0f, 1f);
                    SaveBoth();
                    // Play something as it moves, throttled, so the level is audible.
                    if (_preview <= 0f) { _preview = 0.18f; _game.Audio.Play("blip", 1f, priority: 5); }
                }
                break;

            case Row.ResetClimber:
                if (_game.Input.Pressed(Btn.Jump))
                {
                    _climber.SeedDefaults(new[] { "JMP", "APE", "ACE", "BAR", "TOP", "RUN", "BOB", "AAA" },
                                       12000, 1200);
                    SaveBoth();
                    Say("CLIMBER SCORES RESET");
                    _game.Audio.Play("select", 0.9f, priority: 6);
                }
                break;

            case Row.ResetRunner:
                if (_game.Input.Pressed(Btn.Jump))
                {
                    _hunch.SeedDefaults(new[] { "QUASIMODO", "ESMERALDA", "CLOPIN", "GRINGOIRE",
                                                "PHOEBUS", "FROLLO", "DJALI", "PIERRE" }, 18000, 1800);
                    SaveBoth();
                    Say("RUNNER SCORES RESET");
                    _game.Audio.Play("select", 0.9f, priority: 6);
                }
                break;

            case Row.Where:
                if (_game.Input.Pressed(Btn.Jump)) Say(Directory.Exists(
                    Path.GetDirectoryName(_climberStore.Path_)) ? "FOLDER EXISTS" : "FOLDER MISSING");
                break;
        }
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        f.DrawCentred(batch, "SETTINGS", W / 2, 8, Theme.Accent);

        int y = 34;
        Slider(batch, px, f, "MUSIC", _game.Audio.MusicVolume, y, _row == Row.Music); y += 26;
        Slider(batch, px, f, "EFFECTS", _game.Audio.EffectVolume, y, _row == Row.Effects); y += 32;

        Option(batch, px, f, "RESET CLIMBER SCORES", y, _row == Row.ResetClimber); y += 16;
        Option(batch, px, f, "RESET RUNNER SCORES", y, _row == Row.ResetRunner); y += 16;
        Option(batch, px, f, "WHERE IS THE SAVE?", y, _row == Row.Where); y += 24;

        f.Draw(batch, "CLIMBER BEST " + _climber.Best.ToString("D6"), new Vector2(12, y), Theme.Ink);
        y += 12;
        f.Draw(batch, "RUNNER  BEST " + _hunch.Best.ToString("D6"), new Vector2(12, y), Theme.Ink);
        y += 18;

        // The path, wrapped - it is long and different on every platform.
        string path = _climberStore.Path_;
        f.Draw(batch, "SAVE PATH", new Vector2(12, y), Theme.InkDim); y += 11;
        for (int i = 0; i < path.Length && y < H - 30; i += 34)
        {
            f.Draw(batch, path.Substring(i, Math.Min(34, path.Length - i)), new Vector2(12, y),
                   Theme.Info);
            y += 10;
        }

        if (_messageTime > 0f)
            f.DrawCentred(batch, _message, W / 2, H - 18, Theme.Good);
        else
            f.DrawCentred(batch, "U D PICK   L R CHANGE   FIRE GO", W / 2, H - 18, Theme.InkDim);
    }

    private static void Slider(SpriteBatch b, Texture2D px, BitmapFont f, string label,
                               float value, int y, bool selected)
    {
        f.Draw(b, label, new Vector2(12, y), selected ? Theme.Accent : Theme.Ink);
        b.Draw(px, new Rectangle(12, y + 12, 200, 6), new Color(206, 208, 204));
        b.Draw(px, new Rectangle(12, y + 12, (int)(200 * value), 6),
               selected ? Theme.Accent : Theme.Info);
        f.DrawRight(b, ((int)(value * 100)).ToString("D3"), 212, y, Theme.InkDim);
    }

    private static void Option(SpriteBatch b, Texture2D px, BitmapFont f, string label,
                               int y, bool selected)
    {
        if (selected) b.Draw(px, new Rectangle(8, y - 2, W - 16, 12), Theme.Highlight);
        f.Draw(b, (selected ? "> " : "  ") + label, new Vector2(12, y),
               selected ? Theme.Accent : Color.White);
    }
}
