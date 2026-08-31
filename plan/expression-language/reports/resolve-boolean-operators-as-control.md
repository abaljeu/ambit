# Resolve Boolean operators as control

Recorded the HITL lock on [[.scratch/expression-language/issues/04-boolean-operators-as-control.md|Boolean operators as control]]. Status is resolved.

## Changed

- Ticket Answer: Prolog-like control, not Prolog syntax; reserved `AND` `OR` `NOT` (caps only; lowercase is a name); no `;`; comma is `OR`; combinators bind juxtaposition then `NOT` then `AND` then `OR`/comma; same-operator chains associative; mixed `AND`/`OR` legal by precedence but write parentheses; `NOT` keeps the left Node on zero Answers from the predicate. No Boolean Answer type.
- [[.scratch/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] Answer: `not` revised to `NOT`.
- Map [[.scratch/expression-language/map.md]] Decisions so far: one gist line. Not yet specified no longer waits on precedence or letter-case.
- [[.scratch/expression-language/spec-draft.md]] boolean section marked Locked; catalog word `NOT`.
- [[.scratch/expression-language/reports/pipeline-examples.md]] uses `NOT containing`. [[.scratch/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]] stays open.
- [[.scratch/expression-language/project.md]] summary mentions boolean combinators locked. [[.scratch/index.md]] updated for that row.

## Frontier

Open, unblocked, unclaimed, first by number:

- [[.scratch/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]]
- [[.scratch/expression-language/issues/07-statements-in-this-spec.md|Statements in this spec]]
- [[.scratch/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]]
- [[.scratch/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]
- [[.scratch/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]]
