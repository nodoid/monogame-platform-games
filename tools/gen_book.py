"""Turns the chapter sources in book/src/*.txt into LibreOffice documents that
use the publisher's own paragraph styles.

Source format, one construct per line:

    # Title                 chapter title (Heading 1)
    ## Section              Heading 2
    ### Sub-section         Heading 3
    #### Sub-sub-section    Heading 4
    - item                  bullet
    -- item                 bullet within bullet
    1. item                 numbered bullet
    > text                  Tip [PACKT]
    ! text                  Information Box [PACKT]
    $ command               Command Line [PACKT]
    [fig] caption           Figure [PACKT]
    " quote                 Quote [PACKT]
    ```                     fenced code block, style Code [PACKT]
    (blank)                 paragraph break
    anything else           Normal [PACKT]

Inline: `code`, **bold**, *italics*.
"""
import os
import re
import sys
import zipfile

import odt

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SRC = os.path.join(ROOT, "book", "src")
OUT = os.path.join(ROOT, "book")
FIGS = os.path.join(ROOT, "book", "figures")
TEMPLATE = os.path.expanduser(
    "~/Downloads/New_Template_Normal_Preface_OT_Mini.dot")


# The template puts the page number in a frame 0.51cm wide, which fits two
# digits and clips the third: page 376 prints as "37". It was cut from a document
# under a hundred pages long, so nothing there ever showed it. The frame's right
# edge is pinned to the page content, so widening it grows leftwards and the
# number stays exactly where it was.
NARROW_PAGE_NUMBER = 'svg:width="0.51cm" svg:height="0.328cm"'
WIDE_PAGE_NUMBER = 'svg:width="1.6cm" svg:height="0.328cm"'


def fix_template(styles):
    """Repairs of the converted template that every output needs."""
    return styles.replace(NARROW_PAGE_NUMBER, WIDE_PAGE_NUMBER, 1)


def template_styles():
    """styles.xml straight from the publisher's Word template, converted once."""
    cache = os.path.join(ROOT, "book", ".styles.xml")
    if os.path.exists(cache):
        return fix_template(open(cache, encoding="utf-8").read())

    import subprocess, tempfile
    tmp = tempfile.mkdtemp()
    subprocess.run(["soffice", "--headless", "--convert-to", "odt", "--outdir", tmp, TEMPLATE],
                   check=True, capture_output=True)
    converted = os.path.join(tmp, os.path.splitext(os.path.basename(TEMPLATE))[0] + ".odt")
    styles = zipfile.ZipFile(converted).read("styles.xml").decode("utf-8")
    os.makedirs(os.path.dirname(cache), exist_ok=True)
    open(cache, "w", encoding="utf-8").write(styles)
    return fix_template(styles)


def convert(text, figures=None):
    """Source text -> ODF body XML.

    A `[fig]` line becomes the chapter's screenshot followed by its caption,
    when `book/figures/chNN.png` exists; otherwise it stays a caption on its own
    so the manuscript still reads."""
    out = []
    lines = text.split("\n")
    i = 0
    pending_bullets, pending_kind = [], None

    def flush_bullets():
        nonlocal pending_bullets, pending_kind
        if pending_bullets:
            out.append(odt.bullets(pending_bullets, pending_kind))
            pending_bullets, pending_kind = [], None

    while i < len(lines):
        line = lines[i].rstrip()
        stripped = line.strip()

        if stripped.startswith("```"):
            flush_bullets()
            i += 1
            block = []
            while i < len(lines) and not lines[i].strip().startswith("```"):
                block.append(lines[i])
                i += 1
            i += 1
            if block:
                out.append(odt.code_block(block))
            continue

        if not stripped:
            flush_bullets()
            i += 1
            continue

        m = re.match(r"^(#{1,4})\s+(.*)$", stripped)
        if m:
            flush_bullets()
            out.append(odt.para("h" + str(len(m.group(1))), m.group(2)))
            i += 1
            continue

        if stripped.startswith("-- "):
            if pending_kind != "bullet2":
                flush_bullets()
            pending_kind = "bullet2"
            pending_bullets.append(stripped[3:])
            i += 1
            continue

        if stripped.startswith("- "):
            if pending_kind != "bullet":
                flush_bullets()
            pending_kind = "bullet"
            pending_bullets.append(stripped[2:])
            i += 1
            continue

        m = re.match(r"^\d+\.\s+(.*)$", stripped)
        if m:
            if pending_kind != "number":
                flush_bullets()
            pending_kind = "number"
            pending_bullets.append(m.group(1))
            i += 1
            continue

        flush_bullets()

        if stripped.startswith("> "):
            out.append(odt.para("tip", stripped[2:]))
        elif stripped.startswith("! "):
            out.append(odt.para("info", stripped[2:]))
        elif stripped.startswith("$ "):
            out.append(odt.para("command", stripped[2:]))
        elif stripped.startswith('" '):
            out.append(odt.para("quote", stripped[2:]))
        elif stripped.startswith("[fig] "):
            if figures:
                name, size = figures.pop(0) if figures else (None, None)
                if name:
                    out.append(odt.image(name, size))
            out.append(odt.para("figure", stripped[6:]))
        else:
            out.append(odt.para("normal", stripped))
        i += 1

    flush_bullets()
    return "".join(out)


