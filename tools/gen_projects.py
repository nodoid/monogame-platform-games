"""Generates one runnable Android app and one runnable iOS app per chapter.

Every chapter directory is self-contained apart from two shared folders that are
referenced, not copied: the engine/game source it needs (copied in, so a reader
can open chapter 19 and read chapter 19's code) and the asset folder (linked, so
there is one copy of the art and audio rather than forty-six).
"""
import json
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chapters

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "src")
MODULES = os.path.join(SRC, "_modules")
TEMPLATES = os.path.join(SRC, "_templates")
CHAPTER_SRC = os.path.join(SRC, "_chapters")
ASSETS = os.path.join(ROOT, "assets")

MONOGAME_VERSION = "3.8.5.1"
ANDROID_TFM = "net10.0-android"
IOS_TFM = "net10.0-ios"

DENSITIES = ["mdpi", "hdpi", "xhdpi", "xxhdpi", "xxxhdpi"]

SPLASH_BG = {"climber": "#EEEEEA", "hunch": "#F0F4F6"}

ROOT_SOLUTION = "PlatformBook.slnx"


# --------------------------------------------------------------------- files --
def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as f:
        f.write(text)
    return path


def fill(template_path, **kw):
    s = open(template_path).read()
    for k, v in kw.items():
        s = s.replace(f"__{k}__", v)
    return s


def asset_items(kind, glob_root, ch):
    """Which asset folders this chapter links. Only the icon and splash are
    copied per chapter; art and audio stay in one place and are referenced."""
    folders = ["climber", "hunch"] if ch["assets"] == "both" else [ch["assets"]]
    folders.append("shared")
    lines = []
    for folder in folders:
        for ext in ("png", "wav"):
            lines.append(f'    <{kind} Include="{glob_root}\\{folder}\\**\\*.{ext}" '
                         f'Link="Content\\%(Filename)%(Extension)" />')
    return "\n".join(lines)


# ----------------------------------------------------------------- android ----
# Only the builds that exist to be photographed define this. It turns on the
# outline the capture tool trims to; see Dbg.CaptureFrame.
CAPTURE_DEFINE = "\n    <DefineConstants>$(DefineConstants);CAPTURE</DefineConstants>"


def android_csproj(ch, capture=False):
    assets_glob = os.path.relpath(ASSETS, os.path.join(ch["dir"], "Android"))
    return f"""<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>{ANDROID_TFM}</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <RootNamespace>{ch['ns']}</RootNamespace>{CAPTURE_DEFINE if capture else ''}
    <AssemblyName>{ch['id']}.Android</AssemblyName>
    <ApplicationId>{ch['appid']}</ApplicationId>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <SupportedOSPlatformVersion>23</SupportedOSPlatformVersion>
    <AndroidPackageFormat>apk</AndroidPackageFormat>
    <!-- Games do not want the default .NET culture data or debug symbols in the
         shipped package; both cost megabytes for nothing. -->
    <InvariantGlobalization>true</InvariantGlobalization>
    <AndroidUseAapt2>true</AndroidUseAapt2>
    <EnableLLVM Condition="'$(Configuration)'=='Release'">false</EnableLLVM>
    <AndroidLinkMode Condition="'$(Configuration)'=='Release'">SdkOnly</AndroidLinkMode>
    <DebugSymbols Condition="'$(Configuration)'=='Release'">false</DebugSymbols>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MonoGame.Framework.Android" Version="{MONOGAME_VERSION}" />
  </ItemGroup>

  <ItemGroup>
    <!-- Shared game code lives one level up so both heads compile the same files. -->
    <Compile Include="..\\Game\\**\\*.cs" LinkBase="Game" />
  </ItemGroup>

  <ItemGroup>
    <!-- Art and audio are linked from the book's single asset folder. On Android
         an AndroidAsset lands in assets/, which is what TitleContainer reads. -->
{asset_items("AndroidAsset", assets_glob, ch)}
  </ItemGroup>

</Project>
"""


