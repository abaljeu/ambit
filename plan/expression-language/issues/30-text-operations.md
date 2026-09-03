# 30 — Text operations (`text`, `left`, `right`, `name`, `IS`)

**Context:** Do not coerce Node to Text. Use `text` to coerce. `containing`, `re`, and `rei` are filters on Text that yield Text, or on a Node (via `node.text`) that yield the Node. Equality uses the infix combinator `IS` (not `=`). Work on branch `w/tree2-semantics`. Do not implement product code in this issue. Do not spec the pullback combinator `IF` here: that is [[31-if-pullback.md]]. Do not implement `OUTER` (sibling [[28-outer-prefix-combinator.md]]). Do not replace `tree`. Do not plan a post-pass prune.

**What to lock:** Catalog rows; no implicit Node→Text; dual `containing` / `re` / `rei`; the miss/bind seam; combinator `IS`. HITL closed the open questions on 2026-08-29, then locked `IS` as an infix combinator with same-input evaluation (AND-shaped), then locked explicit `text` (no implicit coerce) and dual filters. Retract the catalog-row `is` / Option-call wording. Retract “all string functions coerce via `text`”. This file stays the plan. Do not start product F# here. An implementation issue can go `ready-for-agent` later.

**Blocked by:** none for planning. [[28-outer-prefix-combinator.md]] is done. Do not start implementation in this issue.

**See also:** [[../spec.md]] chapter 2 (Answer types, equality), chapter 4 (`AndExpr` infix attach), chapter 5 (signatures), chapter 6 (juxtaposition bind; `AND` same-input), chapter 7 (`containing` / `re` / `rei` today; reserved `text` / `name`), chapter 8 (Run vs Search types); [[31-if-pullback.md]]; [[29-re-and-rei-header-filters.md]]; [[CONTEXT.md]] Node, Header, Answer; [[src/Shared/Filename.fs]]; [[src/Shared/Model.fs]] `node.text` / `node.name`; [[src/Shared/ExprAnswer.fs]]; [[src/Shared/ExprEval.fs]] `ofOption` / `andEval`; [[src/Shared/ExprParse.fs]] `parseAnd` / `parseAndTail` (not `tryPrefix`).

**Status:** ready-for-human

Implementation of this locked plan landed 2026-08-29. Report: [[../reports/text-ops-impl.md]].

- [x] `text` is `node.text`, one Text Answer from a Node; the empty Header text is the empty string; a Text input is a miss.
- [x] `name` is Filename `Ok` only; Empty and Invalid are a miss.
- [x] `left n` / `right n` are `Text ⇒ Text`, always one Text Answer; a short input yields the whole string, an n below 1 yields the empty string, and a Node input is a miss.
- [x] `IS` is an infix combinator in the `AND` family: both operands run on the same input and the matching left Answers yield. Lowercase `is` is an ordinary Name. Bare `IS` is a parse error.
- [x] A quoted string in Expression position is a Text Expression, so `"rapid"` is a valid operand of `IS`.
- [x] `containing` / `re` / `rei` are dual: a Text input yields the same Text, and a Node input tests `node.text` and yields the Node. The Node behavior does not regress.
- [x] No implicit Node to Text coerce: `left 5` at the top level is a type error, and `IF (text left 5 IS "rapid")` is the Node pullback.
- [x] `tree`, `OUTER`, `IF`, `NOT`, `AND`, `OR`, and the Run consumer `=` are unchanged.
- [ ] HITL: Run `= … IF (text left 5 IS "rapid")` and `= … IF (name right 4 IS ".txt")` on `/ambit` or `/ambit?debug=1`; Answers are Nodes; bare `left 5` is a type error; lowercase `is` is not the combinator; `"d" "e"` is a parse error.

## No implicit coerce

Do not coerce Node to Text. A term whose domain is Text (`left`, `right`, Text operands of `IS`) does not apply `text` for you. A string-expecting term on a Node is a miss (empty), not a hidden `text`. Write `text left 5`, not `left 5`, when the input is a Node.

`text` is the explicit coerce: it is `node.text`, always one Text Answer from a Node. Retract the earlier rule that all string functions coerce through `text`. Retract juxtaposition inserting coerce when `e1` yields Node and `e2` wants Text.

To operate on the Name, write `name` first: `name right 4 IS ".txt"`. `name` already yields Text, so do not write `text` after `name`. Node path: `text right 4 IS ".txt"` (Header `node.text`). Name path: `name right 4 IS ".txt"` (Filename).

