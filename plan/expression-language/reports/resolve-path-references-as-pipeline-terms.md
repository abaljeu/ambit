# Resolve Path references as pipeline terms

Recorded the HITL decision on [[plan/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]]. Status is resolved. CONTEXT.md was left unchanged.

## Changed

- Ticket Answer: every path operator is one Prolog-style predicate of its left-hand Nodes (0, 1, or many Answers; miss is fail, not an error — including out-of-range `:n` / `!n`, glob, and name / `#` / `/`). `#x` searches down (no bare `#`). `/` keeps Directory Node and Workspace Node. `**` is the same idea as `descendant`. `:*` / `!*` replace bare `:` / `!`.
- Map [[plan/expression-language/map.md]] Decisions so far: one gist line, title wrapping the wikilink, including fail-to-answer.
- [[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]] is unblocked (`Blocked by: none`). Pointer sentence added. Status stays open.
- New open grilling ticket [[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]] (unblocked). That fog line left the map. Not resolved. No further ticket for the empty-miss addendum.
- [[plan/expression-language/spec-draft.md]] path-operator table marked Locked, including fail-to-answer.
- [[plan/expression-language/reports/pipeline-examples.md]] adds `//ws/x`, `#blue` versus `^#blue`, `:*`, and `!-249053534` (zero Answers). [[plan/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]] stays open.
- [[plan/expression-language/project.md]] summary mentions path operators locked. [[plan/index.md]] updated for that row.

## Frontier

Open, unblocked, unclaimed, first by number:

- [[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]]
- [[plan/expression-language/issues/04-boolean-operators-as-control.md|Boolean operators as control]]
- [[plan/expression-language/issues/05-how-multiple-answers-surface.md|How multiple answers surface]]
- [[plan/expression-language/issues/07-statements-in-this-spec.md|Statements in this spec]]
- [[plan/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]]
- [[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]
- [[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]]
