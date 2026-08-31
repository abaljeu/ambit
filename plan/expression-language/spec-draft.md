# Expression language spec draft

The comprehensive specification now lives at [[plan/expression-language/spec.md]]; this draft is superseded.

Working draft toward the destination spec. Sections marked **Locked** come from the destination named 2026-08-27. Sections marked **Proposed** are recommended answers waiting on open tickets. Research findings are cited; they are not a substitute for grilling.

## What an Expression is — Locked

An Expression is a Prolog-like non-deterministic predicate call with many possible Answers. Most Expressions, including current RefExpr path queries, find a Node. Text Answers are in scope. Number Answers wait until a catalog function returns them. Booleans are not Answer values; Prolog-style succeed and fail are control.

## Surface — Locked

Left-to-right word pipeline. Example intent: `root descendant containing "the" named "blue"`. Each unquoted word is a function or relation: input from the left, Answers to the right; it acts as a filter generator. Existing path refs (`/`, `//`, `#todo`, `^`, `.`, `*`, `**`) stay as the implemented base. This effort extends them with the pipeline.

## Statements — Locked

[[plan/expression-language/issues/07-statements-in-this-spec.md|Statements in this spec]]. First spec: `= Expression` and `Name=Expression` (rename plus materialise). Bare Expression is not a Run statement (do nothing). `#ident = Expression` is rejected. `>` shell is not in this spec. Valid `=` / `Name=` with 0 Answers or type error: blueletter Child `No matches found`. If Run writes Children, unfold that Node. Search/Move: leading `=` evals a Node Expression locally; otherwise today’s word search.

## Hand-off — Locked

The destination is a spec (syntax, evaluation semantics, first primitive catalog). Implementation is a later effort.

## Juxtaposition and fixity — Locked

[[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|Pipeline versus Amble juxtaposition]]. Space is a lexical separator, not an operator. Syntax is juxtaposition, left-associative: `a b c` is `((a) b) c` (APL reverse; monadics are postfix). One surface; not Amble prefix `FunCall`. Existing lexer stays. `#todo` is two tokens, `#` and `todo`; `(left) #todo` means `(left) named "todo"`. Mix of fixity: anchor (a value, e.g. `root`), postfix (unary on the left, e.g. `descendant`), infix (left and right, e.g. `containing`; right-hand kind is checked; `<expr> containing root` is a type error). What was Amble `text Ref` is `Ref text` (postfix `text`). Omitted initial left operand is the current Node.

## Path operators — Locked

[[plan/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]]. Path symbols are juxtaposition of Prolog-style predicates of the left-hand Nodes. Each finds 0, 1, or many Answers. A miss (out-of-range `:n` / `!n`, or a name / `#` / `/` search that matches nothing, or an Unloaded Node on a walk) is zero Answers, not an error. Omitted left side is the current Node. `//` is ROOT. `^` walks up to the nearest File Node, Directory Node, or Workspace Node. `.` walks up to the nearest Directory Node or Workspace Node. `//name` is path search from ROOT ([[doc/roadmap/reference-expression-interpretation.md]]). `/` is postfix only: keep Directory Node and Workspace Node; drop File Node and Normal Node; a following name is path search (`//ws/x`), not tag search. `/` is not a prefix. No bare `#`. `#x` searches down for Normal Nodes named `x`; unnamed Nodes are transparent; a named descendant is a wall; the left Node is not a wall. `*` is glob on a name (`#re*ed` equals `named "re*ed"`). `**` is `tree` (transitively Owned; no Directory/Workspace stop); it is not the word `descendant`. `x:n` / `x!n` are Child index and sibling offset (`!0` is `x`). Bare `x:` and `x!` are not defined. `x:*` is every Child; `x!*` is every sibling including `x`. A number is only valid as the right operand of `:` or `!`. Walk words: [[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]].

## First primitive catalog — Locked

