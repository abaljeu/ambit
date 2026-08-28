# Expression Language

Labels: wayfinder:map

## Destination

A hand-off spec: syntax, evaluation semantics, and the first primitive catalog for a Prolog-like Expression language that extends implemented path references with a left-to-right word pipeline. Implementation is a later effort.

## Notes

Domain: Graph query and Expression language over Node, text, and number Answers. Consult [[.agents/skills/domain-modeling/SKILL.md]], [[.scratch/expression-language/reports/existing-language-survey.md]], [[doc/roadmap/language-syntax-and-semantics.md]], [[doc/roadmap/reference-expression-interpretation.md]]. Working draft: [[.scratch/expression-language/spec-draft.md]].

Standing preferences: plan, do not implement eval or parse in `src/`. Use Graph, Node, ROOT, Ref, Owned, Header, Children. User is AFK; recommended answers live on tickets, not as silent locks. Stay on `w/broken` until asked.

## Decisions so far

- [[.scratch/expression-language/issues/09-research-prolog-control-mapped.md]] — Research: Prolog control mapped to this language — conjunction, disjunction, and negation-as-failure map as control over an Answer stream; collection stays a context rule; cut, unification, and `bagof` stay fog. [[.scratch/expression-language/reports/prolog-control-mapping.md]]
- [[.scratch/expression-language/issues/10-research-amble-refexpr-seams.md]] — Research: existing Amble and RefExpr seams the spec must not contradict — empty miss is fail-to-answer; pipeline space must share operators with Amble juxtaposition; Find AND is not the pipeline; `**` Owned-only versus other steps following Ref. [[.scratch/expression-language/reports/amble-refexpr-seams.md]]
- [[.scratch/expression-language/issues/01-pipeline-versus-amble-juxtaposition.md|Pipeline versus Amble juxtaposition]] — juxtaposition is left-associative (APL reverse); space only lexes; `#todo` is `#` plus `todo` meaning tagged; anchors/postfix/infix; `text` is postfix (`Ref text`); omitted left side is the current Node.
- [[.scratch/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]] — every path operator is a Prolog-style predicate of its left-hand Nodes (0/1/many Answers; miss is fail not error); `#x` searches down; `/` keeps Directory Node and Workspace Node; `**` is `descendant`; `:*` / `!*` replace bare `:` / `!`.
- [[.scratch/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] — closed words: `root`, `descendant`, `containing` (Header text), `named` (not tagged), `NOT`, `AND`, `OR` (comma same as OR); juxtaposition is composition, not boolean; comma/OR concatenates Answers.
- [[.scratch/expression-language/issues/04-boolean-operators-as-control.md|Boolean operators as control]] — Prolog-like control, not Prolog syntax; reserved `AND` `OR` `NOT` (caps only); no `;`; comma is `OR`; combinators bind juxtaposition then `NOT` then `AND` then `OR`/comma; same-operator chains associative; mixed `AND`/`OR` legal by precedence but write parentheses; `NOT` keeps the left Node on zero Answers from the predicate.
- [[.scratch/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]] — Answers are a sequence (0/1/many); eval engine is implementation (fastest that preserves Answers and order); juxtaposition left-to-right; `OR` concatenates and may repeat; `AND` is left-predicate order, at most once; materialise as Children is [[.scratch/expression-language/issues/06-top-level-context-node-versus-text.md]].

## Not yet specified

Variable binding and unification. Cut and if-then. Aggregations (`findall`/`bagof`-shaped collection as a primitive). How Unloaded Nodes participate in walks. How Find versus Run versus display share the language (partially ticketed in [[.scratch/expression-language/issues/06-top-level-context-node-versus-text.md]]; Find replacement is out of scope). Shell `> …` (considered, not in the first recommended statement set). Quoted path segments versus quoted filter strings.

## Out of scope

Implementing eval in AmbleEval or AmbleRun in this effort. Replacing the Find dialog. A persistence codec for PathExpr. A full Prolog system (unless a later ticket pulls one piece in).