def android_head(ch, capture=False):
    out = ch["dir"] + "/Android"
    write(out + f"/{ch['id']}.Android.csproj", android_csproj(ch, capture))
    write(out + "/MainActivity.cs", fill(
        TEMPLATES + "/android/MainActivity.cs",
        NS=ch["ns"], LABEL=ch["label"], GAME=ch["game_class"],
        ORIENT="SensorPortrait" if ch["orient"] == "Portrait" else "SensorLandscape",
        MODE=ch["orient"]))

    res = out + "/Resources"
    write(res + "/values/styles.xml", open(TEMPLATES + "/android/styles.xml").read())
    write(res + "/drawable/splash.xml", open(TEMPLATES + "/android/splash.xml").read())
    write(res + "/values/colors.xml",
          '<?xml version="1.0" encoding="utf-8"?>\n<resources>\n'
          f'  <color name="splash_background">{SPLASH_BG[ch["icon"]]}</color>\n'
          '</resources>\n')

    icon_src = os.path.join(ASSETS, ch["icon"], "icon")
    splash_src = os.path.join(ASSETS, ch["icon"], "splash")
    for d in DENSITIES:
        os.makedirs(f"{res}/mipmap-{d}", exist_ok=True)
        shutil.copy(f"{icon_src}/ic_launcher_{d}.png", f"{res}/mipmap-{d}/ic_launcher.png")
        os.makedirs(f"{res}/drawable-{d}", exist_ok=True)
        shutil.copy(f"{splash_src}/splash_{d}.png", f"{res}/drawable-{d}/splash_art.png")


# --------------------------------------------------------------------- ios ----
def ios_csproj(ch, capture=False):
    assets_glob = os.path.relpath(ASSETS, os.path.join(ch["dir"], "iOS"))
    return f"""<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>{IOS_TFM}</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <RootNamespace>{ch['ns']}</RootNamespace>{CAPTURE_DEFINE if capture else ''}
    <AssemblyName>{ch['id']}.iOS</AssemblyName>
    <ApplicationId>{ch['appid']}</ApplicationId>
    <ApplicationTitle>{ch['label']}</ApplicationTitle>
    <ApplicationVersion>1</ApplicationVersion>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <SupportedOSPlatformVersion>13.0</SupportedOSPlatformVersion>
    <InvariantGlobalization>true</InvariantGlobalization>
    <XSAppIconAssets>Assets.xcassets/AppIcon.appiconset</XSAppIconAssets>
    <!-- SdkOnly keeps the linker away from our own reflection-free code while
         still stripping the framework; full linking has caught us out on
         SoundEffect construction in the past. -->
    <MtouchLink>SdkOnly</MtouchLink>
    <MtouchInterpreter Condition="'$(Configuration)'=='Debug'">-all</MtouchInterpreter>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MonoGame.Framework.iOS" Version="{MONOGAME_VERSION}" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="..\\Game\\**\\*.cs" LinkBase="Game" />
  </ItemGroup>

  <ItemGroup>
    <InterfaceDefinition Include="LaunchScreen.storyboard" />
    <ImageAsset Include="Assets.xcassets\\**\\*" />
  </ItemGroup>

  <ItemGroup>
    <!-- BundleResource copies to the app bundle root, where TitleContainer looks. -->
{asset_items("BundleResource", assets_glob, ch)}
  </ItemGroup>

</Project>
"""


def info_plist(ch):
    if ch["orient"] == "Portrait" and not ch["rotates"]:
        orients = ["UIInterfaceOrientationPortrait"]
    elif ch["orient"] == "Landscape" and not ch["rotates"]:
        orients = ["UIInterfaceOrientationLandscapeLeft", "UIInterfaceOrientationLandscapeRight"]
    else:
        orients = ["UIInterfaceOrientationPortrait",
                   "UIInterfaceOrientationLandscapeLeft",
                   "UIInterfaceOrientationLandscapeRight"]
    items = "\n".join(f"\t\t<string>{o}</string>" for o in orients)
    return f"""<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
\t<key>CFBundleIdentifier</key><string>{ch['appid']}</string>
\t<key>CFBundleName</key><string>{ch['label']}</string>
\t<key>CFBundleDisplayName</key><string>{ch['label']}</string>
\t<key>CFBundleExecutable</key><string>{ch['id']}.iOS</string>
\t<key>CFBundleShortVersionString</key><string>1.0</string>
\t<key>CFBundleVersion</key><string>1</string>
\t<key>LSRequiresIPhoneOS</key><true/>
\t<key>MinimumOSVersion</key><string>13.0</string>
\t<key>UIDeviceFamily</key>
\t<array><integer>1</integer><integer>2</integer></array>
\t<key>UIRequiredDeviceCapabilities</key>
\t<array><string>arm64</string></array>
\t<key>UILaunchStoryboardName</key><string>LaunchScreen</string>
\t<key>UIStatusBarHidden</key><true/>
\t<key>UIViewControllerBasedStatusBarAppearance</key><false/>
\t<key>UIRequiresFullScreen</key><true/>
\t<!-- The delegate may narrow this list at runtime but can never widen it. -->
\t<key>UISupportedInterfaceOrientations</key>
\t<array>
{items}
\t</array>
\t<key>UISupportedInterfaceOrientations~ipad</key>
\t<array>
{items}
\t</array>
</dict>
</plist>
"""


