namespace Retro.Engine;

/// <summary>
/// Logical buttons. Nothing in the games ever asks "is the K key down" - they
/// ask "is Jump down". That indirection is what lets one build serve a phone
/// with a touch pad, a tablet, and a desktop keyboard during development.
/// </summary>
public enum Btn
{
    Left, Right, Up, Down, Jump, Action, Pause, Confirm, Back
}
