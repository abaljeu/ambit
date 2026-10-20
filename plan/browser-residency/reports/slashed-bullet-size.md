# Code review — slashed bullet size

- Range: `git diff HEAD`
- Spec: [browser residency spec](../spec.md)
- Arch: [browser residency architecture](../arch.md)

## 1. Standards

1. **No findings.** The slash rule overrides size and the slash fill. The shared `::before` rule still holds the common box.
2. **Scan note** — [standards scan](../../../.agents/skills/code-review/scripts/standards-scan.py) printed no lines.

## 2. Spec

1. **No findings.** The slashed bullet is larger than the hollow bullet, the row height stays the same, and the vertical center stays the same.

## 3. Arch

1. **No findings.** [browser residency architecture](../arch.md) §11 Slashed Unparsed Bullet still paints `amb-bullet-hollow-slash`.

## 4. Totals

- Standards: 0 findings.
- Spec: 0 findings.
- Arch: 0 findings.