`left` / `right` stay `Text ⇒ Text`. Top-level Expressions must type `Node ⇒ τ` ([[../spec.md]] chapter 8), so bare `left 5` at Run / Search / Move is a type error. Write `text left 5` (`Node ⇒ Text`) or `name right 4` (`Node ⇒ Text`).

## Miss vs value

[[src/Shared/ExprAnswer.fs]] is `Node | Text`. A miss is the empty sequence, not a third Answer type ([[src/Shared/ExprEval.fs]] `empty` / `ofOption`). Option is a type-level picture of miss only: `Some x` is one Answer; `None` is empty. Bind already skips the next term when the left sequence is empty ([[../spec.md]] chapter 6: every term is `Answer → seq<Answer>`; juxtaposition `E⟦e1 e2⟧ x = concat [ E⟦e2⟧ y | y ← E⟦e1⟧ x ]`). Do not add an Option case to ExprAnswer.

`text` on a Node always yields one Text Answer (`node.text`). `text` on Text is a miss: `text` is a Node extractor, not identity. No example in this file applies `text` to Text; `#todo text` in [[../spec.md]] is Node then `text` and does not fight this lock. Composition `name text` does not type (`Text` then `Node ⇒ Text`).

`left` / `right` always yield one string Answer. `name` miss is empty, so juxtaposition never reaches the next term. `IF (name IS "x")` drops Nodes whose Filename is not `Ok` because the left operand of `IS` is empty.

## Dual filters (`containing`, `re`, `rei`)

These three are overloaded by input kind:

- Input Text: substring or regex on that string; yield the same Text or empty.
- Input Node: use `node.text` (Header text, not the Name); yield the **Node** or empty.

This keeps `OUTER containing "blue"` and `node containing "blue"` as today for Nodes. They are not Text-only rows. Retract “redefine as `Text ⇒ Text` only”. Retract “on a Node they coerce through `text` and yield Text”.

Typing picks the overload from the input Answer type: `descendant containing "blue"` is `Node ⇒ Node`; `text containing "blue"` is `Node ⇒ Text`.

## Combinator `IS`

`IS` is a combinator: capitals only, infix, both sides are Expressions. It is not a catalog slot row `is "…"`. Lowercase `is` is a Name, not this combinator. Do not use `=`. Run already uses `=` for statements ([[../spec.md]] chapter 8). Retract catalog-row `is`, the Option-call signature, and the bind story that evaluated `is` once per LHS Answer.

Parse attach is the `AND` family, not prefix `NOT` / `IF` / `OUTER`. Spec chapter 4: `AndExpr ::= NotExpr ("AND" NotExpr)*`. Prefix combinators sit on `NotExpr`. [[src/Shared/ExprParse.fs]]: `parseAndTail` loops on `AndKw` between `parseNot` results; `tryPrefix` is `NOT` / `OUTER` / `IF` only. `IS` attaches like `AND` (infix between `NotExpr`s). Do not give `IS` a prefix production. Mixed `AND` / `IS` chains: write the parentheses.

Evaluation story (1), same-input (AND-shaped), not (2) RHS-in-LHS-context:

`E⟦e1 IS e2⟧ x`: evaluate `e1` and `e2` both on the same input `x`. Yield each LHS Answer `y` from `e1`, in that order, for which some `z` from `e2` is equal (`y` equals `z` by chapter 2 Answer equality). Empty `e1` ⇒ empty (nothing to yield; no per-element `IS` body). Empty `e2` ⇒ no equals, empty. This is not juxtaposition: `e2` does not run on each `y`. This is not Run `=`. This is not `AND`'s at-most-once intersection; `IS` yields matching LHS Answers.

A quoted string in Expression position is an Expression that yields that Text from any input. That amends “literals are never terms” for quoted strings, so `"rapid"` is a valid operand of `IS`. A quoted string that fills a catalog slot (`containing "blue"`) stays an argument. Numbers stay operator arguments (`left` / `right` slots).

`text left 5 IS "rapid"`: `text` extracts Header text; `left 5` and `"rapid"` both run on that Text. Compare the results. Yield the LHS Text on match. Do not write `left 5 IS "rapid"` on a Node: `left` does not coerce.

Pullback to Node still needs `IF`: `IF (text left 5 IS "rapid")`. Same-input pullback is [[31-if-pullback.md]].

