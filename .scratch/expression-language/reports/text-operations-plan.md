# Text operations plan

Planning only. No product F#. [[../spec.md]] is not edited (sibling [[../issues/28-outer-prefix-combinator.md]] owns OUTER grammar). Issues: [[../issues/30-text-operations.md]] (text ops) and [[../issues/31-if-pullback.md]] (`IF`, separate).

## Issue 30 — text operations

Node→Text coercion: if a term wants Text and the input is a Node, apply `text` then the term. Already Text: no coerce. Else miss. Coerce uses `text`, never `name`.

| Word | Role |
| --- | --- |
| `text` | Node ⇒ Text. Assumption: Header text field `node.text` (not the Name; Header in [[CONTEXT.md]] includes both). |
| `name` | Node ⇒ Text. The Name. `name right 4` is Name then suffix. |
| `left N` / `right N` | Text ⇒ Text. Prefix / suffix of length N. Number slot (amend “number only after `:` / `!`”). |
| `is` | Text ⇒ Text equality filter. Word `is`, not `=`. Quoted literal this slice. |

Redefine `containing`, `re`, `rei` as `Text ⇒ Text`. On a Node they coerce through `text`, so `node containing "blue"` still tests Header text, but Answers are Text. Breaking vs today's `Node ⇒ Node` Header filters ([[../issues/29-re-and-rei-header-filters.md]]). Search/Move then reject bare `containing "…"` unless a pullback wraps it. `outer containing "blue"` still yields Nodes (emptiness test on the walk).

Without pullback, `nodes left 5 is "rapid"` and `nodes name right 4 is ".txt"` yield Text, not Nodes. Do not spec `IF` in 30.

HITL: confirm `text` = `node.text`; empty Name = empty Text; `is` Text-only this slice; `left`/`right` when N is too large or less than 1.

## Issue 31 — `IF` pullback

Separate combinator. Yield the input when the operand is nonempty. Independent of 30: useful with today's `child` / `containing`. `NOT (NOT e)` is the same function; `outer` pullbacks while walking. Motivating text-ops examples stay in 30.

## Out of scope (both)

Product F#. OUTER implementation. Replacing `tree`. Post-pass prune.

## WORK.md mutations (for the parent)

- `add` [[.scratch/expression-language/issues/30-text-operations.md]] — plan/lock text ops: Node→text coercion; `left`/`right`/`name`/`text`; equality word `is`; redefine `containing`/`re`/`rei` as string ops
- `add` [[.scratch/expression-language/issues/31-if-pullback.md]] — plan/lock `IF` combinator (same-input pullback); independent of 30
