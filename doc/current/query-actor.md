# Query Actor
Category: Capability
See Also:
- [Actors](actors.md)
- [Search Actor](search-actor.md)
- [Workspace graph](graph.md)

A query expression runs on its own Actor.

## Sources

[ExprRun](../../src/Shared/ExprRun.fs) — `ChildNode.reference` on the materialise path

## Data

[ ] One result, then stop. No page and no continuation cursor.
[ ] Limit: the function's own limit applies when it is under 200. A request above 200 stops at 200.
[ ] Trash: ordinary eval skips trash. The function `trash` reaches trash the way `root` reaches ROOT.

## Job

[ ] The server evaluates the query once, when the line runs.
[ ] This Actor does not use the Find and Move Actor or the quiet gap. Detail: [Search Actor](search-actor.md).

## Interface

[ ] Eval: one evaluation of the expression on the server Graph when the line runs.
[ ] Refs: each result under the query line is a Ref, `ChildNode.reference`, not an Owned Child.

## Messages

[ ] Eval: one server evaluation when the line runs. A keypress does not start this Actor.
[ ] Ref child: `ChildNode.reference`, the shape [ExprRun](../../src/Shared/ExprRun.fs) uses to materialise a Node answer.

## Uses

[ ] Own Actor: this Actor starts when the line runs.
[ ] ExprRun shape: `ChildNode.reference`. Detail: [ExprRun](../../src/Shared/ExprRun.fs).

## Seams

[ ] Cap: the server stops at 200. A query function may stop lower.
[ ] Quiet gap: this Actor does not use the Find and Move quiet gap. Detail: [Search Actor](search-actor.md).

## Explanation

A request above 200 still stops at 200, so a query cannot return the whole Graph. The number is the [Search Actor](search-actor.md) cap.
