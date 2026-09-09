"""Deploys every chapter's app to a device and photographs it.

Climber chapters go to an Android emulator, runner chapters to an iOS simulator,
which is what the book's figures are captured on. The shot is auto-cropped to the
letterboxed game area by finding the pixels that are not the theme's bar colour,
so a figure is the game rather than a picture of a phone.

Run tools/gen_projects.py with capture=True first: the capture build sets
Dbg.AutoPlay and Dbg.StartLevel so a screenshot can be taken without a human
pressing start.
"""
import os
import re
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chapters

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "src")
FIGS = os.path.join(ROOT, "book", "figures")
ADB = os.path.expanduser("~/Library/Android/sdk/platform-tools/adb")

BARS = (206, 208, 204)          # Theme.Bars - the letterbox
GROUND = (238, 238, 234)        # Theme.Background - inside the virtual screen


def run(cmd, **kw):
    return subprocess.run(cmd, shell=isinstance(cmd, str), capture_output=True,
                          text=True, **kw)


def platform_for(ch):
    """Climber chapters are photographed on Android, runner chapters on iOS."""
    return "ios" if ch["icon"] == "hunch" else "android"


MARK = (255, 0, 255)            # Dbg.CaptureMark - the capture build's outline


def crop_to_marker(im):
    """Trim to the outline a capture build draws around its own picture.

    Colour-guessing the letterbox does not survive the light theme: a panel
    inside the game can be the same grey as the bars around it, and the figure
    comes out clipped. The marker is exact, so use it when it is there.

    The outline is one pixel wide, so it has to be looked for along complete
    rows and columns - sampling on a grid steps straight over it. Any row
    between the outline's top and bottom crosses both of its verticals, and any
    column between its left and right crosses both horizontals, so a handful of
    lines through the picture is enough to find all four edges."""
    px = im.load()
    w, h = im.size

    def ismark(c):
        return abs(c[0] - 255) < 24 and c[1] < 40 and abs(c[2] - 255) < 24

    def span(hits):
        return (hits[0], hits[-1]) if len(hits) >= 2 and hits[-1] - hits[0] >= 64 else None

    horizontal = vertical = None
    for f in (2, 3, 5, 8, 13, 21):
        if horizontal is None:
            horizontal = span([x for x in range(w) if ismark(px[x, h * (f - 1) // f])])
        if vertical is None:
            vertical = span([y for y in range(h) if ismark(px[w * (f - 1) // f, y])])
        if horizontal and vertical:
            break
    if not (horizontal and vertical):
        return None

    box = [horizontal[0], vertical[0], horizontal[1] + 1, vertical[1] + 1]

    # Step inside the outline - and keep stepping until none of it is left,
    # because a device that reports a fractional pixel ratio can smear a
    # one-pixel line over two.
    out = None
    for inset in range(1, 6):
        out = im.crop((box[0] + inset, box[1] + inset,
                       box[2] - inset, box[3] - inset))
        o = out.load()
        ow, oh = out.size
        edge = ([(x, 0) for x in range(ow)] + [(x, oh - 1) for x in range(ow)] +
                [(0, y) for y in range(oh)] + [(ow - 1, y) for y in range(oh)])
        if not any(ismark(o[x, y]) for x, y in edge):
            return out
    return out


def crop_to_game(path, aspect=None):
    """Trim to the virtual screen.

    Scanning for "not the letterbox colour" picks up the on-screen pad, which is
    drawn over the bars; scanning for the game's background colour fails on the
    runner, whose sky covers every pixel. So walk outwards from the centre
    instead and stop at the first bar-coloured pixel: the game area is one
    contiguous rectangle surrounded by bars, and the pad is inside it."""
    from PIL import Image
    im = Image.open(path).convert("RGB")
    marked = crop_to_marker(im)
    if marked is not None:
        return marked

    w, h = im.size
    px = im.load()

    def isbar(c):
        return all(abs(c[i] - BARS[i]) < 6 for i in range(3))

    cx, cy = w // 2, h // 2
    if isbar(px[cx, cy]):
        return im                                  # nothing recognisable

    left = cx
    while left > 0 and not isbar(px[left - 1, cy]):
        left -= 1
    right = cx
    while right < w - 1 and not isbar(px[right + 1, cy]):
        right += 1
    mid = (left + right) // 2
    top = cy
    while top > 0 and not isbar(px[mid, top - 1]):
        top -= 1
    bottom = cy
    while bottom < h - 1 and not isbar(px[mid, bottom + 1]):
        bottom += 1

    box = [left, top, right + 1, bottom + 1]
    if box[2] - box[0] < 64 or box[3] - box[1] < 64:
        return im

    # Chapters before the virtual screen exists draw straight to the back buffer,
    # so there is no letterbox to find and the scan runs to the edges. Fall back
    # to trimming the uniform border instead: whatever colour the corner is, the
    # figure is the bounding box of everything that is not it.
    if box[2] - box[0] > w - 8 and box[3] - box[1] > h - 8:
        from PIL import ImageChops
        flat = Image.new("RGB", im.size, px[0, 0])
        bb = ImageChops.difference(im, flat).convert("L").point(
            lambda v: 255 if v > 6 else 0).getbbox()
        if bb and (bb[2] - bb[0]) * (bb[3] - bb[1]) < w * h * 0.8:
            pad = 8
            return im.crop((max(0, bb[0] - pad), max(0, bb[1] - pad),
                            min(w, bb[2] + pad), min(h, bb[3] + pad)))
        return im

    # Snap to the exact virtual aspect: a one-pixel scan error would otherwise
    # show up as a slightly squashed figure.
    if aspect:
        cw = box[2] - box[0]
        want = int(round(cw / aspect))
        have = box[3] - box[1]
        if 0 < abs(want - have) <= 8:
            centre = (box[1] + box[3]) // 2
            box[1], box[3] = centre - want // 2, centre - want // 2 + want
    return im.crop((max(0, box[0]), max(0, box[1]), min(w, box[2]), min(h, box[3])))


def looks_blank(im):
    """True if the shot is one flat colour.

    An app photographed before its first frame gives a black rectangle, which
    crops to the whole screen and reports as a success. It is not one."""
    small = im.convert("RGB").resize((32, 32))
    px = list(small.getdata())
    first = px[0]
    return all(max(abs(c[i] - first[i]) for i in range(3)) < 8 for c in px)


def upright(im, ch):
    """Turn a quarter-turned figure back the right way up.

    The runner plays landscape inside a portrait-locked window by rotating its
    own presentation, so the device hands back a sideways picture. The book
    wants it the way the player holds the phone."""
    from PIL import Image
    return im.transpose(Image.ROTATE_90) if ch.get("turned") else im


# ----------------------------------------------------------------- android ---
def android_capture(ch, out):
    proj = f"{SRC}/{ch['id']}/Android/{ch['id']}.Android.csproj"
    r = run(["dotnet", "build", proj, "-c", "Release", "-t:Install", "--nologo"])
    if "Build succeeded" not in r.stdout:
        return "BUILD/INSTALL FAILED: " + " ".join(
            l for l in r.stdout.split("\n") if "error" in l)[:160]

    pkg = ch["appid"]
    act = run([ADB, "shell", "cmd", "package", "resolve-activity", "--brief", pkg])
    activity = act.stdout.strip().split("\n")[-1].strip()
    if "/" not in activity:
        return "NO LAUNCHER ACTIVITY"

    run([ADB, "shell", "am", "force-stop", pkg])
    run([ADB, "logcat", "-c"])
    run([ADB, "shell", "am", "start", "-n", activity])
    time.sleep(ch["shot"].get("wait", 7))

    raw = "/tmp/_cap_android.png"
    with open(raw, "wb") as f:
        p = subprocess.run([ADB, "exec-out", "screencap", "-p"], stdout=f)
    crash = run([ADB, "logcat", "-d", "-b", "crash"]).stdout
    if pkg in crash:
        return "CRASHED: " + next((l for l in crash.split("\n")
                                   if "Caused by" in l or "Exception" in l), "")[:150]

    shot = upright(crop_to_game(raw, ch.get('aspect')), ch)
    if looks_blank(shot):
        return "BLANK SCREEN - launched but had not drawn yet"
    shot.save(out)
    run([ADB, "shell", "am", "force-stop", pkg])
    return None


# --------------------------------------------------------------------- ios ---
def ios_capture(ch, out):
    proj = f"{SRC}/{ch['id']}/iOS/{ch['id']}.iOS.csproj"
    r = run(["dotnet", "build", proj, "-c", "Release",
             "-p:RuntimeIdentifier=iossimulator-arm64", "--nologo"])
    if "Build succeeded" not in r.stdout:
        return "BUILD FAILED: " + " ".join(
            l for l in r.stdout.split("\n") if "error" in l)[:160]

    app = f"{SRC}/{ch['id']}/iOS/bin/Release/net10.0-ios/iossimulator-arm64/{ch['id']}.iOS.app"
    if not os.path.isdir(app):
        return "NO .app PRODUCED"

    # Terminate every chapter app, not just this one: a stale app left in the
    # foreground is indistinguishable from a successful launch in a screenshot.
    for other in chapters.ALL:
        run(["xcrun", "simctl", "terminate", "booted", other["appid"]])
    run(["xcrun", "simctl", "spawn", "booted", "launchctl", "stop", "com.apple.springboard"])
    time.sleep(1)
    r = run(["xcrun", "simctl", "install", "booted", app])
    if r.returncode != 0:
        return "INSTALL FAILED: " + r.stderr.strip()[:150]
    r = run(["xcrun", "simctl", "launch", "booted", ch["appid"]])
    if r.returncode != 0:
        return "LAUNCH FAILED: " + r.stderr.strip()[:150]

    pid = r.stdout.strip().split(":")[-1].strip()
    time.sleep(ch["shot"].get("wait", 7))

    # Confirm the app is still alive; a crash on launch also leaves a pid behind.
    alive = run(["xcrun", "simctl", "spawn", "booted", "launchctl", "list"])
    if ch["appid"] not in alive.stdout:
        return f"NOT RUNNING after launch (pid {pid})"

    raw = "/tmp/_cap_ios.png"
    if os.path.exists(raw):
        os.remove(raw)
    r = run(["xcrun", "simctl", "io", "booted", "screenshot", "--type=png", raw])
    if not os.path.exists(raw):
        return "SCREENSHOT FAILED: " + r.stderr.strip()[:150]

    shot = upright(crop_to_game(raw, ch.get('aspect')), ch)
    if looks_blank(shot):
        return "BLANK SCREEN - launched but had not drawn yet"
    shot.save(out)
    run(["xcrun", "simctl", "terminate", "booted", ch["appid"]])
    return None


def main(only=None):
    os.makedirs(FIGS, exist_ok=True)
    failures = []
    for ch in chapters.ALL:
        if only and ch["id"] not in only:
            continue
        plat = platform_for(ch)
        out = os.path.join(FIGS, f"{ch['id']}.png")
        err = android_capture(ch, out) if plat == "android" else ios_capture(ch, out)
        if err:
            failures.append((ch["id"], plat, err))
            print(f"{ch['id']} {plat} FAIL {err}", flush=True)
        else:
            from PIL import Image
            print(f"{ch['id']} {plat} OK {Image.open(out).size}", flush=True)
    print(f"=== DONE: {len(failures)} failures ===", flush=True)
    for f in failures:
        print("  ", f, flush=True)


if __name__ == "__main__":
    main(sys.argv[1:] or None)
