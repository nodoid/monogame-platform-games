using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter11;

/// <summary>
/// Chapter 11 - Scoring and the High Score Table.
///
/// A table you can poke at. Fire inserts a random score, up and down change the
/// size of that score, and the pause button inserts a score that exactly equals
/// one already on the board.
///
/// That last button is the point of the screen. An equal score is inserted
/// *below* the one that was already there, so whoever got it first keeps the
/// higher rank. Every arcade cabinet worked this way, it is the fairer rule, and
/// it is the one line of the insert routine people get wrong.
/// </summary>
public sealed class Ch11Screen : IScreen
{
    private const int W = 224, H = 256;
    private static readonly string[] Names =
        { "JMP", "APE", "ACE", "BAR", "TOP", "RUN", "BOB", "ZAP", "MAX", "PIP" };

    private readonly RetroGame _game;
    private readonly HighScoreTable _table = new(maxNameLength: 3);
    private readonly Random _rng = new();

    private int _magnitude = 3;
    private int _lastRank = -1;
    private string _lastAction = "";
    private float _flash;

    public Ch11Screen(RetroGame game) => _game = game;

    public void Enter()
    {
        _table.SeedDefaults(new[] { "JMP", "APE", "ACE", "BAR", "TOP", "RUN", "BOB", "AAA" },
                            12000, 1200);
    }

    public void Leave() { }

    public void Update(float dt)
    {
        _flash = MathF.Max(0f, _flash - dt);

        if (_game.Input.Pressed(Btn.Up)) _magnitude = Math.Min(5, _magnitude + 1);
        if (_game.Input.Pressed(Btn.Down)) _magnitude = Math.Max(1, _magnitude - 1);

        if (_game.Input.Pressed(Btn.Jump))
        {
            int score = _rng.Next(1, 10) * (int)Math.Pow(10, _magnitude);
            Insert(Names[_rng.Next(Names.Length)], score, "RANDOM");
        }

        if (_game.Input.Pressed(Btn.Pause) && _table.Entries.Count > 0)
        {
            var target = _table.Entries[_rng.Next(_table.Entries.Count)];
            Insert("TIE", target.Score, "TIE WITH " + target.Name);
        }

        if (_game.Input.Pressed(Btn.Left)) { _table.Clear(); _lastAction = "CLEARED"; _lastRank = -1; }
        if (_game.Input.Pressed(Btn.Right))
        {
            _table.SeedDefaults(new[] { "JMP", "APE", "ACE", "BAR", "TOP", "RUN", "BOB", "AAA" },
                                12000, 1200);
            _lastAction = "RESEEDED";
            _lastRank = -1;
        }
    }

    private void Insert(string name, int score, string what)
    {
        bool qualified = _table.Qualifies(score);
        _lastRank = _table.Insert(name, score, 1);
        _lastAction = qualified ? $"{what} {score}" : $"REJECTED {score}";
        _flash = 1.2f;
        _game.Audio.Play(qualified ? "point" : "blip", 0.7f, priority: 3);
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;

        f.DrawCentred(batch, "HIGH SCORE TABLE", W / 2, 6, Theme.Accent);

        int y = 26;
        for (int i = 0; i < HighScoreTable.Capacity; i++)
        {
            bool present = i < _table.Entries.Count;
            bool lit = i == _lastRank && _flash > 0f && ((int)(_flash * 8) & 1) == 0;
            if (lit) batch.Draw(px, new Rectangle(8, y - 1, W - 16, 10), Theme.Highlight);

            f.Draw(batch, (i + 1).ToString("D2"), new Vector2(14, y), Theme.InkDim);
            if (present)
            {
                var e = _table.Entries[i];
                f.Draw(batch, e.Name, new Vector2(44, y),
                       i == _lastRank ? Theme.Accent : Color.White);
                f.DrawRight(batch, e.Score.ToString("D6"), W - 14, y,
                            i == _lastRank ? Theme.Accent : Color.White);
            }
            else
            {
                f.Draw(batch, "---", new Vector2(44, y), Theme.InkFaint);
                f.DrawRight(batch, "000000", W - 14, y, Theme.InkFaint);
            }
            y += 12;
        }

        y += 10;
        f.Draw(batch, "ENTRIES  " + _table.Entries.Count + " / " + HighScoreTable.Capacity,
               new Vector2(12, y), Color.White); y += 11;
        f.Draw(batch, "BEST     " + _table.Best.ToString("D6"), new Vector2(12, y), Theme.Ink); y += 11;
        f.Draw(batch, "CUT-OFF  " + _table.Lowest.ToString("D6"), new Vector2(12, y), Theme.Ink); y += 11;
        f.Draw(batch, "NEXT     " + ((int)Math.Pow(10, _magnitude)).ToString("D6"),
               new Vector2(12, y), Theme.Info); y += 14;

        if (_lastAction.Length > 0)
            f.Draw(batch, _lastAction, new Vector2(12, y),
                   _lastAction.StartsWith("REJECTED") ? Theme.Warn : Theme.Good);

        f.Draw(batch, "FIRE INSERT   PAUSE TIE", new Vector2(12, H - 32), Theme.InkDim);
        f.Draw(batch, "U D SIZE  L CLEAR R SEED", new Vector2(12, H - 20), Theme.InkDim);
    }
}