APPICON_CONTENTS = json.dumps({
    "images": [{"filename": "AppIcon1024.png", "idiom": "universal",
                "platform": "ios", "size": "1024x1024"}],
    "info": {"author": "xcode", "version": 1}}, indent=2)

SPLASH_CONTENTS = json.dumps({
    "images": [{"filename": "Splash.png", "idiom": "universal", "scale": "1x"},
               {"filename": "Splash@2x.png", "idiom": "universal", "scale": "2x"},
               {"filename": "Splash@3x.png", "idiom": "universal", "scale": "3x"}],
    "info": {"author": "xcode", "version": 1}}, indent=2)

XCASSETS_ROOT = json.dumps({"info": {"author": "xcode", "version": 1}}, indent=2)


def ios_head(ch, capture=False):
    out = ch["dir"] + "/iOS"
    write(out + f"/{ch['id']}.iOS.csproj", ios_csproj(ch, capture))
    mask = ("UIInterfaceOrientationMask.Portrait" if ch["orient"] == "Portrait"
            else "UIInterfaceOrientationMask.Landscape")
    write(out + "/Program.cs", fill(TEMPLATES + "/ios/Program.cs",
                                    NS=ch["ns"], GAME=ch["game_class"],
                                    MODE=ch["orient"], MASK=mask))
    write(out + "/Info.plist", info_plist(ch))
    write(out + "/LaunchScreen.storyboard",
          open(TEMPLATES + "/ios/LaunchScreen.storyboard").read())

    xc = out + "/Assets.xcassets"
    write(xc + "/Contents.json", XCASSETS_ROOT)
    write(xc + "/AppIcon.appiconset/Contents.json", APPICON_CONTENTS)
    shutil.copy(os.path.join(ASSETS, ch["icon"], "icon", "ios_appicon_1024.png"),
                xc + "/AppIcon.appiconset/AppIcon1024.png")
    write(xc + "/Splash.imageset/Contents.json", SPLASH_CONTENTS)
    sp = os.path.join(ASSETS, ch["icon"], "splash")
    shutil.copy(f"{sp}/ios_splash@1x.png", xc + "/Splash.imageset/Splash.png")
    shutil.copy(f"{sp}/ios_splash@2x.png", xc + "/Splash.imageset/Splash@2x.png")
    shutil.copy(f"{sp}/ios_splash@3x.png", xc + "/Splash.imageset/Splash@3x.png")


# ------------------------------------------------------------------ chapter ---
def copy_sources(ch, capture=False):
    """Copy this chapter's module set in. Entries are either a path, or a
    (source, destination) pair where a chapter uses an earlier version of a
    file - `Climber/_v/ClimberPlayer.v1.cs` lands as `Climber/ClimberPlayer.cs`, so the
    reader always opens the file at the name the book calls it."""
    dest = os.path.join(ch["dir"], "Game")
    if os.path.isdir(dest):
        shutil.rmtree(dest)
    for entry in ch["modules"]:
        src_rel, dst_rel = entry if isinstance(entry, tuple) else (entry, entry)
        src = os.path.join(MODULES, src_rel)
        if not os.path.isfile(src):
            raise FileNotFoundError(src)
        target = os.path.join(dest, dst_rel)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        shutil.copy(src, target)

    own = os.path.join(CHAPTER_SRC, ch["id"])
    if os.path.isdir(own):
        for name in sorted(os.listdir(own)):
            if name.endswith(".cs"):
                shutil.copy(os.path.join(own, name), os.path.join(dest, name))

    host = generate_host(ch, capture)
    if host:
        write(os.path.join(dest, f"Ch{ch['num']:02d}Game.cs"), host)