Type is AND-shaped: both operands `τ1 ⇒ τ2`; `e1 IS e2` is `τ1 ⇒ τ2`. This slice's motivating examples are Text. Answer equality already includes Node identity; Node-yielding operands are the same rule, not a second catalog row.

Bare `IS` and `e1 IS` with no right operand are missing-operand parse errors, same as incomplete `AND`.

## Catalog rows (this slice)

| Entry | Spellings | Slot | Signature | Answer function |
| --- | --- | --- | --- | --- |
| text | `text` | — | `Node ⇒ Text` | `node.text`. Postfix. Always one Text Answer from a Node. Empty Header text yields the empty string, not a miss. Text input is a miss (Node extractor). |
| name | `name` | — | `Node ⇒ Text` | Filename `Ok` only: `Ok s → Some s`, Empty and Invalid → `None` ([[src/Shared/Filename.fs]] `tryValue`). Miss is the empty sequence. Then `name right 4` is Name, then suffix. |
| left | `left` | required Int | `Text ⇒ Text` | Prefix of length N. Always one Text Answer. Short input → the whole string. N < 1 → the empty string. Length never causes a miss. Node input is a miss (no implicit `text`). |
| right | `right` | required Int | `Text ⇒ Text` | Suffix of length N. Same short-input and non-positive-N rules as `left`. Length never causes a miss. Node input is a miss (no implicit `text`). |
| containing | `containing` | required quoted string | Text ⇒ Text, or Node ⇒ Node | Text: yield the input Text when it contains the argument as a case-insensitive substring. Node: yield the input Node when `node.text` contains the argument (Header text, not the Name). |
| re | `re` | required quoted string | Text ⇒ Text, or Node ⇒ Node | Text: yield the input Text when it matches the argument as a regular expression, case-sensitive. Node: yield the input Node when `node.text` matches. Invalid pattern is a miss. Same engine as [[29-re-and-rei-header-filters.md]]. |
| rei | `rei` | required quoted string | Text ⇒ Text, or Node ⇒ Node | Same as `re`, case-insensitive via engine flags. |

`left` / `right` need a Number slot. Amend the locked wording that a number is only valid as the right operand of `:` or `!`: a number is valid as the slot of `:`, `!`, `left`, or `right`. Bare `left` / `right` / `containing` / `re` / `rei` are missing-argument parse errors.

## Breaking change

Today `containing`, `re`, and `rei` are `Node ⇒ Node` Header-text filters ([[../spec.md]] chapter 7; issue 29). This slice keeps that Node overload and adds Text ⇒ Text. `node containing "blue"` and `OUTER containing "blue"` still yield Nodes. `root descendant containing "the"` still types `Node ⇒ Node`. Search and Move still accept it. Retract the prior-plan claim that these rows become Text-only and that Search/Move reject bare `containing`.

New Text-domain terms (`left`, `right`, `IS` on Text) do not coerce. `nodes left 5 IS "rapid"` is a type error or a miss on the Node (no hidden `text`). Write `text left 5 IS "rapid"` (yields Text) or `IF (text left 5 IS "rapid")` (yields Nodes). Run still accepts `Node ⇒ Text`.

`OUTER containing "blue"` still yields Nodes: `OUTER` tests emptiness of the operand and yields the visited Node ([[28-outer-prefix-combinator.md]]). Same-input pullback for Text predicates is not this issue; see [[31-if-pullback.md]].

## Examples (Answers are Text unless noted)

- `text` — `node.text` of the current Node (empty string is one Answer).
- `text left 5 IS "rapid"` — extract Header text, take prefix 5 (the whole string if shorter; the empty string if N < 1), keep that string when it equals `rapid`. `left` and `"rapid"` both run on the Text from `text`. `left` does not miss on length. Do not write `left 5 IS "rapid"` on a Node.
- `right 0` / `left -1` — one Text Answer, the empty string (input must already be Text). Then `IS ""` keeps it.
- `name right 4 IS ".txt"` — Name only when Filename is `Ok`; then suffix 4; keep when it equals `.txt`. No `text` prefix: `name` yields Text. Empty or Invalid Filename: `name` misses; the left operand of `IS` is empty, so `IS` yields empty.
- `text right 4 IS ".txt"` — same suffix/`IS` shape on Header `node.text`, not the Name.
- `name IS "x"` on Filename Empty or Invalid — `name` misses; `IS` yields empty.
- `IF (name IS "x")` — keep the Node when `name` yields `"x"`. Nodes without Filename.Ok miss the operand and drop.
- `IF (text left 5 IS "rapid")` — keep the Node when Header-text prefix 5 equals `rapid`. Do not write `IF (left 5 IS "rapid")` on a Node.
- `containing "blue"` — on a Node: keep the Node when Header text contains `blue` (today’s filter). On Text: keep that Text when it contains `blue`.
- `re ".*blue.*"` / `rei ".*BLUE.*"` — same dual: Node yields Node; Text yields Text.
- `(nodes) text left 5 IS "rapid"` — yields Text, not Nodes. A pullback combinator is required to keep Nodes; that combinator is issue 31, not this file.
- `root OUTER (text left 5 IS "rapid")` — yields Nodes (outermost Owned descendants whose Header-text prefix is `rapid`), because `OUTER` already pullbacks. Sibling issue 28 owns `OUTER`.
- `root OUTER containing "blue"` — yields Nodes (Node overload of `containing`).

