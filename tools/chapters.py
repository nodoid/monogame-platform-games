"""The chapter manifest: title, app, module set and debug lens for all 46 chapters."""

E_CORE = ["Engine/VirtualScreen.cs", "Engine/Assets.cs", "Engine/BitmapFont.cs",
          "Engine/Buttons.cs", "Engine/InputState.cs", "Engine/VirtualPad.cs",
          "Engine/IPlatformService.cs", "Engine/Animation.cs", "Engine/ScreenManager.cs",
          "Engine/Dbg.cs", "Engine/Theme.cs"]

E_V1 = E_CORE + [("Engine/_v/RetroGame.v1.cs", "Engine/RetroGame.cs")]

E_FULL = E_CORE + ["Engine/AudioManager.cs", "Engine/RetroGame.cs",
                   "Engine/HighScoreTable.cs", "Engine/HighScoreStore.cs",
                   "Engine/CommonScreens.cs"]

E_MIN = ["Engine/Assets.cs", "Engine/BitmapFont.cs", "Engine/IPlatformService.cs",
         "Engine/Theme.cs"]

C_CORE = ["Climber/ClimberLevel.cs", "Climber/ClimberStages.cs", "Climber/ClimberTuning.cs"]
C_FULL = C_CORE + ["Climber/ClimberPlayer.cs", "Climber/ClimberEntities.cs", "Climber/ClimberSession.cs",
                   "Climber/ClimberPlayScreen.cs", "Climber/ClimberGame.cs"]

H_CORE = ["Hunch/HunchLevel.cs", "Hunch/HunchScreens.cs", "Hunch/Parallax.cs",
          "Hunch/HunchEntities.cs"]
H_FULL = H_CORE + ["Hunch/HunchRunner.cs", "Hunch/HunchSession.cs",
                   "Hunch/HunchPlayScreen.cs", "Hunch/HunchGame.cs"]

SANDBOX_C = ["Climber/_v/ClimberSandboxGame.cs"]
SANDBOX_H = ["Hunch/_v/HunchSandboxScreen.cs"]


def C(num, title, host, modules, orient="Portrait", icon="climber", assets="climber",
      rotates=False, lens=None, caption=None, factory=None, base_game=None,
      shot=None):
    return dict(num=num, title=title, host=host, modules=list(modules), orient=orient,
                icon=icon, assets=assets, rotates=rotates, lens=lens or {},
                caption=caption, factory=factory, base_game=base_game,
                shot=shot or {})


