# Building Two Classic Platform Games

A book and its source: two complete platform games for Android and iOS, in C# and
MonoGame, plus the engine they share.

* **Monkey Climber** — a single-screen climber. Portrait, 224x256, four cycling
  stages of sloping girders, barrels that take ladders, fireballs, cement pies,
  unkillable springs, elevators, rivets, a hammer, a bonus timer, three lives and
  arcade three-initial high score entry.
* **Run and Jump** — a flick-screen run-and-jump. Landscape 256x192 for play,
  turning back to portrait 192x256 for the high score table. Sixteen
  hand-designed screens, ropes, arrows, patrolling knights, bouncing fireballs,
  fire pits, parallax, a relentless clock, three lives.

Both ship a **light theme**: pale ground, dark text, daylight backdrop.

## Layout

```
book/              46 chapter documents (.odt, Packt author styles)
book/src/          chapter sources in a small markup, one file per chapter
src/_modules/      the engine and both games (41 files, ~6,800 lines C#)
src/_chapters/     per-chapter demo code for the laboratory chapters
src/_templates/    the Android and iOS head templates
src/PlatformBook.slnx  one solution over all 92 projects, foldered by part
src/chNN/          46 generated apps, each with Android/ and iOS/ heads
src/chNN/chNN.slnx one solution per chapter
assets/            87 generated files: sprites, tiles, font, icons, splash, audio
tools/             the generators
```

## Building an app

Every chapter is a runnable application on both platforms.

```
cd src/ch26
dotnet build ch26.slnx -c Release
dotnet build Android/ch26.Android.csproj -c Release -t:Run
dotnet build iOS/ch26.iOS.csproj -c Release -p:RuntimeIdentifier=ios-arm64
```

`ch26` is the finished climber, `ch42` the finished runner, `ch45` a release
pre-flight check and `ch46` both games' pieces in one loop.

## Regenerating

```
cd tools
python3 gen_climber_art.py  ../assets/climber   # climber sprites and tiles
python3 gen_hunch_art.py    ../assets/hunch     # runner sprites, tiles, backdrop
python3 gen_audio.py        ../assets           # 31 WAVs including two music loops
python3 gen_icons.py        ../assets           # icons, splash screens, bitmap font
python3 gen_climber_stages.py ../src/_modules/Climber/ClimberStages.cs
python3 gen_hunch_screens.py ../src/_modules/Hunch/HunchScreens.cs
python3 gen_projects.py                         # all 46 chapters, both heads
python3 build_all.py                            # build all 92
python3 gen_book.py --measure                   # chapter .odt files, with page counts
```

Every asset is generated. Nothing is sourced or licensed.

The climber's stage generator validates its own output: no two levels closer than
the jump apex, every beam end dropping exactly one level, and every ladder seated
on the beams it joins.

## Requirements

.NET 10 SDK with the `android` and `ios` workloads, a JDK for Android, Xcode for
iOS, and Python 3 with Pillow if you want to regenerate the assets.
