# Expression Language

Labels: wayfinder:map

## Destination

A hand-off spec: syntax, evaluation semantics, and the first primitive catalog for a Prolog-like Expression language that extends implemented path references with a left-to-right word pipeline. Implementation is a later effort.

## Notes

Domain: Graph query and Expression language over Node, text, and number Answers. Consult [[.agents/skills/domain-modeling/SKILL.md]], [[plan/expression-language/reports/existing-language-survey.md]], [[doc/roadmap/language-syntax-and-semantics.md]], [[doc/roadmap/reference-expression-interpretation.md]]. Working draft: [[plan/expression-language/spec-draft.md]].

Standing preferences: plan, do not implement eval or parse in `src/`. Use Graph, Node, ROOT, Ref, Owned, Header, Children. User is AFK; recommended answers live on tickets, not as silent locks. Stay on `w/broken` until asked.

## Decisions so far

- [[plan/expression-language/issues/09-research-prolog-control-mapped.md]] — Research: Prolog control mapped to this language — conjunction, disjunction, and negation-as-failure map as control over an Answer stream; collection stays a context rule; cut, unification, and `bagof` stay fog. [[plan/expression-language/reports/prolog-control-mapping.md]]
- [[plan/expression-language/issues/10-research-amble-refexpr-seams.md]] — Research: existing Amble and RefExpr seams the spec must not contradict — empty miss is fail-to-answer; pipeline space must share operators with Amble juxtaposition; Find AND is not the pipeline; `**` Owned-only versus other steps following Ref. [[plan/expression-language/reports/amble-refexpr-seams.md]]
- [[plan/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|Pipeline versus Amble juxtaposition]] — juxtaposition is left-associative (APL reverse); space only lexes; `#todo` is `#` plus `todo` meaning tagged; anchors/postfix/infix; `text` is postfix (`Ref text`); omitted left side is the current Node.
- [[plan/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]] — every path operator is a Prolog-style predicate of its left-hand Nodes (0/1/many Answers; miss is fail not error); `#x` searches down; `/` is postfix (not a prefix); `**` is `tree`; `:*` / `!*` replace bare `:` / `!`; a number is only valid on the right of `:` or `!`.
- [[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] — closed words: `root`, `child`, `descendant` (closure of `child`), `tree`, `containing` (Header text), `named` (not tagged), `NOT`, `AND`, `OR` (comma same as OR); juxtaposition is composition, not boolean; comma/OR concatenates Answers.
- [[plan/expression-language/issues/04-boolean-operators-as-control.md|Boolean operators as control]] — Prolog-like control, not Prolog syntax; reserved `AND` `OR` `NOT` (caps only); no `;`; comma is `OR`; combinators bind juxtaposition then `NOT` then `AND` then `OR`/comma; same-operator chains associative; mixed `AND`/`OR` legal by precedence but write parentheses; `NOT` keeps the left Node on zero Answers from the predicate.
- [[plan/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]] — Answers are a sequence (0/1/many); eval engine is implementation (fastest that preserves Answers and order); juxtaposition left-to-right; `OR` concatenates and may repeat; `AND` is left-predicate order, at most once; materialise as Children is [[plan/expression-language/issues/06-top-level-context-node-versus-text.md]].
- [[plan/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context: Node versus text]] — typed functions; kind mix via booleans is an error; no number producers; Run: Node → Ref Children, text → new Owned Nodes; Search/Move dialog; language matcher opt-in with `=` (else word search); zoomRoot left; pick one (zoom vs relocate); no display consumer. Statement form, no-op, blueletter, unfold: [[plan/expression-language/issues/07-statements-in-this-spec.md]].
- [[plan/expression-language/issues/07-statements-in-this-spec.md|Statements in this spec]] — Run is `= Expression` or `Name=Expression` (plus rename); bare Expression forbidden (do nothing); `#ident =` rejected; `>` not in this spec; 0 Answers/type error → blueletter `No matches found`; unfold when Children are written; dialog leading `=` evals, else word search.
- [[plan/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]] — HITL: rows match, then amended (`//ws/x` Workspace or Directory; `#blue` defers to locked `#` search; `// OR /` undefined; bare `3` type error; walk words on 12); page is not locked syntax.
- [[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]] — `child` finds Children (Owned and Ref); `descendant` is that closure (follows Ref); `tree` / `**` is acyclic Owned-only (`// tree`); no Directory/Workspace stop.
- [[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]] — drop `of`; drop Amble comma-as-`FunCall`; comma stays `OR`; `sort 3,5,2` is not defined.
- [[plan/expression-language/issues/13-fog-of-the-first-spec.md|Fog of the first spec]] — unification, cut/if-then, and `findall`/`bagof` not planned; Unloaded walk is fail-to-answer; quotes for filters only; numbers and shell out for now.
- [[plan/expression-language/issues/14-server-side-search.md|Server-side search]] — all eval is local; server postponed.
- HITL 2026-08-28 — `section` is the pure filter “named Normal Node”; `subsection` is the spoken catalog spelling of cluster `#` (search for sections below). `named` stays the name-glob filter. Lock: [[plan/expression-language/reports/section-filter-lock.md]].

## Not yet specified

Quoted path segments (`//"a b"`). Number-returning functions and shell `> …` (out of this spec for now). Server-side Search (postponed; all eval is local).

## Out of scope

Implementing eval in AmbleEval or AmbleRun in this effort. Replacing the Find or Move dialog chrome (a later matcher may use this language). A persistence codec for PathExpr. A full Prolog system (unless a later ticket pulls one piece in). Logical variables and unification (not planned). Cut and if-then (not planned). A `findall`/`bagof` collection primitive (not planned; collection stays the consumer).
