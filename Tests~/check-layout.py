#!/usr/bin/env python3
"""Geometry check for CasinoLayout profiles: no zone off-canvas, no two zones
overlapping. Parses the C# directly so the profiles stay the single source of
truth. Run from the repo root:  python3 Tests~/check-layout.py

Catches what a screenshot at one breakpoint cannot: this found the action rail
sitting on top of the version stamp in both landscape profiles, a collision that
predated the profile system and was invisible at 1280x720 because the stamp is
only 6 units into the button."""
import re, sys, itertools, pathlib

SRC = pathlib.Path(__file__).resolve().parent.parent / "Assets/Scripts/CasinoLayout.cs"
# Table and Hint deliberately share horizontal band with the draw pile column.
ALLOWED = ({"Table", "DrawPile"}, {"Hint", "DrawPile"})

def profile(src, name):
    blk = src.split(f"public static readonly Profile {name} = new()", 1)[1].split("};", 1)[0]
    ref = tuple(float(x) for x in re.search(r"Reference = new Vector2\(([\d.]+), ([\d.]+)\)", blk).groups())
    zones = {m[0]: tuple(float(v) for v in m[1:]) for m in re.findall(
        r"(\w+)\s*=\s*new Zone\(([-\d.f]+)f?,\s*([-\d.f]+)f?,\s*([-\d.]+),\s*([-\d.]+),\s*([-\d.]+),\s*([-\d.]+)\)",
        blk.replace('f,', ','))}
    a = {k: tuple(float(x) for x in re.search(k + r"\s*=\s*new Vector2\(([-\d.]+)f?,\s*([-\d.]+)f?\)", blk).groups())
         for k in ("ActionAnchor", "ActionFirst", "ActionStep", "ActionSize")}
    for i, nm in enumerate(["Sweep", "Trail", "Build", "Suggest"]):
        zones[nm] = (a["ActionAnchor"][0], a["ActionAnchor"][1],
                     a["ActionFirst"][0] + a["ActionStep"][0] * i,
                     a["ActionFirst"][1] + a["ActionStep"][1] * i,
                     a["ActionSize"][0], a["ActionSize"][1])
    return ref, zones

def rect(z, W, H):
    ax, ay, px, py, w, h = z
    l, b = ax * W + px - ax * w, ay * H + py - ay * h
    return l, b, l + w, b + h

def main():
    src, ok = SRC.read_text(), True
    for name in ("Wide", "Compact", "Portrait"):
        (W, H), zones = profile(src, name)
        rects = {k: rect(v, W, H) for k, v in zones.items()}
        problems = []
        for k, r in rects.items():
            if r[0] < -1 or r[1] < -1 or r[2] > W + 1 or r[3] > H + 1:
                problems.append(f"off-canvas {k} {tuple(round(x) for x in r)}")
        for x, y in itertools.combinations(sorted(rects), 2):
            if "GameOver" in (x, y) or {x, y} in ALLOWED:
                continue
            ox = min(rects[x][2], rects[y][2]) - max(rects[x][0], rects[y][0])
            oy = min(rects[x][3], rects[y][3]) - max(rects[x][1], rects[y][1])
            if ox > 1 and oy > 1:
                problems.append(f"overlap {x} x {y} by {ox:.0f}x{oy:.0f}")
        print(f"{name:<9} {W:.0f}x{H:.0f}  {'OK' if not problems else 'FAIL'}")
        for p in problems:
            print("   " + p); ok = False
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main())