CHAPTERS = [
    # ---------------------------------------------------- Part Zero: engine ---
    C(1, "Why These Two Games", "raw", E_MIN + ["Engine/Dbg.cs"], assets="climber"),
    C(2, "Setting Up the Project", "raw", E_MIN + ["Engine/Dbg.cs"]),
    C(3, "The Game Loop and Fixed Timestep", "screen", E_V1),
    C(4, "Sprites, Sheets and the Content Pipeline", "screen", E_V1),
    C(5, "Input That Feels Right", "screen", E_V1),
    C(6, "Animation Systems", "screen", E_V1),
    C(7, "Collision Fundamentals", "screen", E_V1),
    C(8, "Game States and Screen Flow", "screen", E_V1),
    C(9, "Building the Audio Engine", "screen", E_FULL),
    C(10, "Sourcing and Making Sound Effects", "screen", E_FULL),
    C(11, "Scoring and the High Score Table", "screen", E_FULL),
    C(12, "Persisting High Scores", "screen", E_FULL),

    # -------------------------------------------------- Part One: the climber -
    C(13, "Anatomy of a Single-Screen Climber", "climberfactory",
      E_FULL + C_CORE + SANDBOX_C + ["Climber/_v/ClimberLevelViewer.cs"],
      factory="new ClimberLevelViewer(g)"),
    C(14, "The Static Playfield", "climberfactory",
      E_FULL + C_CORE + SANDBOX_C + ["Climber/_v/ClimberSandboxScreen.cs",
                                     ("Climber/_v/ClimberPlayer.v1.cs", "Climber/ClimberPlayer.cs")],
      factory='new ClimberSandboxScreen(g, "CH14 WALKING")'),
    C(15, "Climbing Mechanics", "climberfactory",
      E_FULL + C_CORE + SANDBOX_C + ["Climber/_v/ClimberSandboxScreen.cs",
                                     ("Climber/_v/ClimberPlayer.v2.cs", "Climber/ClimberPlayer.cs")],
      factory='new ClimberSandboxScreen(g, "CH15 CLIMBING")'),
    C(16, "The Jump Arc", "climberfactory",
      E_FULL + C_CORE + SANDBOX_C + ["Climber/_v/ClimberSandboxScreen.cs", "Climber/ClimberPlayer.cs"],
      factory='new ClimberSandboxScreen(g, "CH16 JUMPING")'),

    C(17, "Barrels and Rolling Hazards", "climber", E_FULL + C_FULL,
      lens=dict(Paths=True, Ladders=True), caption="CH17 BARREL PATHS"),
    C(18, "Enemy Variety Across Stages", "climber", E_FULL + C_FULL,
      lens=dict(Hitboxes=True), caption="CH18 ENEMIES"),
    C(19, "The Hammer and Power-Ups", "climber", E_FULL + C_FULL,
      lens=dict(Hitboxes=True), caption="CH19 HAMMER BOX"),
    C(20, "Scoring, Lives and the Bonus Timer", "climber", E_FULL + C_FULL,
      caption="CH20 SCORE AND LIVES"),
    C(21, "Sound Effects for the Climber", "climber", E_FULL + C_FULL,
      lens=dict(AudioMeter=True), caption="CH21 SOUND"),
    C(22, "Music and the Bonus Timer", "climber", E_FULL + C_FULL,
      lens=dict(AudioMeter=True, Timing=True), caption="CH22 MUSIC TEMPO"),
    C(23, "The High Score Table and Initial Entry", "climber", E_FULL + C_FULL,
      caption="CH23 HIGH SCORES"),
    C(24, "Stage Progression and Difficulty", "climber", E_FULL + C_FULL,
      lens=dict(Ladders=True, Timing=True), caption="CH24 DIFFICULTY"),
    C(25, "Presentation: Cutscenes and Attract Mode", "climber", E_FULL + C_FULL,
      caption="CH25 ATTRACT MODE"),
    C(26, "Polishing the Climber", "climber", E_FULL + C_FULL,
      lens=dict(Hitboxes=True, JumpArc=True, Timing=True), caption="CH26 POLISH"),

    # --------------------------------------------------- Part Two: the runner -
    C(27, "Anatomy of a Run-and-Jump", "hunchfactory",
      E_FULL + H_CORE + ["Hunch/HunchRunner.cs", "Hunch/_v/HunchSandboxScreen.cs",
                         "Hunch/_v/HunchScreenViewer.cs"],
      orient="Landscape", icon="hunch", assets="hunch",
      factory="new HunchScreenViewer(g)"),
    C(28, "Flick-Screen versus Scrolling", "hunchfactory",
      E_FULL + H_CORE + ["Hunch/HunchRunner.cs", "Hunch/_v/HunchSandboxScreen.cs",
                         "Hunch/_v/HunchCameraScreen.cs"],
      orient="Landscape", icon="hunch", assets="hunch",
      factory="new HunchCameraScreen(g)"),
    C(29, "Level Data and the Tile Map", "hunchfactory",
      E_FULL + H_CORE + ["Hunch/HunchRunner.cs", "Hunch/_v/HunchSandboxScreen.cs"],
      orient="Landscape", icon="hunch", assets="hunch",
      factory='new HunchSandboxScreen(g, "CH29 TILE MAP")'),
    C(30, "Running Movement", "hunchfactory",
      E_FULL + H_CORE + [("Hunch/_v/HunchRunner.v1.cs", "Hunch/HunchRunner.cs"),
                         "Hunch/_v/HunchSandboxScreen.cs"],
      orient="Landscape", icon="hunch", assets="hunch",
      factory='new HunchSandboxScreen(g, "CH30 RUNNING")'),
    C(31, "Precision Jumping", "hunchfactory",
      E_FULL + H_CORE + [("Hunch/_v/HunchRunner.v2.cs", "Hunch/HunchRunner.cs"),
                         "Hunch/_v/HunchSandboxScreen.cs"],
      orient="Landscape", icon="hunch", assets="hunch",
      factory='new HunchSandboxScreen(g, "CH31 JUMPING")'),

    C(32, "Rope Swings and Traversal Gadgets", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(Paths=True, Hitboxes=True), caption="CH32 ROPE ARCS"),
    C(33, "Hazards and Obstacles", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(Hitboxes=True), caption="CH33 HAZARD BOXES"),
    C(34, "Projectiles and Patrolling Guards", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(Hitboxes=True, AudioMeter=True), caption="CH34 PROJECTILES"),
    C(35, "The Screen Goal and the Bell", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(Hitboxes=True), caption="CH35 THE BELL"),
    C(36, "The Relentless Timer", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(Timing=True), caption="CH36 THE CLOCK"),
    C(37, "Scoring, Bonuses and the High Score Table", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      caption="CH37 SCORE AND BONUS"),
    C(38, "Screen Design as Level Design", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(TileGrid=True, JumpArc=True), caption="CH38 SCREEN DESIGN"),
    C(39, "Parallax, Backgrounds and Atmosphere", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(Camera=True), caption="CH39 PARALLAX"),
    C(40, "Sound Effects for the Runner", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(AudioMeter=True), caption="CH40 SOUND"),
    C(41, "Music, Feedback and Audio as a Warning", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(AudioMeter=True, Timing=True), caption="CH41 AUDIO WARNINGS"),
    C(42, "Polishing the Runner", "hunch", E_FULL + H_FULL,
      orient="Portrait", icon="hunch", assets="hunch",
      lens=dict(Hitboxes=True, JumpArc=True, Timing=True), caption="CH42 POLISH"),

    # ------------------------------------------------ Part Three: both games --
    C(43, "Performance and Optimisation", "climber", E_FULL + C_FULL,
      lens=dict(Timing=True, AudioMeter=True), caption="CH43 PERFORMANCE"),
    C(44, "Settings, Saves and Shared Score Data", "screen", E_FULL + C_FULL + H_FULL,
      assets="both", caption="CH44 SETTINGS"),
    C(45, "Packaging and Release", "screen", E_FULL + C_FULL + H_FULL,
      assets="both", caption="CH45 RELEASE CHECK"),
    C(46, "Where to Take These Engines Next", "screen", E_FULL + C_FULL + H_FULL,
      assets="both", caption="CH46 BOTH GAMES"),
]

