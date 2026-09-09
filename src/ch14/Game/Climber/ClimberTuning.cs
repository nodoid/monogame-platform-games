namespace Retro.Climber;

/// <summary>
/// The climber's movement numbers, in one place.
///
/// They live here rather than on <see cref="ClimberPlayer"/> because chapter 13's
/// stage viewer needs the jump constants to draw its REACH overlay, and it does
/// not compile the player at all. Chapter 13 makes the general point: a debug
/// display computed from its own copy of a number will eventually disagree with
/// the game, and a debug display you cannot trust is worse than none.
/// </summary>
public static class ClimberTuning
{
    /// <summary>Pixels per second. Sixteen percent faster than a barrel on the flat.</summary>
    public const float WalkSpeed = 52f;

    /// <summary>Two thirds of the walking speed, which is what makes a ladder a commitment.</summary>
    public const float ClimbSpeed = 34f;

    public const float Gravity = 480f;

    /// <summary>Upward launch speed. Apex is v^2 / 2g = 25 pixels.</summary>
    public const float JumpSpeed = 155f;

    /// <summary>
    /// Levels are 38 pixels apart, so one is survivable and two are not. The
    /// value sits between them rather than on either, which is what makes the
    /// rule learnable in about thirty seconds without being told.
    /// </summary>
    public const float FatalFall = 50f;

    public const float HammerSeconds = 8f;

    /// <summary>The height of the arc, derived rather than typed.</summary>
    public const float JumpApex = JumpSpeed * JumpSpeed / (2f * Gravity);
}
