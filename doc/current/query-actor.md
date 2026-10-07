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

[x] One result, then stop. No page and no continuation cursor.
[ ] Limit: the function's own limit applies when it is under 200. A request above 200 stops at 200.
[ ] Trash: ordinary eval skips trash. The function `trash` reaches trash the way `root` reaches ROOT.

## Job

[x] The server evaluates the query once, when the line runs.
[x] This Actor does not use the Find and Move Actor or the Find globe. Detail: [Search Actor](search-actor.md).

## Interface

[x] Eval: one evaluation of the expression on the server Graph when Run hits a Node whose text contains `=`. The request is the existing Run ActorStart (`zoomId`, `focusId`, `commandId`, `graphIds`, `eventId`) on `POST /ambit/command`. The observable reply is Node ids on ActorStop (`ActorQuery`). Detail: [11 — Server evaluates](../../plan/online-search/issues/11-server-evaluates.md).
[ ] Refs: each result under the query line is a Ref, `ChildNode.reference`, not an Owned Child. That post is a proposed design. [16 — Insert Refs under the query line](../../plan/online-search/issues/16-insert-refs-under-the-query-line.md) owns it.

## Messages

[x] Eval. The Actor starts when the line runs. A keypress does not start this Actor.
[x] Start. Run on a Node whose text contains `=`. The request is the existing ActorStart. `focusId` is that line when Run is on it. `commandId` is that same Node.
[x] Eval reply. The result is Node ids on ActorStop (`ActorQuery`). This reply is the server evaluation. The command response stays the existing Run response. The client reads the ids from that event.
[ ] Result item. Proposed design. Each result under the query line is one `ChildNode.reference`. `ref` is the string `ref`. `id` is a Node id string. [16 — Insert Refs under the query line](../../plan/online-search/issues/16-insert-refs-under-the-query-line.md) owns this post. It is not the eval reply.

```json
{ "ref": "ref", "id": "550e8400-e29b-41d4-a716-446655440000" }
```

## Uses

[x] Own Actor: this Actor starts when the line runs.
[ ] ExprRun shape: `ChildNode.reference`. Detail: [ExprRun](../../src/Shared/ExprRun.fs).

## Seams

[ ] Cap: the server stops at 200. A query function may stop lower.
[x] Globe: this Actor does not use the Find globe. Detail: [Search Actor](search-actor.md).

## Explanation

A request above 200 still stops at 200, so a query cannot return the whole Graph. The number is the [Search Actor](search-actor.md) cap.
