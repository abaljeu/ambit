# Text operations plan

Planning only. No product F#. [[../spec.md]] reserved `text` / `name` / `IS` rows point here; the closed catalog still has today's `Node ⇒ Node` Header filters (issue 30 adds a Text overload, it does not replace the Node row). Issues: [[../issues/30-text-operations.md]] (text ops, locks closed 2026-08-29) and [[../issues/31-if-pullback.md]] (`IF`, separate, done).

## Issue 30 — text operations (locked)

No implicit Node→Text. A Text-domain term (`left`, `right`, Text operands of `IS`) on a Node is a miss, not a hidden `text`. Write `text left 5`, not `left 5`, on a Node. Retract “all string functions coerce via `text`”. Retract juxtaposition inserting coerce at the Node/Text seam.

`text` is `node.text`, always one Text Answer from a Node. `text` on Text is a miss (Node extractor, not identity). No current example fights that (`#todo text` in the spec is Node then `text`). `name` already yields Text: `name right 4 IS ".txt"` needs no `text` prefix. Node path: `text right 4 IS ".txt"`. Name path: `name right 4 IS ".txt"`.

Miss is the empty sequence ([[src/Shared/ExprEval.fs]] `ofOption`). [[src/Shared/ExprAnswer.fs]] is `Node | Text`. Option is a type-level picture of miss only: `Some x` is one Answer; `None` is empty. Bind already skips the next term ([[../spec.md]] chapter 6: every term is `Answer → seq<Answer>`; juxtaposition is monadic bind). Do not add an Option Answer case.

`IS` is an infix combinator (capitals), both sides Expressions, not a catalog slot row `is "…"`. Evaluation story (1), same-input (AND-shaped): `E⟦e1 IS e2⟧ x` runs `e1` and `e2` both on `x`; yield each LHS Answer `y` for which some `z` from `e2` is equal. Empty `e1` ⇒ empty. Empty `e2` ⇒ empty. Not juxtaposition (RHS-in-LHS-context). Not Run `=`. Not `AND`'s at-most-once intersection. Parse attach is the `AND` family ([[../spec.md]] chapter 4 `AndExpr`; [[src/Shared/ExprParse.fs]] `parseAndTail`), not prefix `NOT` / `IF` / `OUTER`. Quoted `"rapid"` is an Expression that yields that Text from any input. `text left 5 IS "rapid"`: `text` then both sides on that Text; yield the LHS Text on match. Do not write `left 5 IS "rapid"` on a Node. Pullback to Node: `IF (text left 5 IS "rapid")`. Retract catalog-row `is` / Option-call wording.

| Word | Role |
| --- | --- |
| `text` | Node ⇒ Text. `node.text` (not the Name; Header in [[CONTEXT.md]] includes both). Always one Text Answer from a Node; empty string is not a miss. Text input is a miss. |
| `name` | Node ⇒ Text. Filename `Ok` only ([[src/Shared/Filename.fs]] `tryValue`); Empty and Invalid miss. `name right 4` is Name then suffix. |
| `left N` / `right N` | Text ⇒ Text. Prefix / suffix of length N. Always one string Answer. Short input → whole string. N < 1 → empty string. Length never causes a miss. Node input is a miss. Number slot (amend “number only after `:` / `!`”). |
| `IS` | Infix combinator. Same-input; yield matching LHS. Capitals. Quoted Text as an Expression this slice. |
| `containing` / `re` / `rei` | Dual. Text → same Text or empty (substring / regex). Node via `node.text` → same Node or empty. Keeps `OUTER containing "blue"` and `node containing "blue"` as today for Nodes. |

`containing` / `re` / `rei` stay Node ⇒ Node on a Node (today’s Header-text filters, [[../issues/29-re-and-rei-header-filters.md]]) and add Text ⇒ Text on Text. Retract “redefine as Text-only”. Search/Move still accept `root descendant containing "the"`. `OUTER containing "blue"` still yields Nodes.

Without pullback, `nodes text left 5 IS "rapid"` and `nodes name right 4 IS ".txt"` yield Text, not Nodes. Do not spec `IF` in 30.

Do not start issue 30 coding in this plan.

## Issue 31 — `IF` pullback

Separate combinator. Yield the input when the operand is nonempty. Independent of 30: useful with today's `child` / `containing`. `NOT (NOT e)` is the same function; `OUTER` pullbacks while walking. Motivating text-ops examples stay in 30. Status: done.

## Out of scope (both)

Product F#. Replacing `tree`. Post-pass prune.

## WORK.md mutations (for the parent)

- expected-outcome tweak of the existing Pending row [[plan/expression-language/issues/30-text-operations.md]] — plan locked: explicit `text` coerce (no implicit Node→Text); `containing`/`re`/`rei` dual Text→Text or Node via node.text→Node; infix `IS`; `left`/`right` always string; name Filename.Ok only
