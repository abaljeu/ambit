# Resolve First primitive catalog

Recorded the HITL decision on [[.scratch/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]]. Status is resolved. CONTEXT.md was left unchanged (`named` is catalog syntax, not a Graph glossary term).

## Changed

- Ticket Answer: closed words `root`, `descendant`, `containing` (Header text only), `named` (not `tagged`), `not`, `AND`, `OR` (comma same as `OR`). Juxtaposition is composition, not boolean. Comma/`OR` concatenates Answers. Pure-filter composition equals `AND`; a generator does not.
- Map [[.scratch/expression-language/map.md]] Decisions so far: one gist line. Fog no longer waits on this catalog for a short-symbol table.
- [[.scratch/expression-language/issues/04-boolean-operators-as-control.md|Boolean operators as control]] stays open; pointer that 03 locked composition versus `AND` versus comma/`OR` versus `not`. Remainder: precedence and letter-case.
- [[.scratch/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]] stays open; pointer that comma is kept as `OR` / concatenation; `of` is still that ticket.
- [[.scratch/expression-language/spec-draft.md]] catalog marked Locked.
- [[.scratch/expression-language/reports/pipeline-examples.md]] adds `containing "the" AND named "blue"` and `#x , #y`. [[.scratch/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]] stays open.
- [[.scratch/expression-language/project.md]] summary mentions catalog locked. [[.scratch/index.md]] updated for that row.

## Frontier

Open, unblocked, unclaimed, first by number:

- [[.scratch/expression-language/issues/04-boolean-operators-as-control.md|Boolean operators as control]]
- [[.scratch/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]]
- [[.scratch/expression-language/issues/07-statements-in-this-spec.md|Statements in this spec]]
- [[.scratch/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]]
- [[.scratch/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]
- [[.scratch/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]]
