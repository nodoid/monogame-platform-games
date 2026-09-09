using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Retro.Engine;
using Retro.Climber;
using Retro.Hunch;

namespace Chapter46;

/// <summary>
/// Chapter 46 - Where to Take These Engines Next.
///
/// One app, both games' pieces, running side by side on one screen.
///
/// That is the closing argument of the book. A barrel from the climber and a
/// guard from the runner are updating in the same loop, drawn by the same
/// batch, animated by the same Animation class, over the same tile grid, and
/// neither knows the other exists. Nothing in <c>Retro.Engine</c> ever knew
/// which game it was serving. Whatever you build next - a scrolling adventure, a
/// multi-screen arcade cabinet, something that is neither - starts here rather
/// than from an empty folder.
///
/// Left and right move the cursor through the roadmap; fire toggles the demo.
/// </summary>
public sealed class Ch46Screen : IScreen
{
    private const int W = 224, H = 256;

    private static readonly (string Title, string Detail)[] Roadmap =
    {
        ("MORE STAGES", "THE CLIMBER CYCLES FOUR. THE STAGE FORMAT IS TEXT - ADD A FIFTH."),
        ("A LEVEL EDITOR", "THE ASCII MAPS ARE ALREADY AN EDITOR FORMAT. GIVE THEM A UI."),
        ("SCROLLING WORLD", "CHAPTER 28 BUILT THE CAMERA. POINT IT AT ONE WIDE TILE MAP."),
        ("ONLINE BOARDS", "HIGHSCORETABLE IS ALREADY A RANKED LIST. POST IT SOMEWHERE."),
        ("TWO PLAYERS", "INPUTSTATE IS ONE OBJECT. MAKE TWO AND PASS ONE TO EACH PLAYER."),
        ("REPLAYS", "FIXED TIMESTEP MEANS RECORDING INPUT IS ENOUGH TO REPLAY A RUN."),
    };

    private readonly RetroGame _game;

    private ClimberLevel _climberLevel;
    private HunchLevel _hunchLevel;
    private Animation _barrel, _fire, _quasi, _bell, _jack;

    private int _cursor;
    private bool _demo = true;
    private float _t;
    private Vector2 _barrelPos, _guardPos;
    private int _guardDir = 1;

    public Ch46Screen(RetroGame game) => _game = game;

    public void Enter()
    {
        _climberLevel = ClimberStages.Load(0);
        _hunchLevel = HunchScreens.Load(0);
        _barrel = new Animation(_game.Assets.Texture("barrel"), 16, 16, 12f);
        _fire = new Animation(_game.Assets.Texture("fireball"), 16, 16, 8f);
        _quasi = new Animation(_game.Assets.Texture("quasi_run"), 14, 16, 12f);
        _jack = new Animation(_game.Assets.Texture("jack_run"), 12, 16, 12f).SetRange(1, 3);
        _bell = new Animation(_game.Assets.Texture("bell"), 16, 16, 4f);
        _barrelPos = new Vector2(20, 96);
        _guardPos = new Vector2(40, 96);
        _game.Audio.PlayMusic("music_title");
    }

    public void Leave() => _game.Audio.StopMusic();

    public void Update(float dt)
    {
        _t += dt;
        if (_game.Input.Pressed(Btn.Down)) _cursor = (_cursor + 1) % Roadmap.Length;
        if (_game.Input.Pressed(Btn.Up)) _cursor = (_cursor + Roadmap.Length - 1) % Roadmap.Length;
        if (_game.Input.Pressed(Btn.Jump)) _demo = !_demo;

        _barrel.Update(dt); _fire.Update(dt); _quasi.Update(dt); _jack.Update(dt); _bell.Update(dt);

        // Both games' actors, one update loop, no idea about each other.
        _barrelPos.X += 44f * dt;
        if (_barrelPos.X > W + 16) _barrelPos.X = -16;

        _guardPos.X += _guardDir * 26f * dt;
        if (_guardPos.X > W - 24 || _guardPos.X < 24) _guardDir = -_guardDir;
    }

    public void Draw(SpriteBatch batch)
    {
        var f = _game.Font;
        var px = _game.Assets.Pixel;
        var tiles = _game.Assets.Texture("tiles");

        f.DrawCentred(batch, "WHAT NEXT", W / 2, 8, Theme.Accent);

        // A shared stage: the climber's girder on the left, the runner's rampart
        // on the right, one tile sheet each, drawn by the same code.
        int floorY = 96;
        for (int x = 0; x < W; x += 8)
        {
            bool left = x < W / 2;
            batch.Draw(tiles, new Rectangle(x, floorY, 8, 8),
                       new Rectangle(left ? 0 : 16, 0, 8, 8), Color.White);
        }
        batch.Draw(px, new Rectangle(W / 2, 24, 1, 80), Theme.InkFaint * 0.5f);
        f.Draw(batch, "CLIMBER", new Vector2(10, 26), new Color(216, 40, 88));
        f.DrawRight(batch, "RUNNER", W - 10, 26, new Color(72, 148, 64));

        if (_demo)
        {
            _jack.Draw(batch, new Vector2(34, floorY), false, Color.White);
            _barrel.Draw(batch, _barrelPos, false, Color.White);
            _fire.Draw(batch, new Vector2(88, floorY), false, Color.White);
            _quasi.Draw(batch, new Vector2(150, floorY), _guardDir < 0, Color.White);
            _bell.DrawAt(batch, new Vector2(196, floorY - 24), false, Color.White);
            var guard = _game.Assets.Texture("guard");
            batch.Draw(guard, new Rectangle((int)_guardPos.X, floorY - 16, 16, 16),
                       new Rectangle(((int)(_t * 5) % 2) * 16, 0, 16, 16), Color.White);
        }
        else
        {
            f.DrawCentred(batch, "DEMO OFF", W / 2, floorY - 30, Theme.InkDim);
        }

        f.DrawCentred(batch, "ONE ENGINE, TWO GAMES", W / 2, 110, Theme.Ink);

        int y = 128;
        for (int i = 0; i < Roadmap.Length; i++)
        {
            bool sel = i == _cursor;
            if (sel) batch.Draw(px, new Rectangle(8, y - 2, W - 16, 12), Theme.Highlight);
            f.Draw(batch, (sel ? "> " : "  ") + Roadmap[i].Title, new Vector2(12, y),
                   sel ? Theme.Accent : Color.White);
            y += 13;
        }

        y += 6;
        var detail = Roadmap[_cursor].Detail;
        for (int i = 0; i < detail.Length && y < H - 24; i += 34)
        {
            f.Draw(batch, detail.Substring(i, Math.Min(34, detail.Length - i)),
                   new Vector2(12, y), Theme.Info);
            y += 10;
        }

        f.DrawCentred(batch, "U D ROADMAP   FIRE DEMO", W / 2, H - 14, Theme.InkDim);
    }
}
