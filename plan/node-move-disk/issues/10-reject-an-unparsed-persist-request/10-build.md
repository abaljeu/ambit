# 10 — Reject an unparsed persist request

Ticket: [10 — Reject an unparsed persist request](10-reject-an-unparsed-persist-request-ticket.md)

Build has not run. Status is `defined`.

The scope is a write-ticket record. The planned edit is:

- `src/Server/Core/PersistThread.fs` — `runRequest` calls `unparsedNode` on `state.graph` and `request.nodeId` after `currentState` and before the unpersisted write. An Unparsed node never reaches `persistNode` or `Notice.NodePersisting`.
- `PersistThread.runRequest` already skips a Persisted node. The Unparsed guard uses that same shape.
- `tests/Server.Tests/PersistThreadTests.fs` — one fact: an Unparsed, Unpersisted special node produces zero writes and zero notices.
