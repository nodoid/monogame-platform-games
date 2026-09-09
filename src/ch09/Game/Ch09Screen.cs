using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter09;

/// <summary>
/// Chapter 9 - Building the Audio Engine.
///
/// A sound test bench. Up and down pick an effect, fire plays it, left and right
/// change the pan, and the pause button starts and stops the music.
///
/// The two rows at the bottom are the reason this screen exists. VOICES shows
/// how many SoundEffectInstances are alive; hold fire on the barrel roll and
/// watch the throttle refuse to open a new voice every frame. DUCK shows the
/// music gain being pulled down while a jingle plays, and eased back afterwards
/// rather than stepped - a hard step is audible as a click.
/// </summary>
public sealed class Ch09Screen : IScreen
{
    private const int W = 224, H = 256;

    private static readonly (string Name, int Priority, float Gap)[] Bank =
    {
        ("jump", 2, 0.03f), ("land", 1, 0.03f), ("step", 0, 0.10f),
        ("point", 2, 0.03f), ("hammer_get", 5, 0.05f), ("hammer_hit", 4, 0.03f),
        ("extra_life", 6, 0.10f), ("timer_warn", 3, 0.10f), ("death", 10, 0.20f),
        ("stage_clear", 10, 0.20f), ("blip", 2, 0.02f), ("select", 6, 0.05f),
    };

    private readonly RetroGame _game;
    private int _index;
    private float _pan;
    private bool _music;
    private float _repeat;

    public Ch09Screen(RetroGame game) => _game = game;

    public void Enter() { }
    public void Leave() => _game.Audio.StopAllLoops();

    public void Update(float dt)
    {
        _repeat -= dt;
        if (_game.Input.Pressed(Btn.Down)) _index = (_index + 1) % Bank.Length;
        if (_game.Input.Pressed(Btn.Up)) _index = (_index + Bank.Length - 1) % Bank.Length;
        if (_game.Input.Down(Btn.Left)) _pan = Math.Max(-1f, _pan - dt * 1.5f);
        if (_game.Input.Down(Btn.Right)) _pan = Math.Min(1f, _pan + dt * 1.5f);

        var (name, priority, gap) = Bank[_index];
        if (_game.Input.Down(Btn.Jump) && _repeat <= 0f)
        {
            _repeat = 0.05f;                 // deliberately faster than most gaps
            _game.Audio.Play(name, 0.9f, pan: _pan, priority: priority, minGap: gap);
            if (priority >= 6) _game.Audio.Duck(0.2f);
        }
        if (!_game.Input.Down(Btn.Jump)) _game.Audio.Unduck();

        if (_game.Input.Pressed(Btn.Pause))
        {
            _music = !_music;
            if (_music) _game.Audio.PlayMusic("music_stage");
            else _game.Audio.StopMusic();
        }
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        f.DrawCentred(batch, "SOUND TEST", W / 2, 6, Theme.Accent);

        int y = 24;
        for (int i = 0; i < Bank.Length; i++)
        {
            bool sel = i == _index;
            if (sel) batch.Draw(px, new Rectangle(6, y - 1, W - 12, 10), Theme.Highlight);
            f.Draw(batch, Bank[i].Name.Replace('_', ' '), new Vector2(12, y),
                   sel ? Theme.Accent : Color.White);
            f.DrawRight(batch, "P" + Bank[i].Priority, W - 40, y, Theme.InkDim);
            f.DrawRight(batch, (Bank[i].Gap * 1000).ToString("000") + "MS", W - 12, y, Theme.InkDim);
            y += 10;
        }

        y += 8;
        f.Draw(batch, "PAN", new Vector2(12, y), Theme.InkDim);
        batch.Draw(px, new Rectangle(48, y + 2, 140, 4), new Color(206, 208, 204));
        batch.Draw(px, new Rectangle(48 + (int)((_pan + 1f) * 0.5f * 136), y, 4, 8), Theme.Info);

        y += 16;
        f.Draw(batch, "MUSIC " + (_music ? "ON" : "OFF"), new Vector2(12, y),
               _music ? Theme.Good : Theme.InkDim);

        y += 14;
        f.Draw(batch, "FIRE  PLAY   PAUSE  MUSIC", new Vector2(12, y), Theme.InkDim);
        f.Draw(batch, "HOLD FIRE TO TEST THROTTLE", new Vector2(12, y + 10), Theme.InkDim);
    }
}
