# Resolve Owned versus Ref walk for descendant

Recorded the HITL lock on [[plan/expression-language/issues/12-owned-versus-ref-walk-for-descendant.md|Owned versus Ref walk for descendant]]. Status is resolved. Same HITL also amended [[plan/expression-language/issues/08-prototype-pipeline-examples.md|Prototype: pipeline examples]].

## Changed

- Ticket Answer: `child` finds Children (Owned and Ref). `descendant` is the closure of `child` (follows Ref). `tree` is acyclic Owned-only; `// tree` from ROOT. `**` stays today’s `**` and is not `descendant`.
- [[plan/expression-language/reports/pipeline-examples.md]]: `// OR /` undefined; bare `3` type error; added `child` and `// tree`; `root descendant` is child-closure.
- [[plan/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]]: `/` is not a prefix; `**` is not `descendant`; number only on the right of `:` or `!`.
- [[plan/expression-language/issues/03-first-primitive-catalog.md|First primitive catalog]]: `child`, `descendant`, `tree` in the closed word set.
- [[plan/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context]]: number literal only on the right of `:` or `!`.
- Map [[plan/expression-language/map.md]]: 02, 03, 08 gists updated; 12 gist added. Not yet specified: whether `**` is `tree` or keeps today’s Directory/Workspace stop.
- [[plan/expression-language/spec-draft.md]] Path operators, catalog, top-level, examples.

## Frontier

Open, unblocked, unclaimed, first by number:

- [[plan/expression-language/issues/11-keep-or-drop-amble-of-and-comma.md|Keep or drop Amble of and comma]]
