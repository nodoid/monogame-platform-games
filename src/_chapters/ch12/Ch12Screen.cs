using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;

namespace Chapter12;

/// <summary>
/// Chapter 12 - Persisting High Scores.
///
/// Write the table, read it back, and then deliberately break it.
///
/// The CORRUPT button rewrites one score in the file by hand, exactly as a
/// curious player with a file browser would. Reload afterwards and the checksum
/// rejects the whole file, the table falls back to its seeded defaults, and -
/// this is the part that matters - the game carries on. A save format that can
/// throw on load is a save format that can stop your game booting on a device
/// you will never see.
/// </summary>
public sealed class Ch12Screen : IScreen
{
    private const int W = 224, H = 256;

    private readonly RetroGame _game;
    private readonly HighScoreTable _table = new(maxNameLength: 3);
    private readonly HighScoreStore _store = new("chapter12.scores");
    private readonly Random _rng = new();

    private string _status = "READY";
    private Color _statusColour = Theme.Ink;
    private long _fileSize = -1;
    private string[] _preview = Array.Empty<string>();

    public Ch12Screen(RetroGame game) => _game = game;

    public void Enter()
    {
        if (!_store.Load(_table)) Seed("NO FILE - SEEDED", Theme.InkDim);
        Refresh();
    }

    public void Leave() { }

    private void Seed(string message, Color colour)
    {
        _table.SeedDefaults(new[] { "JMP", "APE", "ACE", "BAR", "TOP", "RUN", "BOB", "AAA" },
                            12000, 1200);
        _status = message;
        _statusColour = colour;
    }

    private void Refresh()
    {
        try
        {
            var info = new FileInfo(_store.Path_);
            _fileSize = info.Exists ? info.Length : -1;
            _preview = info.Exists ? File.ReadAllLines(_store.Path_) : Array.Empty<string>();
        }
        catch (Exception)
        {
            _fileSize = -1;
            _preview = Array.Empty<string>();
        }
    }

    public void Update(float dt)
    {
        if (_game.Input.Pressed(Btn.Jump))
        {
            _table.Insert("NEW", _rng.Next(1000, 40000), 1);
            _status = _store.Save(_table) ? "SAVED" : "SAVE FAILED";
            _statusColour = _status == "SAVED" ? Theme.Good : Theme.Warn;
            Refresh();
            _game.Audio.Play("point", 0.7f, priority: 4);
        }

        if (_game.Input.Pressed(Btn.Up))
        {
            _table.Clear();
            if (_store.Load(_table)) { _status = "LOADED OK"; _statusColour = Theme.Good; }
            else Seed("LOAD REJECTED - SEEDED", Theme.Warn);
            Refresh();
        }

        if (_game.Input.Pressed(Btn.Down))
        {
            // Tamper: bump the first score line by a million, leaving the
            // checksum line untouched.
            try
            {
                var lines = File.ReadAllLines(_store.Path_);
                for (int i = 0; i < lines.Length; i++)
                {
                    var parts = lines[i].Split('|');
                    if (parts.Length == 3 && int.TryParse(parts[1], out int v))
                    {
                        lines[i] = $"{parts[0]}|{v + 1000000}|{parts[2]}";
                        break;
                    }
                }
                File.WriteAllLines(_store.Path_, lines);
                _status = "FILE TAMPERED";
                _statusColour = Theme.Accent;
                Refresh();
            }
            catch (Exception ex)
            {
                _status = ex.GetType().Name.ToUpperInvariant();
                _statusColour = Theme.Warn;
            }
        }

        if (_game.Input.Pressed(Btn.Pause))
        {
            try { File.Delete(_store.Path_); _status = "FILE DELETED"; _statusColour = Theme.Accent; }
            catch (Exception) { _status = "DELETE FAILED"; _statusColour = Theme.Warn; }
            Refresh();
        }
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;

        f.DrawCentred(batch, "PERSISTENCE", W / 2, 6, Theme.Accent);
        f.DrawCentred(batch, _status, W / 2, 20, _statusColour);

        int y = 38;
        f.Draw(batch, "TABLE IN MEMORY", new Vector2(12, y), Theme.InkDim); y += 12;
        for (int i = 0; i < Math.Min(4, _table.Entries.Count); i++)
        {
            var e = _table.Entries[i];
            f.Draw(batch, (i + 1) + " " + e.Name, new Vector2(20, y), Theme.Ink);
            f.DrawRight(batch, e.Score.ToString("D6"), W - 14, y, Theme.Ink);
            y += 11;
        }

        y += 8;
        f.Draw(batch, "FILE " + (_fileSize < 0 ? "MISSING" : _fileSize + " BYTES"),
               new Vector2(12, y), _fileSize < 0 ? Theme.Warn : Theme.Good);
        y += 14;

        f.Draw(batch, "RAW CONTENTS", new Vector2(12, y), Theme.InkDim); y += 12;
        for (int i = 0; i < Math.Min(6, _preview.Length); i++)
        {
            var line = _preview[i];
            if (line.Length > 30) line = line[..30];
            f.Draw(batch, line, new Vector2(16, y), Theme.Info);
            y += 10;
        }

        f.Draw(batch, "FIRE ADD+SAVE   U RELOAD", new Vector2(12, H - 32), Theme.InkDim);
        f.Draw(batch, "D CORRUPT    PAUSE DELETE", new Vector2(12, H - 20), Theme.InkDim);
    }
}
