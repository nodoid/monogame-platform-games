using System;
using Retro.Engine;

namespace Retro.Hunch;

/// <summary>Run state for the runner: score, lives, which screen, and the clock.</summary>
public sealed class HunchSession
{
    public const int StartingLives = 3;
    public const int ExtraLifeAt = 15000;

    public int Score;
    public int Lives = StartingLives;
    public int ScreenIndex;
    public float TimeLeft;
    public int Loop;

    private int _nextExtraLife = ExtraLifeAt;

    public HighScoreTable Table { get; }
    public HighScoreStore Store { get; }

    public HunchSession(HighScoreTable table, HighScoreStore store)
    {
        Table = table;
        Store = store;
    }

    public int ScreenNumber => ScreenIndex + 1;

    public bool Award(int points)
    {
        Score += points;
        if (Score >= _nextExtraLife) { _nextExtraLife += ExtraLifeAt; Lives++; return true; }
        return false;
    }

    public void Reset()
    {
        Score = 0; Lives = StartingLives; ScreenIndex = 0; Loop = 0;
        _nextExtraLife = ExtraLifeAt;
    }

    /// <summary>
    /// The clock shortens on every loop. It is the only difficulty knob the game
    /// turns, which keeps the fifteen screens themselves honest: they are as hard
    /// on loop three as on loop one, you just have less time to be careful.
    /// </summary>
    public float TimeForScreen(int limit) => MathF.Max(18f, limit - Loop * 8f);
}
