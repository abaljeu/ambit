# Resolve How multiple answers surface

Recorded the HITL lock on [[.scratch/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]]. Status is resolved.

## Changed

- Ticket Answer: Answers are a sequence (0/1/many). Eval engine is implementation (fastest that preserves Answers and order). Juxtaposition left-to-right. `OR` concatenates and may repeat. `AND` is left-predicate order, at most once. Materialise as Children is a consumer on [[.scratch/expression-language/issues/06-top-level-context-node-versus-text.md]].
- [[.scratch/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context: Node versus text]] unblocked (`Blocked by: none`). Pointer that 05 locked the sequence.
- [[.scratch/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] Answer: pointer for `OR`/`AND` sequence rules.
- Map [[.scratch/expression-language/map.md]] Decisions so far: one gist line.
- [[.scratch/expression-language/spec-draft.md]] Multiple Answers marked Locked. Top-level context no longer waits on 05.
- [[.scratch/expression-language/project.md]] summary mentions Answer sequence locked. [[.scratch/index.md]] updated for that row.

## Frontier

Open, unblocked, unclaimed, first by number:

- [[.scratch/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context: Node versus text]]
- [[.scratch/expression-language/issues/07-statements-in-this-spec.md|Statements in this spec]]
- [[.scratch/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]]
- [[.scratch/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]
- [[.scratch/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]]
