# Owned versus Ref walk for descendant

Type: grilling
Status: open
Blocked by: none

## Question

[[.scratch/expression-language/issues/02-path-references-as-pipeline-terms.md|Path references as pipeline terms]] locks `**` as today’s `**` and as the same idea as postfix `descendant`. Today’s implementation (`pathScopeDescendants` in [[src/Shared/RefExprMatch.fs]]) walks Owned Children only and stops at Directory Node and Workspace Node. The interpretation doc also says named search excludes Ref. Other path steps follow Ref appearances (tests lock that). Does `descendant` / `**` walk Owned Children only and stop at Directory Node and Workspace Node, or also follow Ref?

Recommended answer (HITL confirm): keep today’s `**` walk — Owned only, stop at Directory Node and Workspace Node — and use that walk for `descendant`.