def estimate_pages(text):
    """A Packt page holds roughly 325 words of body text, or 34 lines of code.
    Calibrated against LibreOffice's own pagination - run with --measure to
    re-check it, which converts every chapter to PDF and counts for real."""
    words, code_lines = 0, 0
    in_code = False
    for line in text.split("\n"):
        if line.strip().startswith("```"):
            in_code = not in_code
            continue
        if in_code:
            code_lines += 1
        else:
            words += len(line.split())
    return words / 310.0 + code_lines / 32.0


def build_one(path, styles):
    name = os.path.splitext(os.path.basename(path))[0]
    text = open(path, encoding="utf-8").read()
    title = next((l[2:].strip() for l in text.split("\n") if l.startswith("# ")), name)

    shot = os.path.join(FIGS, name + ".png")
    images, queue = {}, []
    if os.path.exists(shot):
        from PIL import Image
        images[name + ".png"] = shot
        queue = [(name + ".png", Image.open(shot).size)]

    body = convert(text, list(queue))
    target = os.path.join(OUT, name + ".odt")
    odt.write_odt(target, body, styles, title,
                  "Building Two Classic Platform Games", images=images)
    # A figure occupies about a third of a page; count it.
    return target, estimate_pages(text) + 0.35 * len(queue)


def measure_pages(paths):
    """Ground truth: let LibreOffice paginate, then count /Type /Page in the PDF."""
    import subprocess, tempfile, re as _re
    tmp = tempfile.mkdtemp()
    subprocess.run(["soffice", "--headless", "--convert-to", "pdf", "--outdir", tmp] + paths,
                   check=False, capture_output=True)
    counts = {}
    for p in paths:
        pdf = os.path.join(tmp, os.path.splitext(os.path.basename(p))[0] + ".pdf")
        if not os.path.exists(pdf):
            counts[os.path.basename(p)] = -1
            continue
        data = open(pdf, "rb").read()
        counts[os.path.basename(p)] = len(_re.findall(rb"/Type\s*/Page[^s]", data))
    return counts


def main(pattern=None):
    styles = template_styles()
    os.makedirs(OUT, exist_ok=True)
    results = []
    for name in sorted(os.listdir(SRC)):
        if not name.endswith(".txt"):
            continue
        if pattern and pattern not in name:
            continue
        target, pages = build_one(os.path.join(SRC, name), styles)
        results.append((os.path.basename(target), pages))
    measured = {}
    if "--measure" in sys.argv:
        measured = measure_pages([os.path.join(OUT, n) for n, _ in results])
    for name, pages in results:
        real = measured.get(name)
        shown = real if real else pages
        flag = "" if 9 <= shown <= 20 else "  <-- OUT OF RANGE"
        extra = f"  (estimate {pages:.1f})" if real else ""
        print(f"{name:16s} {shown:5.1f} pages{extra}{flag}")
    return results


if __name__ == "__main__":
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    main(args[0] if args else None)
