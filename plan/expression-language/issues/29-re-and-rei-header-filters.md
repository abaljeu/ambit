# 29 — `re` and `rei` Header regex filters

**Context:** Pure after-filters on a Node, same family as `containing`. `re` is case-sensitive. `rei` is case-insensitive via engine flags. Locked 2026-08-29. Work on branch `w/tree2-semantics`. Do not change `containing`. Do not implement `outer`.

**What to build:** Catalog rows `re` and `rei` with a required quoted pattern. Match Header text (`node.text`), the same field as `containing`. Engine is `System.Text.RegularExpressions` in Shared for .NET and Fable. `rei` uses `RegexOptions.IgnoreCase` (.NET) / the `i` flag (Fable/JS). An invalid pattern is a miss. Bare `re` and `rei` are missing-argument parse errors.

**Blocked by:** none.

**See also:** [[.scratch/expression-language/spec.md]] chapter 7 `re` and `rei` rows; [[.scratch/expression-language/reports/re-filter.md]]; [[src/Shared/ExprWalk.fs]]; [[src/Shared/ExprPrimitive.fs]]; [[tests/Shared.Tests/ExprFilterTests.fs]].

**Status:** done

- [x] `x re ".*blue.*"` keeps the Node when Header text matches, same field as `containing "blue"`, case-sensitive.
- [x] `rei` keeps the same Node when only case differs; `re` does not.
- [x] Bare `re` and `rei` are missing-argument parse errors.
- [x] An invalid pattern yields no Answers (a miss).

## Comments

Engine: Shared already uses `System.Text.RegularExpressions` ([[src/Shared/ExprParse.fs]], [[src/Shared/CssClass.fs]], [[src/Shared/OpenTarget.fs]]). Fable maps `Regex` to JS `RegExp` and `RegexOptions.IgnoreCase` to the `i` flag. Inline `(?i)` is not the case switch; JS typically does not honor it.

.NET `IgnoreCase` is culture-aware; JS `i` is Unicode-aware in current browsers. ASCII letters agree. .NET-only constructs (balancing groups, some lookbehind forms) are not in the common subset; those patterns miss or differ. `Regex.IsMatch` is a partial match on both engines, so `.*` wrapping is optional for a substring.

Invalid pattern: the BCL constructor throws. The walk catches that at the boundary and yields the empty sequence, the same miss as a wrong slot type (`requireQuoted` → empty). No new error architecture.
