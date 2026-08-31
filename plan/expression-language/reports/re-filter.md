# `re` and `rei` Header filters

Implemented two catalog after-filters on Header text (`node.text`), the same field as `containing`. `containing` is unchanged.

| Word | Case | Slot | Miss |
| --- | --- | --- | --- |
| `re` | sensitive (`RegexOptions.None`) | quoted pattern | invalid pattern, or no match |
| `rei` | insensitive (`RegexOptions.IgnoreCase` / JS `i`) | quoted pattern | invalid pattern, or no match |

`x re ".*blue.*"` equals `x containing "blue"` when the Header has that same-case substring. `rei` is the case-insensitive twin. There is no third flag syntax and no inline `(?i)`.

## Code

- Catalog: [[src/Shared/ExprPrimitive.fs]]
- Match: [[src/Shared/ExprWalk.fs]] `re` / `rei`
- Parse slot: [[src/Shared/ExprParse.fs]] `wordWantsTrailingLiteral`
- Spec rows: [[plan/expression-language/spec.md]] chapter 7 (surgical insert next to `containing`; `outer` grammar and combinator rows not touched)
- Issue: [[plan/expression-language/issues/29-re-and-rei-header-filters.md]]

## Verify

- Shared tests: `ExprFilterTests` and `ExprPathClusterParseTests` — 23 passed
- Client compile gate: `bash ./scripts/client.sh build` — Fable and esbuild succeeded

## Engine

Shared already uses `System.Text.RegularExpressions`. Fable maps that type to JS `RegExp`. Case for `rei` is an engine flag, not `(?i)`. An invalid pattern throws in both engines; the walk catches it and yields no Answers.

## Board mutations

- `add` [[plan/expression-language/reports/re-filter.md]] — HITL: Run `= … re "…"` and `= … rei "…"` on `/ambit` or `/ambit?debug=1`; confirm Header match, case split, and invalid pattern as no matches
- do not `remove` the `outer` spec-lock pending item
