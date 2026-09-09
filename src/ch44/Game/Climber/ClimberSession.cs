using System;
using Retro.Engine;

namespace Retro.Climber;

/// <summary>
/// Everything that survives losing a life: score, lives, which stage we are on
/// and how many times we have been round the loop.
///
/// Keeping this out of the play screen is what makes a life loss cheap - the
/// screen is thrown away and rebuilt from the level data, and the session is
/// simply handed to the new one.
/// </summary>
public sealed class ClimberSession
{
    public const int StartingLives = 3;
    public const int ExtraLifeAt = 10000;

    public int Score;
    public int Lives = StartingLives;
    public int StageIndex;
    public int Loop;                 // how many times the four stages have cycled
    public int Bonus;

    private int _nextExtraLife = ExtraLifeAt;

    public HighScoreTable Table { get; }
    public HighScoreStore Store { get; }

    public ClimberSession(HighScoreTable table, HighScoreStore store)
    {
        Table = table;
        Store = store;
    }

    public int StageNumber => Loop * ClimberStages.Count + StageIndex + 1;

    /// <summary>Returns true if the award crossed an extra-life threshold.</summary>
    public bool Award(int points)
    {
        Score += points;
        if (Score >= _nextExtraLife)
        {
            _nextExtraLife += ExtraLifeAt;
            Lives++;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        Score = 0;
        Lives = StartingLives;
        StageIndex = 0;
        Loop = 0;
        _nextExtraLife = ExtraLifeAt;
    }

    public void AdvanceStage()
    {
        StageIndex++;
        if (StageIndex >= ClimberStages.Count) { StageIndex = 0; Loop++; }
    }

    /// <summary>
    /// Difficulty is a function of the loop count only, never of how well the
    /// player is doing. Hidden rubber-banding in an arcade game destroys the
    /// thing players come back for: a score you can compare with someone else's.
    /// </summary>
    public float BarrelInterval => MathF.Max(0.9f, 2.6f - Loop * 0.35f - StageIndex * 0.08f);
    public float LadderChance => MathF.Min(0.55f, 0.18f + Loop * 0.09f);
}
