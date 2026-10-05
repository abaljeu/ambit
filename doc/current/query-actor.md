# Query Actor

Category: Capability

See Also:

[Actors](actors.md)
[Search Actor](search-actor.md)
[Workspace graph](workspace-graph.md)

A query expression runs on its own Actor.

## Sources

[ExprRun](../../src/Shared/ExprRun.fs)

## State

[ ] One result: one result, then stop. No page and no continuation cursor.
[ ] Limit: the function's own limit applies when it is under 200. A request above 200 stops at 200. That cap is the [Search Actor](search-actor.md) cap.
[ ] Trash: ordinary eval skips trash. The function `trash` reaches trash the way `root` reaches ROOT.

## Interface

[ ] Eval: the Actor evaluates the expression on the server Graph once, when the line runs. A keypress does not start this Actor.
[ ] Refs: results under the query line are Refs, not Owned Children. The child is `ChildNode.reference`, as [ExprRun](../../src/Shared/ExprRun.fs) materialises a Node answer.
[ ] Insert: the query expression inserts the N items the server finds, as Refs under the query line. N stops at the limit.

## Uses

[ ] Own Actor: this Actor starts when the line runs.
[ ] Search Actor: this Actor does not use the Find and Move Actor or that quiet gap. Detail: [Search Actor](search-actor.md).

## Seams

[ ] Cap: the server stops at 200. A query function may stop lower.
[ ] Quiet gap: this Actor does not use the Find and Move quiet gap. Detail: [Search Actor](search-actor.md).
