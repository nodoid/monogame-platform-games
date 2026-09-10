# Building Two Classic Platform Games - source

Two games, forty-six chapters, ninety-two runnable apps. Every chapter is a
complete Android app and a complete iOS app that build and run on their own, so
you can open the chapter you are reading and press play.

**Monkey Climber** is the portrait one: a single-screen climber at 224x256, four
stages of sloping girders, barrels, fires, pies, springs and rivets.
**Run and Jump** is the landscape one: sixteen flick-screens at 256x192, ropes,
arrows, knights, fireballs and a clock that is the real antagonist.

## Getting it

    git clone https://github.com/nodoid/monogame-platform-games

## Opening it

    PlatformBook.slnx     all forty-six chapters, foldered by the book's parts
    chNN/chNN.slnx        one chapter's two apps on their own

## Building and running

    dotnet build chNN/chNN.slnx -c Release

    dotnet build chNN/Android/chNN.Android.csproj -c Release -t:Run
    dotnet build chNN/iOS/chNN.iOS.csproj -c Release -p:RuntimeIdentifier=ios-arm64

Use `-p:RuntimeIdentifier=iossimulator-arm64` for the simulator. You will need
the .NET 10 SDK with the `android` and `ios` workloads, MonoGame 3.8.5.1,
and Xcode for the iOS side.

## Layout

    _modules/       the engine and both games, the one copy that is edited
    _chapters/      hand-written hosts for the few chapters that need one
    _templates/     the Android and iOS platform heads
    chNN/           a generated chapter: Game/ (copied source), Android/, iOS/
    ../assets/      art and audio, linked into every project rather than copied
    ../tools/       the generators - projects, art, audio, levels, the book

`chNN/` directories are **generated**. Fix a bug in `_modules/`, not in a
chapter copy, then regenerate:

    python3 ../tools/gen_projects.py            # all forty-six
    python3 ../tools/gen_projects.py ch19       # just one
    python3 ../tools/build_all.py               # build all ninety-two

Each chapter copies only the files that chapter has reached, so chapter 14 has
the walking player and chapter 15 the climbing one. Where a chapter needs an
earlier version of a file, `_modules/**/_v/` holds it and it is copied in under
the name the book uses.

## The chapters

### Part 0 - Shared Foundations

    ch01  Why These Two Games
    ch02  Setting Up the Project
    ch03  The Game Loop and Fixed Timestep
    ch04  Sprites, Sheets and the Content Pipeline
    ch05  Input That Feels Right
    ch06  Animation Systems
    ch07  Collision Fundamentals
    ch08  Game States and Screen Flow
    ch09  Building the Audio Engine
    ch10  Sourcing and Making Sound Effects
    ch11  Scoring and the High Score Table
    ch12  Persisting High Scores

### Part 1 - The Climber - Monkey Climber

    ch13  Anatomy of a Single-Screen Climber
    ch14  The Static Playfield
    ch15  Climbing Mechanics
    ch16  The Jump Arc
    ch17  Barrels and Rolling Hazards
    ch18  Enemy Variety Across Stages
    ch19  The Hammer and Power-Ups
    ch20  Scoring, Lives and the Bonus Timer
    ch21  Sound Effects for the Climber
    ch22  Music and the Bonus Timer
    ch23  The High Score Table and Initial Entry
    ch24  Stage Progression and Difficulty
    ch25  Presentation: Cutscenes and Attract Mode
    ch26  Polishing the Climber

### Part 2 - The Runner - Run and Jump

    ch27  Anatomy of a Run-and-Jump
    ch28  Flick-Screen versus Scrolling
    ch29  Level Data and the Tile Map
    ch30  Running Movement
    ch31  Precision Jumping
    ch32  Rope Swings and Traversal Gadgets
    ch33  Hazards and Obstacles
    ch34  Projectiles and Patrolling Knights
    ch35  The Screen Goal and the Bell
    ch36  The Relentless Timer
    ch37  Scoring, Bonuses and the High Score Table
    ch38  Screen Design as Level Design
    ch39  Parallax, Backgrounds and Atmosphere
    ch40  Sound Effects for the Runner
    ch41  Music, Feedback and Audio as a Warning
    ch42  Polishing the Runner

### Part 3 - Beyond the Two Games

    ch43  Performance and Optimisation
    ch44  Settings, Saves and Shared Score Data
    ch45  Packaging and Release
    ch46  Where to Take These Engines Next
