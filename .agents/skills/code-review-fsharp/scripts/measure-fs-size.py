#!/usr/bin/env python3
"""Measure F# binding size (40 lines) and long lines (100 chars).

  python .agents/skills/code-review-fsharp/scripts/measure-fs-size.py --diff HEAD
  python .agents/skills/code-review-fsharp/scripts/measure-fs-size.py \\
    --fn src/Client/App.fs::runLoadServer \\
    --range src/Shared/ResidentProjection.fs:141-185 --usage captureLoadResponse
"""
from __future__ import annotations
import argparse, re, subprocess, sys
from pathlib import Path

MAX_FN, MAX_LINE = 40, 100
LET = re.compile(r"^(?:let|and)(?:\s+(?:rec|private|inline|mutable))*\s+(\S+)")

def read(p): return Path(p).read_text(encoding="utf-8").splitlines()
def ind(s): return len(s) - len(s.lstrip()) if s.strip() else 999
def nm(tok): return re.split(r"[:<\(\s,]", tok, maxsplit=1)[0]

def measure(path, label, a, b, added):
    n, lines = b - a, read(path)
    bad = n > MAX_FN
    print(f"{path}::{label}: lines {a+1}-{b} ({n} lines)" + (f"  ** OVER {MAX_FN}" if bad else ""))
    for i in range(a, b):
        if len(lines[i]) > MAX_LINE and (added is None or i + 1 in added):
            print(f"  LONG {i+1} ({len(lines[i])})"); bad = True
    return bad

def span_let(lines, name):
    for i, line in enumerate(lines):
        m = LET.match(line.lstrip())
        if m and nm(m.group(1)) == name:
            d = ind(line); j = i + 1
            while j < len(lines) and (not lines[j].strip() or ind(lines[j]) > d): j += 1
            return i, j

def parse_diff(ref):
    text = subprocess.check_output(
        ["git", "diff", ref, "--", "*.fs", "*.fsi"], text=True, encoding="utf-8", errors="replace")
    added, lets, path, new_ln = {}, [], None, 0
    for raw in text.splitlines():
        if raw.startswith("+++ b/"):
            path = raw[6:].replace("\\", "/"); added.setdefault(path, set())
        elif path and raw.startswith("@@"):
            m = re.search(r"\+(\d+)", raw); new_ln = int(m.group(1)) if m else 0
        elif path and raw.startswith("+") and not raw.startswith("+++"):
            added[path].add(new_ln); c = raw[1:]
            if ind(c) <= 4 and not path.startswith("tests/") and (m := LET.match(c.lstrip())):
                if "," not in c.lstrip().split("=", 1)[0]:
                    n = nm(m.group(1))
                    if n and (path, n) not in lets: lets.append((path, n))
            new_ln += 1
        elif path and not raw.startswith("-"): new_ln += 1
    return added, lets

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--diff", nargs="?", const="HEAD", default=None)
    p.add_argument("--fn", action="append", default=[])
    p.add_argument("--range", dest="ranges", action="append", default=[])
    p.add_argument("--usage", action="append", default=[])
    args = p.parse_args()
    if args.diff is None and not (args.fn or args.ranges or args.usage):
        args.diff = "HEAD"
    added, fns = {}, [s.split("::", 1) for s in args.fn]
    if args.diff is not None:
        added, discovered = parse_diff(args.diff)
        if not fns and not args.ranges: fns = discovered
    bad = False
    for path, name in fns:
        sp = span_let(read(path), name) if Path(path).is_file() else None
        if not sp: print(f"{path}::{name}: NOT FOUND"); bad = True; continue
        bad |= measure(path, name, *sp, added.get(path) if args.diff else None)
    for r in args.ranges:
        path, se = r.rsplit(":", 1); a, b = map(int, se.split("-", 1))
        bad |= measure(path, f"{a}-{b}", a - 1, b, None)
    if args.diff:
        for path, lns in sorted(added.items()):
            if not Path(path).is_file(): continue
            lines = read(path)
            for ln in sorted(lns):
                if 1 <= ln <= len(lines) and len(lines[ln - 1]) > MAX_LINE:
                    print(f"{path}:{ln} LONG ({len(lines[ln-1])})"); bad = True
    for sym in args.usage:
        r = subprocess.run(["rg", "-n", "--glob", "*.fs", "--glob", "*.fsi", sym, "src", "tests"],
                           capture_output=True, text=True)
        print(f"usage {sym}:\n" + (r.stdout.rstrip() or "(no matches)"))
    return 1 if bad else 0

if __name__ == "__main__":
    sys.exit(main())
