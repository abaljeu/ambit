# Owned versus Ref walk for descendant

Type: grilling
Status: resolved
Blocked by: none

## Question

[[plan/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]] locks `**` as today’s `**` and as the same idea as postfix `descendant`. Today’s implementation (`pathScopeDescendants` in [[src/Shared/RefExprMatch.fs]]) walks Owned Children only and stops at Directory Node and Workspace Node. The interpretation doc also says named search excludes Ref. Other path steps follow Ref appearances (tests lock that). Does `descendant` / `**` walk Owned Children only and stop at Directory Node and Workspace Node, or also follow Ref?

Recommended answer (HITL confirm): keep today’s `**` walk — Owned only, stop at Directory Node and Workspace Node — and use that walk for `descendant`.

## Comments

HITL on [[08-prototype-pipeline-examples.md]] 2026-08-27: first reaction said `descendant` walks Owned Nodes only, same as `**`. Later amendment on that page supersedes it; see Answer.

## Answer

HITL 2026-08-27. Words follow the model element [[CONTEXT.md]] Children (Owned and Ref).

- `child` finds Children: Owned and Ref.
- `descendant` is the closure of `child`. It follows Ref.
- `tree` is acyclic, so it does not follow Ref. `// tree` yields the transitively Owned Nodes from ROOT.

This revises [[08-prototype-pipeline-examples.md]]’s first `descendant` line (Owned-only, same as `**`) and the `**` / `descendant` alias on [[02-path-references-as-pipeline-terms.md|Path references as pipeline terms]] and [[03-first-primitive-catalog.md|First primitive catalog]].

`**` is `tree`: transitively Owned Nodes, no Directory/Workspace stop. It is not `descendant`. Today’s implemented `**` stops at Directory Node and Workspace Node; this spec revises that walk.

HITL 2026-08-28. The final content-search decision separates `#` from `named`.

- `#` takes a required name and searches strictly below each input through Children, both Owned and Ref. It visits Nodes depth-first in Children order and deduplicates by Node identity within each search; first reach wins. Existing named-Normal and structural content-search walls stay in force. In `a#b#c`, `#b` searches below each Answer from `a`, then `#c` searches below each resulting `b`.
- `named "x"` is a pure `Node ⇒ Node` filter. It yields its input Node when that Node’s name matches `x`; otherwise it yields no Answers. It does not walk Children, so the Owned-versus-Ref question does not apply.