[[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]]. Closed words: `root` (`//`), `child` (Children: Owned and Ref; same set as `:*`), `descendant` (closure of `child`), `tree` (transitively Owned; short form `**`; `// tree`), `containing` (quoted string; Header text only; path-segment quotes are later), `named` (not `tagged`; short form `#x`; same glob as `#re*ed`), `NOT` (negation-as-failure), `AND` (same-left intersection), `OR` (disjunction; comma `,` has the same meaning). `x f g` is composition, not boolean. `x (f, g)` and `#x , #y` concatenate Answers (`OR`). Composition of two pure filters equals `AND`; a generator such as `descendant` makes them differ. Out of this slice: postfix `text`, `name`, `sort`. `of` is dropped ([[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]). Path symbols stay as locked on Path references as pipeline terms.

## Boolean operators as control — Locked

[[plan/expression-language/issues/04-boolean-operators-as-control.md|Boolean operators as control]]. Prolog-like control, not Prolog syntax. Reserved words are `AND`, `OR`, `NOT` (all caps); lowercase is a name. No `;`. Comma is `OR`. Combinators, not pipeline words. Precedence, tightest first: juxtaposition, `NOT`, `AND`, `OR`/comma. Same-operator chains are semantically associative. Mixed `d AND b OR c` is `(d AND b) OR c`; write parentheses. `NOT`: for each left Node, any Answer from the predicate drops it; zero Answers keeps that Node. Compound predicates need parentheses. No Boolean Answer type.

## Multiple Answers — Locked

[[plan/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]]. An Expression yields 0, 1, or many Answers as a sequence. Lazy, eager, or backtracking is implementation: pick whatever is fastest, as long as Answers and order match the spec. Juxtaposition is left-to-right. `OR`/comma concatenates (may repeat). `AND` keeps the left predicate's Answers that also appear on the right, in left-predicate order (at most once). Materialising as Children is a consumer rule, not eval.

## Top-level context — Locked

[[plan/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context: Node versus text]]. Every function has a defined type. Mixing kinds via `AND` / `OR` / `NOT` is an error. No number producers in this catalog. A number literal is only valid as the right operand of `:` or `!`. Run: Node Answers become Ref Children; text Answers become new Owned Nodes. Search and Move keep the existing scrolling dialog; a leading `=` evals a Node-typed Expression locally (omitted left is zoomRoot); otherwise today’s word search; pick one Node (Search zooms, Move relocates). 0 Answers and parse/type error both show no hits. Statement form, no-op, blueletter, and unfold: [[plan/expression-language/issues/07-statements-in-this-spec.md]]. No display consumer.

## Unloaded walks — Locked

[[plan/expression-language/issues/13-fog-of-the-first-spec.md|Fog of the first spec]]. A walk that meets an Unloaded Node yields no Answer. It does not Load. All eval is local. Server-side Search is postponed: [[plan/expression-language/issues/14-server-side-search.md|Server-side search]].

## Examples — Confirmed prototype (not locked syntax)

[[plan/expression-language/reports/pipeline-examples.md]] HITL 2026-08-27 ([[plan/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]]): rows match, then amended. `//ws/x` may be a Workspace Node or Directory Node named `ws`. `#blue` defers to locked `#` search details. `// OR /` is undefined (`/` is not a prefix). Bare `3` is a type error. `child` / `descendant` / `tree` as on [[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]].

## Prolog control — Research resolved

[[plan/expression-language/issues/09-research-prolog-control-mapped.md]]: conjunction, disjunction, and negation-as-failure map as control over an Answer stream. Collection is a context rule. Cut, if-then, unification, and `bagof` grouping are not planned ([[plan/expression-language/issues/13-fog-of-the-first-spec.md|Fog of the first spec]]). Note: [[plan/expression-language/reports/prolog-control-mapping.md]].

## Seams the spec must not contradict — Research resolved

[[plan/expression-language/issues/10-research-amble-refexpr-seams.md]]: empty miss is fail-to-answer, not silent success; parse errors stay distinct. Pipeline space must share operators with Amble juxtaposition. Amble `,` stays concatenate. Find AND is not the pipeline. Implemented `**` is Owned-only and stops at Directory/Workspace; this spec’s `**` is `tree` and does not stop there. Other path steps follow Ref. Note: [[plan/expression-language/reports/amble-refexpr-seams.md]].