def generate_host(ch, capture=False):
    """Each chapter's app needs a Game class. Only chapters 1 and 2 write their
    own; everything else gets one generated from the manifest, because the only
    thing that varies is which screen it opens and which debug lens it turns on."""
    n = ch["num"]
    host, ns, cls = ch["host"], ch["ns"], f"Ch{ch['num']:02d}Game"
    if host == "raw":
        return None

    lens = ch.get("lens") or {}
    caption = ch.get("caption") or f"CH{n:02d}"
    lens_lines = [f'        Dbg.Caption = "{caption}";']
    for k, v in sorted(lens.items()):
        lens_lines.append(f"        Dbg.{k} = {str(v).lower()};")
    if capture:
        # Capture build only: skip the menus and open on the stage or screen
        # this chapter is about, so the figure needs no human present. The
        # outline the figure is trimmed to comes from the CAPTURE constant.
        shot = ch.get("shot") or {}
        if shot.get("play", True) and ch["host"] in ("climber", "hunch"):
            lens_lines.append("        Dbg.AutoPlay = true;")
        if shot.get("level"):
            lens_lines.append(f'        Dbg.StartLevel = {shot["level"]};')
    lens_body = "\n".join(lens_lines)

    if host == "screen":
        portrait = ch["orient"] == "Portrait"
        w, h = (224, 256) if portrait else (256, 192)
        return f"""using Microsoft.Xna.Framework;
using Retro.Engine;

namespace {ns};

/// <summary>
/// Chapter {n} - {ch['title']}.
///
/// Generated host: sets the orientation, sizes the virtual screen and opens this
/// chapter's demo. See tools/gen_projects.py.
/// </summary>
public class {cls} : RetroGame
{{
    public {cls}(IPlatformService platform) : base(platform, {w}, {h}) {{ }}

    protected override void OnLoaded()
    {{
        LockOrientation(OrientationMode.{ch['orient']});
        Dbg.Reset();
{lens_body}
        Screens.Replace(new Ch{n:02d}Screen(this), fade: false);
    }}
}}
"""

    if host in ("climberfactory", "hunchfactory"):
        base_cls = ("Retro.Climber.ClimberSandboxGame" if host == "climberfactory"
                    else "Retro.Hunch.HunchSandboxGame")
        using = "using Retro.Climber;" if host == "climberfactory" else "using Retro.Hunch;"
        return f"""using Microsoft.Xna.Framework;
using Retro.Engine;
{using}

namespace {ns};

/// <summary>Chapter {n} - {ch['title']}.</summary>
public class {cls} : {base_cls}
{{
    public {cls}(IPlatformService platform)
        : base(platform, g => {ch['factory']})
    {{
        Dbg.Reset();
{lens_body}
    }}
}}
"""

    base_cls = "Retro.Climber.ClimberGame" if host == "climber" else "Retro.Hunch.HunchGame"
    return f"""using Microsoft.Xna.Framework;
using Retro.Engine;

namespace {ns};

/// <summary>
/// Chapter {n} - {ch['title']}.
///
/// The complete game, with this chapter's debug lens switched on so the app
/// shows what the chapter is about. Turn the lens off and it is the shipping
/// build.
/// </summary>
public class {cls} : {base_cls}
{{
    public {cls}(IPlatformService platform) : base(platform)
    {{
        Dbg.Reset();
{lens_body}
    }}
}}
"""


# ---------------------------------------------------------------- solutions --
def chapter_slnx(ch):
    """One solution per chapter, holding that chapter's two apps.

    A reader working through chapter 19 opens `ch19/ch19.slnx` and sees exactly
    two projects. The XML solution format is used rather than the old `.sln`
    because it is short enough to generate honestly and to read in a diff."""
    return f"""<Solution>
  <Project Path="Android/{ch['id']}.Android.csproj" />
  <Project Path="iOS/{ch['id']}.iOS.csproj" />
</Solution>
"""


