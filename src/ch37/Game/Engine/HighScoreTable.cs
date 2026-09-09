using System;
using System.Collections.Generic;

namespace Retro.Engine;

public readonly struct ScoreEntry
{
    public readonly string Name;
    public readonly int Score;
    public readonly int Stage;

    public ScoreEntry(string name, int score, int stage)
    {
        Name = name ?? "";
        Score = score;
        Stage = stage;
    }
}

/// <summary>
/// A ranked, capped, always-sorted table of scores.
///
/// The rule that makes an arcade table feel right is the tie-break: a new score
/// equal to an existing one is inserted *below* it. The player who got there
/// first keeps the higher rank, which is both fairer and what every cabinet did.
/// </summary>
public sealed class HighScoreTable
{
    public const int Capacity = 8;

    private readonly List<ScoreEntry> _entries = new();

    /// <summary>How many characters an entry may have. Three for the arcade
    /// climber, eight for the home-computer runner.</summary>
    public int MaxNameLength { get; }

    public HighScoreTable(int maxNameLength = 3)
    {
        MaxNameLength = Math.Clamp(maxNameLength, 1, 12);
    }

    public IReadOnlyList<ScoreEntry> Entries => _entries;
    public int Best => _entries.Count > 0 ? _entries[0].Score : 0;
    public int Lowest => _entries.Count < Capacity ? 0 : _entries[^1].Score;

    /// <summary>Would this score get on to the board at all?</summary>
    public bool Qualifies(int score) => score > 0 && (_entries.Count < Capacity || score > Lowest);

    /// <summary>Inserts and returns the rank (0-based), or -1 if it did not qualify.</summary>
    public int Insert(string name, int score, int stage)
    {
        if (!Qualifies(score)) return -1;
        name = Sanitise(name);

        int rank = _entries.Count;
        for (int i = 0; i < _entries.Count; i++)
        {
            if (score > _entries[i].Score) { rank = i; break; }
        }
        _entries.Insert(rank, new ScoreEntry(name, score, stage));
        while (_entries.Count > Capacity) _entries.RemoveAt(_entries.Count - 1);
        return rank;
    }

    public string Sanitise(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return new string('A', 1);
        Span<char> buf = stackalloc char[MaxNameLength];
        int n = 0;
        foreach (char c in name.ToUpperInvariant())
        {
            if (n >= MaxNameLength) break;
            // The font only has ASCII 32..95, so anything else would draw as a gap.
            if (c >= ' ' && c <= '_') buf[n++] = c;
        }
        return n == 0 ? "A" : new string(buf[..n]);
    }

    public void Clear() => _entries.Clear();

    public void Load(IEnumerable<ScoreEntry> entries)
    {
        _entries.Clear();
        foreach (var e in entries)
        {
            _entries.Add(new ScoreEntry(Sanitise(e.Name), e.Score, e.Stage));
            if (_entries.Count >= Capacity) break;
        }
        _entries.Sort((a, b) => b.Score.CompareTo(a.Score));
    }

    /// <summary>Seed a fresh table so a new install never shows an empty board.</summary>
    public void SeedDefaults(string[] names, int top, int step)
    {
        _entries.Clear();
        for (int i = 0; i < Math.Min(Capacity, names.Length); i++)
            _entries.Add(new ScoreEntry(Sanitise(names[i]), Math.Max(0, top - i * step), 1));
    }
}
