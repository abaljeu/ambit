# 121 — MarkUnparsed renamed NodeUnparsed scope

Scope for [121 — MarkUnparsed renamed NodeUnparsed](../issues/121-markunparsed-renamed-nodeunparsed.md). Mode: `write-ticket`. Blockers: none.

## Owner decisions

[ ] Alan, 2026-10-26: the case stays under the name `Notice.NodeUnparsed`
[ ] `Notice.NodeUnparsed` is posted before the parse, when a node is marked Unparsed and pushed for parsing
[ ] `Notice.NodeParsed` is posted after the nodes are done parsing
[ ] `encodeNotice` writes the tag `nodeUnparsed`
[ ] `noticeFrom` reads `nodeUnparsed` and `markUnparsed` as `Notice.NodeUnparsed`, so existing logs and database rows load
[ ] [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md) is not a blocker

## Constructors on staging

[ ] [CoreMailboxFilesAxis.fs](../../src/Server/Core/CoreMailboxFilesAxis.fs) `markUnparsedThenPush` builds `Notice.MarkUnparsed nodeId`, then calls `rawPush`
[ ] [ParseThread.fs](../../src/Server/ParseThread.fs) `bootCore` builds `Notice.MarkUnparsed nodeId` in the `markUnparsed` closure and posts it with `CoreMailboxApi.postNotice`
[ ] [DocumentOpImpact.fs](../../src/Shared/DocumentOpImpact.fs) `noticesBeside` builds `Notice.MarkUnparsed id` when the new state is Unparsed and the old state is not PullingFile
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `noticeFrom` builds `Notice.MarkUnparsed nodeId` for the tag `markUnparsed`

The ticket names the match arms, the comments, and the tests.