def root_slnx(all_chapters):
    """One solution over every chapter, foldered by the book's four parts.

    Ninety-two projects is a lot to scroll, so each chapter gets its own folder
    inside its part - the same shape as the table of contents."""
    lines = ["<Solution>"]
    for part, name, first, last in chapters.PARTS:
        lines.append(f'  <Folder Name="/Part {part} - {name}/" />')
        for ch in all_chapters:
            if not first <= ch["num"] <= last:
                continue
            folder = f"/Part {part} - {name}/{ch['id']} {ch['short']}/"
            lines.append(f'  <Folder Name="{folder}">')
            lines.append(f'    <Project Path="{ch["id"]}/Android/{ch["id"]}.Android.csproj" />')
            lines.append(f'    <Project Path="{ch["id"]}/iOS/{ch["id"]}.iOS.csproj" />')
            lines.append("  </Folder>")
    lines.append("</Solution>")
    return "\n".join(lines) + "\n"


def root_readme(all_chapters):
    """The README at the root of the source tree.

    Written from the manifest rather than by hand, for the same reason the
    projects are: forty-six chapters is too many to keep a hand-written index
    honest, and an index that has drifted is worse than none."""
    rows = []
    for part, name, first, last in chapters.PARTS:
        rows.append("")
        rows.append(f"### Part {part} - {name}")
        rows.append("")
        for ch in all_chapters:
            if first <= ch["num"] <= last:
                rows.append(f"    {ch['id']}  {ch['title']}")
    index = "\n".join(rows)

    return f"""# Building Two Classic Platform Games - source

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

    {ROOT_SOLUTION:<22}all forty-six chapters, foldered by the book's parts
    {'chNN/chNN.slnx':<22}one chapter's two apps on their own

## Building and running

    dotnet build chNN/chNN.slnx -c Release

    dotnet build chNN/Android/chNN.Android.csproj -c Release -t:Run
    dotnet build chNN/iOS/chNN.iOS.csproj -c Release -p:RuntimeIdentifier=ios-arm64

Use `-p:RuntimeIdentifier=iossimulator-arm64` for the simulator. You will need
the .NET {ANDROID_TFM.split('-')[0].replace('net', '').removesuffix('.0')} SDK with the `android` and `ios` workloads, MonoGame {MONOGAME_VERSION},
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
{index}
"""


def solution(ch):
    return f"""# {ch['id']} - {ch['title']}

Chapter {ch['num']} of *Building Two Classic Platform Games*.

Open `{ch['id']}.slnx` for this chapter alone, or `../{ROOT_SOLUTION}` for all
forty-six, foldered by the book's four parts.

    Both      dotnet build {ch['id']}.slnx -c Release
    Android   dotnet build Android/{ch['id']}.Android.csproj -c Release -t:Run
    iOS       dotnet build iOS/{ch['id']}.iOS.csproj -c Release \\
                  -p:RuntimeIdentifier=ios-arm64

Orientation: {ch['orient']}{' (rotates at runtime)' if ch['rotates'] else ' (locked)'}
Shared code in `Game/`, art and audio linked from `../../assets/{ch['assets']}`.
"""


def build_chapter(ch, capture=False):
    ch["dir"] = os.path.join(SRC, ch["id"])
    os.makedirs(ch["dir"], exist_ok=True)
    copy_sources(ch, capture)
    android_head(ch, capture)
    ios_head(ch, capture)
    write(ch["dir"] + f"/{ch['id']}.slnx", chapter_slnx(ch))
    write(ch["dir"] + "/README.md", solution(ch))
    return ch["dir"]


def main(argv):
    """gen_projects.py [--capture] [chNN ...] - regenerate chapter projects."""
    capture = "--capture" in argv
    only = [a for a in argv if a.startswith("ch")]
    for ch in chapters.ALL:
        if only and ch["id"] not in only:
            continue
        build_chapter(ch, capture=capture)
        print(ch["id"], "generated" + (" (capture)" if capture else ""), flush=True)

    if not only:
        write(os.path.join(SRC, ROOT_SOLUTION), root_slnx(chapters.ALL))
        write(os.path.join(SRC, "README.md"), root_readme(chapters.ALL))
        print(ROOT_SOLUTION, "and README.md generated", flush=True)


if __name__ == "__main__":
    import sys as _s
    main(_s.argv[1:])
