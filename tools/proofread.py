"""Proofreads the book's chapter sources.

Three passes, because they catch different things:

  spelling   British English, on prose only - code fences, inline code, command
             lines and figure captions are stripped out first, so `Color` and
             `initialize` in C# are never flagged while "colour" in a sentence is.
  spelling   American spellings that survived into prose.
  phrasing   Constructions that read as machine-written, and a few habits worth
             breaking: filler openers, hedges, and the "not just X, but Y" tic.
"""
import os
import re
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "book", "src")

# ---------------------------------------------------------------- prose ------
def prose_lines(text):
    """Yield (line_no, prose) with everything that is not prose removed."""
    out, in_fence = [], False
    for i, raw in enumerate(text.split("\n"), 1):
        s = raw.rstrip()
        if s.startswith("```"):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        if s[:1] in ("$", "["):        # shell commands, figure captions
            continue
        s = re.sub(r"`[^`]*`", " ", s)          # inline code
        s = re.sub(r"^[#>!\-\d.\"]+\s*", "", s)  # markers
        if s.strip():
            out.append((i, s))
    return out


# --------------------------------------------------------- americanisms ------
US_UK = {
    "color": "colour", "colors": "colours", "colored": "coloured",
    "behavior": "behaviour", "behaviors": "behaviours",
    "center": "centre", "centers": "centres", "centered": "centred",
    "initialize": "initialise", "initialized": "initialised",
    "initializing": "initialising", "initialization": "initialisation",
    "organize": "organise", "organized": "organised",
    "recognize": "recognise", "recognized": "recognised",
    "optimize": "optimise", "optimized": "optimised", "optimizing": "optimising",
    "normalize": "normalise", "normalized": "normalised",
    "synchronize": "synchronise", "synchronized": "synchronised",
    "analyze": "analyse", "analyzed": "analysed",
    "prioritize": "prioritise", "summarize": "summarise",
    "gray": "grey", "grays": "greys",
    "traveled": "travelled", "traveling": "travelling",
    "canceled": "cancelled", "canceling": "cancelling",
    "modeling": "modelling", "labeled": "labelled", "labeling": "labelling",
    "signaled": "signalled", "totaled": "totalled", "fueled": "fuelled",
    "defense": "defence", "offense": "offence", "license": "licence (noun)",
    "practicing": "practising", "meters": "metres", "liter": "litre",
    "artifact": "artefact", "artifacts": "artefacts",
    "catalog": "catalogue", "dialog": "dialogue", "maneuver": "manoeuvre",
    "favorite": "favourite", "flavor": "flavour", "honor": "honour",
    "neighbor": "neighbour", "neighboring": "neighbouring",
    "toward": "towards", "afterward": "afterwards",
    "aluminum": "aluminium", "skeptical": "sceptical", "program": "programme (unless a computer program)",
}

# ------------------------------------------------------------- phrasing ------
PHRASES = [
    (r"\bdelve\b", "delve"),
    (r"\bleverag(e|es|ing|ed)\b", "leverage"),
    (r"\bseamless(ly)?\b", "seamless"),
    (r"\brobust\b", "robust"),
    (r"\bcutting[- ]edge\b", "cutting-edge"),
    (r"\bstate[- ]of[- ]the[- ]art\b", "state-of-the-art"),
    (r"\bgame[- ]chang(er|ing)\b", "game-changer"),
    (r"\ba testament to\b", "a testament to"),
    (r"\btapestry\b", "tapestry"),
    (r"\bin the realm of\b", "in the realm of"),
    (r"\bnavigat(e|ing) the\b", "navigating the"),
    (r"\bunlock(s|ing)? the\b", "unlock the"),
    (r"\bharness(es|ing)?\b", "harness"),
    (r"\belevat(e|es|ing) (your|the)\b", "elevate the"),
    (r"\bdive (deep )?into\b", "dive into"),
    (r"\blet's (explore|take a look|dive)\b", "let's explore"),
    (r"\bit('s| is) (worth|important to) not(e|ing)\b", "it is worth noting"),
    (r"\bit('s| is) (crucial|vital|essential) (to|that)\b", "it is crucial to"),
    (r"\bfurthermore\b", "furthermore"),
    (r"\bmoreover\b", "moreover"),
    (r"\bin conclusion\b", "in conclusion"),
    (r"\bat the end of the day\b", "at the end of the day"),
    (r"\bwhen it comes to\b", "when it comes to"),
    (r"\bin today's\b", "in today's"),
    (r"\bever[- ]evolving\b", "ever-evolving"),
    (r"\bbest practices\b", "best practices"),
    (r"\bcomprehensive\b", "comprehensive"),
    (r"\bmyriad\b", "myriad"),
    (r"\bplethora\b", "plethora"),
    (r"\bis not just .{1,40}, (it|but) (is|it's)\b", "not just X, but Y"),
    (r"\bisn't just .{1,40}, (it|but)\b", "isn't just X, but Y"),
    (r"\bmore than just\b", "more than just"),
    (r"\bthe key (is|to)\b", "the key is"),
    (r"\bwe('ll| will) (explore|examine|look at)\b", "we will explore"),
    (r"\bas we('ve| have) seen\b", "as we have seen"),
    (r"\bin this (section|chapter), we\b", "in this chapter, we"),
    (r"\bremember(,| that)\b", "remember,"),
    (r"\bsimply put\b", "simply put"),
    (r"\bneedless to say\b", "needless to say"),
]


def spell(files, personal):
    cmd = ["aspell", "-d", "en_GB-ise", "--encoding=utf-8", "list",
           "--personal=" + personal, "--run-together"]
    counts = {}
    for p in files:
        text = "\n".join(l for _, l in prose_lines(open(p).read()))
        r = subprocess.run(cmd, input=text, capture_output=True, text=True)
        for w in r.stdout.split():
            counts.setdefault(w, []).append(os.path.basename(p))
    return counts


def main():
    files = sorted(f for f in
                   (os.path.join(SRC, n) for n in os.listdir(SRC))
                   if f.endswith(".txt"))
    personal = os.path.join(os.path.dirname(os.path.abspath(__file__)), "wordlist.txt")

    print("=== spelling (en_GB, prose only) ===")
    counts = spell(files, personal)
    for w in sorted(counts, key=lambda w: (-len(counts[w]), w)):
        where = counts[w]
        print(f"  {w:24s} {len(where):3d}  {', '.join(sorted(set(where))[:4])}")
    print(f"  ({len(counts)} distinct)")

    print("\n=== american spellings in prose ===")
    n = 0
    for p in files:
        for ln, line in prose_lines(open(p).read()):
            for m in re.finditer(r"[A-Za-z']+", line):
                w = m.group().lower()
                if w in US_UK:
                    print(f"  {os.path.basename(p)}:{ln}  {m.group()} -> {US_UK[w]}")
                    n += 1
    print(f"  ({n} found)")

    print("\n=== phrasing ===")
    n = 0
    for p in files:
        for ln, line in prose_lines(open(p).read()):
            for pat, label in PHRASES:
                if re.search(pat, line, re.I):
                    print(f"  {os.path.basename(p)}:{ln}  [{label}]  {line.strip()[:110]}")
                    n += 1
    print(f"  ({n} found)")


if __name__ == "__main__":
    main()
