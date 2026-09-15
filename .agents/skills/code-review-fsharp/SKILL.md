---
name: code-review-fsharp
description: F# binding-size and long-line measurer invoked by code-review standards-scan. Use directly only for --fn, --range, or --usage on *.fs / *.fsi.
---

Companion measurer for [[.agents/skills/code-review/SKILL.md]]. The parent invokes it via [[.agents/skills/code-review/scripts/standards-scan.py]]; use this skill directly only for `--fn`, `--range`, or `--usage`.

## Size check

This wrapper is the F# size implementation that [[.agents/skills/code-review/scripts/standards-scan.py]] invokes. For optional narrowing:

```bash
.agents/skills/code-review-fsharp/scripts/measure-fs-size.sh --diff HEAD
```

If a fixed point was named, pass that ref instead of `HEAD`.

Thresholds match [[.agents/rules/fsharp-source.md]]: **40 lines/function**, **100 chars/line**. Long lines are reported only on **added** hunk lines. Over-limit bindings and added long lines are documented-standard findings citing [[.agents/rules/fsharp-source.md]].

Do **not** measure match arms separately — they are sub-parts of a function, and the enclosing `let`/`and` must already be ≤40 lines. The script finds module-level `let`/`and` via indentation (no `--arm`).

### Optional narrowing

`--fn`, `--range path:start-end`, and `--usage`:

```bash
.agents/skills/code-review-fsharp/scripts/measure-fs-size.sh \
  --fn 'src/Client/App.fs::runLoadServer' \
  --usage captureLoadResponse
```

```bash
.agents/skills/code-review-fsharp/scripts/measure-fs-size.sh \
  --range src/Shared/ResidentProjection.fs:141-185
```

`--help` lists all flags.
