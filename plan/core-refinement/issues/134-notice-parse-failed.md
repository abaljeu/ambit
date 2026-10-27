# 134 — Notice.ParseFailed

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

A parse posts `Notice.NodeParsing` before it changes the node, and `Notice.NodeParsed` after it succeeds. When the parse fails after `NodeParsing`, no Notice closes it.

Sites that post `NodeParsing` and can then fail:

- `ParseThread.parseFile` and `ParseThread.parseDirectory`, through `postParseOps`, when the Change post fails
- `LazyLoadReconciliationServer.reconcileReceivedFiles`, for `parsedDirIds`, when the Change post fails

## 4. Change

- Add `Notice.ParseFailed of nodeId: NodeId * message: string`, beside `Notice.PersistFailed`, with its `EventJson` encoding.
- Each site above posts `Notice.ParseFailed` for each node it posted `NodeParsing` for, with the failure message, when the parse fails. It posts no `NodeParsed` for that node.
- The producer builds the Notice from the ids it already has. No Notice is inferred from ops.

Source: `History.fs`, `EventJson.fs`, `ParseThread.fs`, `LazyLoadReconciliationServer.fs`, and each exhaustive `Notice` match (`CoreMailboxLoop.fs`, `Database.fs`, `History.fs`).

- `Notice.ParseFailed` leaves the node's document state unchanged.

## 5. Tests

[ ] A file parse whose Change fails posts `NodeParsing`, then `ParseFailed`, and no `NodeParsed`.
[ ] A directory parse whose Change fails posts `NodeParsing`, then `ParseFailed`, and no `NodeParsed`.
[ ] A git Load reconcile whose Change fails posts `ParseFailed` for each `parsedDirIds` entry.
[ ] `EventJson` round-trips `Notice.ParseFailed`.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
