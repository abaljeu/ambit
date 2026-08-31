# Resolve How multiple answers surface

Recorded the HITL lock on [[plan/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]]. Status is resolved.

## Changed

- Ticket Answer: Answers are a sequence (0/1/many). Eval engine is implementation (fastest that preserves Answers and order). Juxtaposition left-to-right. `OR` concatenates and may repeat. `AND` is left-predicate order, at most once. Materialise as Children is a consumer on [[plan/expression-language/issues/06-top-level-context-node-versus-text.md]].
- [[plan/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context: Node versus text]] unblocked (`Blocked by: none`). Pointer that 05 locked the sequence.
- [[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] Answer: pointer for `OR`/`AND` sequence rules.
- Map [[plan/expression-language/map.md]] Decisions so far: one gist line.
- [[plan/expression-language/spec-draft.md]] Multiple Answers marked Locked. Top-level context no longer waits on 05.
- [[plan/expression-language/project.md]] summary mentions Answer sequence locked. [[plan/index.md]] updated for that row.

## Frontier

Open, unblocked, unclaimed, first by number:

- [[plan/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context: Node versus text]]
- [[plan/expression-language/issues/07-statements-in-this-spec.md|Statements in this spec]]
- [[plan/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]]
- [[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]
- [[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]]
