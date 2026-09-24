#!/usr/bin/env python3
"""Mechanical Standards-axis scan. Not smells.

  python .agents/skills/code-review/scripts/standards-scan.py
  python .agents/skills/code-review/scripts/standards-scan.py --diff HEAD
  python .agents/skills/code-review/scripts/standards-scan.py --diff origin/ready

Default: git diff HEAD plus untracked. Named ref: git diff REF...HEAD.
Invokes measure-fs-size.py when *.fs/*.fsi are in range (no duplicated logic).
measure-fs-size still uses two-dot --diff REF; this script uses three-dot for
named refs. Nested modules are not scanned (unreliable from a hunk).
"""
from __future__ import annotations
import argparse, os, re, subprocess, sys
from pathlib import Path

MAX_LINE, MAX_FILE = 100, 800
FS = {".fs", ".fsi"}
SRC = FS | {".fsx", ".cs", ".js", ".ts", ".tsx", ".jsx", ".py", ".sh"}
SKIP = ("/bin/", "/obj/", "/node_modules/", "/.git/", "/packages/")
FSHARP = ".agents/rules/fsharp-source.md"
REFER = ".agents/rules/refer-by-name.md"
MARK = ".agents/rules/markdown-writing.md"
BARE = re.compile(r"(?i)\b(?:keep|ticket|issue)\s+#?(\d+)\b")
HASH = re.compile(r"(?<![#\w])#(\d+)\b")
ITEMS = re.compile(r"(?i)\b(?:user\s+)?items?\s+\d+(?:\s*[–—-]\s*\d+)?\b")
FS_SIZE = (
    Path(__file__).resolve().parents[2]
    / "code-review-fsharp" / "scripts" / "measure-fs-size.py"
)


def git(*args):
    return subprocess.check_output(
        ["git", *args], text=True, encoding="utf-8", errors="replace")


def quote(s, n=72):
    s = s.replace("\u2013", "-").replace("\u2014", "-")
    s = re.sub(r"\s+", " ", s.strip())
    return s if len(s) <= n else s[: n - 3] + "..."


def skip(p):
    q = "/" + p.replace("\\", "/").lower()
    return any(s in q for s in SKIP)


def ext(p):
    return Path(p).suffix.lower()


def emit(path, loc, rule, kind, text):
    where = f"{path}:{loc}" if loc else path
    print(f"{where}  {rule}  {kind}  {quote(text)}")


def parse_diff(text):
    added, path, new_ln = {}, None, 0
    for raw in text.splitlines():
        if raw.startswith("+++ b/"):
            path = raw[6:].replace("\\", "/")
            if path == "/dev/null":
                path = None
            else:
                added.setdefault(path, set())
        elif path and raw.startswith("@@"):
            m = re.search(r"\+(\d+)", raw)
            new_ln = int(m.group(1)) if m else 0
        elif path and raw.startswith("+") and not raw.startswith("+++"):
            added[path].add(new_ln)
            new_ln += 1
        elif path and not raw.startswith("-"):
            new_ln += 1
    return added


def untracked():
    out = []
    for line in git("status", "--porcelain", "-u").splitlines():
        if line.startswith("?? "):
            out.append(line[3:].strip().strip('"').replace("\\", "/"))
    return out


def read_lines(path):
    p = Path(path)
    if not p.is_file():
        return None
    try:
        data = p.read_text(encoding="utf-8")
    except OSError:
        return None
    if "\0" in data[:4096]:
        return None
    return data.splitlines()


def old_count(base, path):
    r = subprocess.run(
        ["git", "show", f"{base}:{path}"],
        text=True, encoding="utf-8", errors="replace",
        stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
    if r.returncode != 0:
        return 0
    return len(r.stdout.splitlines())


def named(num, line):
    if re.search(rf"{num}\s+[—–-]\s+\S", line):
        return True
    for m in re.finditer(r"\[\[[^|\]]+\|([^\]]+)\]\]", line):
        disp = m.group(1)
        if num in disp and re.search(r"[A-Za-z]{3,}", disp):
            return True
    return False


def refer_hits(line):
    hits = []
    for m in BARE.finditer(line):
        if not named(m.group(1), line):
            hits.append(m.group(0))
    for m in HASH.finditer(line):
        if not named(m.group(1), line):
            hits.append(m.group(0))
    for m in ITEMS.finditer(line):
        hits.append(m.group(0))
    return hits


def scan_added(path, lines, added, untracked_fs, bad):
    is_fs = ext(path) in FS
    is_src = ext(path) in SRC
    is_plan = path.startswith("plan/") and path.endswith(".md")
    for ln in sorted(added):
        if ln < 1 or ln > len(lines):
            continue
        line = lines[ln - 1]
        if is_src and (not is_fs or untracked_fs):
            if len(line) > MAX_LINE:
                emit(path, ln, FSHARP, f"LONG ({len(line)})", line)
                bad = True
        if is_fs and "\t" in line:
            emit(path, ln, FSHARP, "TAB", line)
            bad = True
        if is_fs and re.search(r"\bmutable\b", line.split("//")[0]):
            emit(path, ln, FSHARP, "MUTABLE", line)
            bad = True
        if is_plan:
            for hit in refer_hits(line):
                emit(path, ln, REFER, "BARE_ID", hit)
                bad = True
            if line.strip() == "" and ln > 1 and lines[ln - 2].strip() == "":
                emit(path, ln, MARK, "BLANK_BLANK", "(consecutive blank)")
                bad = True
    return bad


def file_growth(path, old, new, bad):
    # File limit is MAX_FILE in fsharp-source. That rule does not apply to tests.
    # A file at or under the limit is not a hit, even when this change grew it.
    # An already-over file is a hit only when this change increased it.
    if ext(path) not in FS or path.startswith("tests/"):
        return bad
    if new > MAX_FILE and new > old:
        emit(
            path, None, FSHARP, f"FILE {old}->{new}",
            f"exceeds {MAX_FILE} lines and this change increased it")
        return True
    return bad


def measure_fs(ref):
    r = subprocess.run(
        [sys.executable, str(FS_SIZE), "--diff", ref],
        text=True, encoding="utf-8", errors="replace",
        capture_output=True)
    out = (r.stdout or "").rstrip()
    if not out and r.returncode:
        out = (r.stderr or "").rstrip()
    if out:
        print("--- measure-fs-size ---")
        print(out)
    return r.returncode != 0


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--diff", nargs="?", const="HEAD", default="HEAD")
    args = p.parse_args()
    os.chdir(git("rev-parse", "--show-toplevel").strip())
    ref = args.diff
    if ref == "HEAD":
        diff = git("diff", "--no-color", "HEAD")
        base = "HEAD"
        extra = untracked()
    else:
        diff = git("diff", "--no-color", f"{ref}...HEAD")
        base = git("merge-base", ref, "HEAD").strip()
        extra = []
    added = parse_diff(diff)
    for path in extra:
        if skip(path) or path in added:
            continue
        lines = read_lines(path)
        if lines is None:
            continue
        added[path] = set(range(1, len(lines) + 1))
    bad = False
    fs_paths = []
    for path, lns in sorted(added.items()):
        if skip(path):
            continue
        lines = read_lines(path)
        if lines is None:
            continue
        if ext(path) in FS:
            fs_paths.append(path)
        is_untracked = path in extra
        bad = scan_added(path, lines, lns, is_untracked, bad)
        bad = file_growth(path, old_count(base, path), len(lines), bad)
    if fs_paths:
        bad = measure_fs(ref) or bad
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