PARTS = [
    (0, "Shared Foundations", 1, 12),
    (1, "The Climber - Monkey Climber", 13, 26),
    (2, "The Runner - Run and Jump", 27, 42),
    (3, "Beyond the Two Games", 43, 46),
]

GAME_CLASS = {
    "climber": "Retro.Climber.ClimberGame",
    "hunch": "Retro.Hunch.HunchGame",
    "climberfactory": "Retro.Climber.ClimberSandboxGame",
    "hunchfactory": "Retro.Hunch.HunchSandboxGame",
}


# Home-screen labels have room for about twelve characters, so each chapter gets
# a hand-picked short name rather than a truncated title.
SHORT = {
    1: "Two Games", 2: "Setup", 3: "Game Loop", 4: "Sprites", 5: "Input",
    6: "Animation", 7: "Collision", 8: "Screens", 9: "Audio", 10: "Sound FX",
    11: "Scores", 12: "Saving",
    13: "Stages", 14: "Walking", 15: "Climbing", 16: "Jumping", 17: "Barrels",
    18: "Enemies", 19: "Hammer", 20: "Lives", 21: "Climber SFX", 22: "Climber Music",
    23: "Hi Scores", 24: "Difficulty", 25: "Attract", 26: "Climber Polish",
    27: "Screens 15", 28: "Camera", 29: "Tile Map", 30: "Running", 31: "Jump Feel",
    32: "Ropes", 33: "Hazards", 34: "Arrows", 35: "The Bell", 36: "The Clock",
    37: "Bonus", 38: "Design", 39: "Parallax", 40: "Bell SFX", 41: "Warnings",
    42: "Bell Polish",
    43: "Speed", 44: "Settings", 45: "Release", 46: "Both Games",
}


def enrich(ch):
    n = ch["num"]
    ch["id"] = f"ch{n:02d}"
    ch["ns"] = f"Chapter{n:02d}"
    ch["appid"] = f"com.retrobook.ch{n:02d}"
    ch["short"] = SHORT.get(n, str(n))
    ch["label"] = f"{n:02d} {ch['short']}"
    ch["game_class"] = f"{ch['ns']}.Ch{n:02d}Game"
    return ch


def part_of(num):
    for p, name, a, b in PARTS:
        if a <= num <= b:
            return p, name
    return 3, PARTS[-1][1]


SHOTS = {17: {'level': 0, 'wait': 11}, 18: {'level': 1, 'wait': 11}, 19: {'level': 0, 'wait': 13}, 20: {'level': 0, 'wait': 11}, 21: {'level': 0, 'wait': 11}, 22: {'level': 0, 'wait': 13}, 23: {'play': False, 'wait': 20}, 24: {'level': 3, 'wait': 11}, 25: {'play': False, 'wait': 10}, 26: {'level': 0, 'wait': 12}, 43: {'level': 2, 'wait': 11}, 32: {'level': 3, 'wait': 9}, 33: {'level': 7, 'wait': 9}, 34: {'level': 11, 'wait': 10}, 35: {'level': 0, 'wait': 9}, 36: {'level': 15, 'wait': 9}, 37: {'play': False, 'wait': 15}, 38: {'level': 15, 'wait': 9}, 39: {'level': 4, 'wait': 9}, 40: {'level': 8, 'wait': 10}, 41: {'level': 8, 'wait': 11}, 42: {'level': 15, 'wait': 10}}


def _apply_shots(ch):
    ch["shot"] = SHOTS.get(ch["num"], {})
    # The runner's window is locked portrait and its gameplay is presented turned
    # a quarter turn (see VirtualScreen.Rotated), so a screenshot of a runner
    # chapter that is actually playing comes off the device on its side and has
    # to be turned back before it can go in the book.
    # A runner figure is landscape whenever the app is actually playing, and the
    # simulator always hands back a portrait-framed screenshot, so the picture
    # arrives on its side either way: the sandbox chapters because iOS turned
    # the scene for their landscape lock, and the game chapters because the
    # engine turned its own presentation inside a portrait window.
    ch["turned"] = ch["icon"] == "hunch" and (ch["orient"] == "Landscape"
                                              or ch["shot"].get("play", True))
    # The virtual screen's aspect, used to trim a screenshot exactly. This is the
    # aspect on the glass, so a turned shot is measured portrait.
    # The aspect is the one on the glass, and every runner figure lands in a
    # portrait frame, turned or not.
    ch["aspect"] = 192.0 / 256.0 if ch["icon"] == "hunch" else 224.0 / 256.0
    return ch


ALL = [_apply_shots(enrich(c)) for c in CHAPTERS]
