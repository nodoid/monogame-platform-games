"""Builds every chapter for both platforms and reports the failures.

Ninety-two builds. Run it after regenerating the projects without --capture:
that is the code the reader gets, and it is the only version worth proving."""
import os
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chapters

SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "src")


def build(job):
    ch, plat = job
    if plat == "android":
        proj = f"{SRC}/{ch['id']}/Android/{ch['id']}.Android.csproj"
        args = []
    else:
        proj = f"{SRC}/{ch['id']}/iOS/{ch['id']}.iOS.csproj"
        args = ["-p:RuntimeIdentifier=iossimulator-arm64"]
    r = subprocess.run(["dotnet", "build", proj, "-c", "Release", "--nologo"] + args,
                       capture_output=True, text=True)
    ok = "Build succeeded" in r.stdout
    err = "" if ok else " | ".join(
        l.strip() for l in r.stdout.split("\n") if ": error" in l)[:200]
    return ch["id"], plat, ok, err


def main(only=None):
    jobs = [(ch, p) for ch in chapters.ALL for p in ("android", "ios")
            if not only or ch["id"] in only]
    fails = []
    with ThreadPoolExecutor(max_workers=4) as pool:
        for cid, plat, ok, err in pool.map(build, jobs):
            print(f"{cid} {plat} {'OK' if ok else 'FAIL ' + err}", flush=True)
            if not ok:
                fails.append((cid, plat, err))
    print(f"=== {len(jobs) - len(fails)}/{len(jobs)} built, {len(fails)} failed ===",
          flush=True)
    for f in fails:
        print("  ", f, flush=True)
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:] or None))