## Locked (2026-08-29 HITL)

1. `text` extracts `node.text`, not the Name and not the whole Header. Header in [[CONTEXT.md]] is every field except Children, so Header includes both `node.text` and the Name. This lock picks `node.text`. Always one Text Answer from a Node. Text input is a miss.
2. `name` is `Filename.tryValue`: `Ok s` is `Some s`; Empty and Invalid are `None` (empty sequence). Not empty Text.
3. `IS` is an infix combinator (capitals), AND-shaped same-input: both operands run on `x`; yield matching LHS Answers. Not a catalog slot row. Not juxtaposition / RHS-in-LHS-context. Not Run `=`. Quoted `"rapid"` is an Expression that yields that Text from any input. Pullback to Node is `IF (text left 5 IS "rapid")`. `containing` stays case-insensitive; `IS` uses chapter 2 Text equality (case-sensitive).
4. `left` / `right` always yield one string Answer. If N is greater than the string length, yield the whole string. If N is less than 1, yield the empty string. Length never causes a miss. Domain is Text only; a Node input is a miss (write `text left 5`).
5. Length N is the .NET / Fable string length (UTF-16 code units), the same measure the rest of Shared uses.
6. No implicit Node→Text. `containing` / `re` / `rei` are dual: Text → same Text or empty; Node via `node.text` → same Node or empty. Retract “all string functions coerce via `text`”.

## Out of scope

- Combinator `IF` ([[31-if-pullback.md]]).
- Combinator `OUTER` ([[28-outer-prefix-combinator.md]]).
- Replacing `tree` / `**`.
- A post-pass prune.
- Number Answers, `sort`.
- Product F# in this planning issue.

## Comments

HITL 2026-08-29 closed the four open questions: `text` = `node.text`; `name` = Filename `Ok` only; equality is not Run `=`; `left` / `right` never miss on length. A later HITL the same day locked `IS` as the infix combinator with evaluation story (1): both sides on the same input `x`, yield matching LHS. Retract catalog-row `is`, Option-call wording, and the bind story that ran `is` per LHS Answer. `IS` attaches like `AND` (spec `AndExpr`; [[src/Shared/ExprParse.fs]] `parseAndTail`), not like prefix `NOT` / `IF` / `OUTER`. Current catalog `text` in [[src/Shared/ExprPrimitive.fs]] already yields `node.text`; this issue does not change that product code.

HITL 2026-08-29 (later): drop implicit Node→Text. Use `text` to coerce. `containing` / `re` / `rei` dual by input kind (Text→Text or Node via `node.text`→Node). Retract “all string functions coerce via `text`”. `text` on Text is a miss (Node extractor); no example fights that. Examples: `IF (text left 5 IS "rapid")`; `name right 4 IS ".txt"` needs no `text` prefix; Node path `text right 4 IS ".txt"` vs Name path `name right 4 IS ".txt"`.

Two quoted-string terms next to each other in juxtaposition (`"d" "e"`) are a dedicated parse error. Quoted strings remain Text Expressions; combinator operands and catalog slots stay legal.

- 2026-09-02: Parked from WORK.md. Remaining HITL: Run `= … IF (text left 5 IS "rapid")` and `= … IF (name right 4 IS ".txt")` on `/ambit` or `/ambit?debug=1`; confirm the Answers are Nodes, that a bare `left 5` reports a type error, that lowercase `is` is not the combinator, and that `"d" "e"` is a parse error. Implementation report: [[../reports/text-ops-impl.md]].
