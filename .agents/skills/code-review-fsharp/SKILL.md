---
name: code-review-fsharp
description: F#-specific helpers for the code-review skill — measure binding size and long lines against fsharp-source thresholds. Use when a code-review diff touches *.fs / *.fsi.
---

Companion to [[.agents/skills/code-review/SKILL.md]]. Run when the review range touches F# (`*.fs` / `*.fsi`).

## Size check

Before the Standards sub-agent runs, measure against the same review range:

```bash
python .agents/skills/code-review-fsharp/scripts/measure-fs-size.py --diff HEAD
```

If a fixed point was named, pass that ref instead of `HEAD`.

Thresholds match [[.cursor/rules/fsharp-source.mdc]]: **40 lines/function**, **100 chars/line**. Long lines are reported only on **added** hunk lines. Paste the script output into the Standards sub-agent prompt; treat over-limit bindings and added long lines as documented-standard findings citing `fsharp-source.mdc`.

Do **not** measure match arms separately — they are sub-parts of a function, and the enclosing `let`/`and` must already be ≤40 lines. The script finds module-level `let`/`and` via indentation (no `--arm`).

### Optional narrowing

`--fn`, `--range path:start-end`, and `--usage`:

```bash
python .agents/skills/code-review-fsharp/scripts/measure-fs-size.py \
  --fn 'src/Client/App.fs::runLoadServer' \
  --usage captureLoadResponse
```

```bash
python .agents/skills/code-review-fsharp/scripts/measure-fs-size.py \
  --range src/Shared/ResidentProjection.fs:141-185
```

`--help` lists all flags.
