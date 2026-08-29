# 30 — Text operations (`text`, `left`, `right`, `name`, `is`)

**Context:** Functions whose domain is Text must accept a Node by coercion through `text`. Equality uses the word `is` (not `=`). Then `containing`, `re`, and `rei` become string operations, not Header-only Node filters. Work on branch `w/tree2-semantics`. Do not implement product code in this issue. Do not spec the pullback combinator `IF` here: that is [[31-if-pullback.md]]. Do not implement `outer` (sibling [[28-outer-prefix-combinator.md]]). Do not replace `tree`. Do not plan a post-pass prune.

**What to lock:** Catalog rows and the Node→Text coercion rule below. After HITL closes the open questions, an implementation issue can go `ready-for-agent`. Until then this file is the plan.

**Blocked by:** none for planning. Implementation must not start while [[28-outer-prefix-combinator.md]] still owns [[src/Shared/ExprParse.fs]], [[src/Shared/ExprEval.fs]], and the `outer` walk in [[src/Shared/ExprWalk.fs]].

**See also:** [[../spec.md]] chapter 2 (Answer types, equality), chapter 5 (signatures), chapter 7 (`containing` / `re` / `rei` today; reserved `text` / `name`), chapter 8 (Run vs Search types); [[31-if-pullback.md]]; [[29-re-and-rei-header-filters.md]]; [[CONTEXT.md]] Node, Header, Answer.

**Status:** needs-info

## Coercion rule

If a term's domain is Text and the input Answer is a Node, apply `text` then the term. If the input is already Text, do not coerce. If coerce is impossible (the input is not Node and not Text), miss.

Coerce uses `text`, never `name`. To operate on the Name, write `name` first: `name right 4 is ".txt"`.

Juxtaposition `e1 e2` inserts this coerce at the seam when `e1` yields Node and `e2` wants Text. The same rule applies when a consumer applies a Text-domain term to its initial Node.

## Catalog rows (this slice)

| Entry | Spellings | Slot | Signature | Answer function |
| --- | --- | --- | --- | --- |
| text | `text` | — | `Node ⇒ Text` | The Header text field (`node.text`). Postfix. A Node with empty Header text yields the empty string (one Answer), not a miss. |
| name | `name` | — | `Node ⇒ Text` | The Name. Then `name right 4` is Name, then suffix. |
| left | `left` | required Int | `Text ⇒ Text` | Prefix of length N. |
| right | `right` | required Int | `Text ⇒ Text` | Suffix of length N. |
| is | `is` | required quoted string | `Text ⇒ Text` | Yield the input Text when it equals the literal (Answer equality on Text). Miss otherwise. Juxtaposition: `left 5 is "rapid"`. |
| containing | `containing` | required quoted string | `Text ⇒ Text` | Yield the input Text when it contains the argument as a case-insensitive substring. |
| re | `re` | required quoted string | `Text ⇒ Text` | Yield the input Text when it matches the argument as a regular expression, case-sensitive. Invalid pattern is a miss. Same engine as [[29-re-and-rei-header-filters.md]]. |
| rei | `rei` | required quoted string | `Text ⇒ Text` | Same as `re`, case-insensitive via engine flags. |

`left` / `right` need a Number slot. Amend the locked wording that a number is only valid as the right operand of `:` or `!`: a number is valid as the slot of `:`, `!`, `left`, or `right`. Bare `left` / `right` / `is` / `containing` / `re` / `rei` are missing-argument parse errors.

`is` is a catalog word (lowercase), not a reserved combinator. Do not use `=`. Run already uses `=` for statements ([[../spec.md]] chapter 8).

## Breaking change

Today `containing`, `re`, and `rei` are `Node ⇒ Node` Header-text filters ([[../spec.md]] chapter 7; issue 29). This slice redefines them as `Text ⇒ Text`. On a Node they coerce through `text`, so `node containing "blue"` still matches the same Header text field, but the Answers are Text, not the Node.

`root descendant containing "the"` therefore types `Node ⇒ Text`. Search and Move require `Node ⇒ Node` ([[../spec.md]] chapter 8), so that Expression is a type error there unless a pullback combinator wraps it. Run still accepts `Node ⇒ Text` and writes new Owned Nodes from the strings.

`outer containing "blue"` still yields Nodes: `outer` tests emptiness of the operand and yields the visited Node ([[28-outer-prefix-combinator.md]]). Same-input pullback (keep this Node when a Text predicate succeeds) is not this issue; see [[31-if-pullback.md]].

## Examples (Answers are Text unless noted)

- `text` — Header text of the current Node.
- `left 5 is "rapid"` — coerce via `text`, take prefix 5, keep that string when it equals `rapid`.
- `name right 4 is ".txt"` — Name, then suffix 4, keep when it equals `.txt`.
- `containing "blue"` — coerce via `text`, keep the Header text when it contains `blue`.
- `re ".*blue.*"` / `rei ".*BLUE.*"` — same coerce, regex on that string.
- `(nodes) left 5 is "rapid"` — yields Text, not Nodes. A pullback combinator is required to keep Nodes; that combinator is issue 31, not this file.
- `root outer (left 5 is "rapid")` — yields Nodes (outermost Owned descendants whose Header-text prefix is `rapid`), because `outer` already pullbacks. Sibling issue 28 owns `outer`.

## Assumptions

1. `text` extracts `node.text` (what the spec already calls Header text for `containing`), not the Name and not the whole Header. Header in [[CONTEXT.md]] is every field except Children, so Header includes both `node.text` and the Name. This assumption picks one field.
2. `name` extracts the Name string. An unnamed Node (empty Filename) yields the empty string, one Answer, parallel to empty Header text. It is not a miss.
3. `is` in this slice compares Text to a quoted literal only, case-sensitive, by the chapter 2 Text equality rule. `containing` stays case-insensitive; `is` does not.
4. If N is greater than the string length, `left N` / `right N` yield the whole string. If N is less than 1, miss.
5. Length N is the .NET / Fable string length (UTF-16 code units), the same measure the rest of Shared uses.

## Open questions (HITL)

- Confirm assumption 1: is `text` locked to `node.text`? If not, which Header field does it extract?
- Confirm assumption 2: empty Name is empty Text, not a miss?
- Does `is` stay Text-only in the first implementation, with Node identity later? (Plan: Text-only this slice.)
- Confirm assumption 4 for short strings and non-positive N.

## Out of scope

- Combinator `IF` ([[31-if-pullback.md]]).
- Combinator `outer` ([[28-outer-prefix-combinator.md]]).
- Replacing `tree` / `**`.
- A post-pass prune.
- Node-identity `is`, Number Answers, `sort`.
- Product F# in this planning issue.

## Comments
