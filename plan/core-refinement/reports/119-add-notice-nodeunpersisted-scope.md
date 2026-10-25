# 119 — Add Notice.NodeUnpersisted scope

Scope for one expand slice: add `Notice.NodeUnpersisted of nodeId`, wire `CoreMailboxLoad.applyNotice`, [EventJson.fs](../../src/Shared/EventJson.fs), and [Database.fs](../../src/Server/Database.fs) `writeNoticeRow`, with no producer. Ticket target: [119 — Add Notice.NodeUnpersisted](../issues/119-add-notice-nodeunpersisted.md) (file not written yet). Pattern: [73 — Add the NodeParsing and NodePersisting Notices](../issues/73-add-the-nodeparsing-and-nodepersisting-notices.md). Blocked by none open; [44 — Apply document state from Node events](../issues/44-apply-document-state-from-file-events.md) is done. Mode: write-ticket. Source unchanged in this pass.

## Confirmations

[ ] `Notice.NodeUnpersisted` is not declared under `src/` today (no matches in `src`).
[ ] The `Notice` union lives in [History.fs](../../src/Shared/History.fs), not [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) (`CoreMsg.fs` holds `CoreMsg`, `Mailbox`, and `PersistHandlers`).
[ ] This slice adds no producer: nothing in production posts `Notice.NodeUnpersisted` through [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) `postNotice`.
[ ] Out of scope for this slice (already true in code): no `SetDocumentState` message case; Parsed state comes from `Notice.NodeParsed`.

## Signatures

[ ] `Gambol.Shared.Notice` in [History.fs](../../src/Shared/History.fs) is `[<RequireQualifiedAccess>]` with cases `NodeParsed`, `NodePersisted`, `MarkUnparsed`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `NodePersisting`, and `PersistFailed` (no `NodeUnpersisted` yet).
[ ] `Gambol.Server.CoreMailboxLoad.applyNotice` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is `internal`; `(persist: PersistHandlers) -> (msg: Notice) -> Result<unit, string>`.
[ ] `Gambol.Server.CoreMailboxLoad.writeUnpersisted` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; `(nodeId: NodeId) -> (live: Graph) -> Result<Graph, string>` and calls `GraphMutate.setPersistState nodeId PersistState.Unpersisted live`.
[ ] `Gambol.Server.CoreEventDispatch.writeAxis` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is `internal`; `(persist: PersistHandlers) -> (notice: Notice) -> (mutate: Graph -> Result<Graph, string>) -> Result<unit, string>`.
[ ] `Gambol.Shared.GraphMutate.setPersistState` in [GraphMutate.fs](../../src/Shared/GraphMutate.fs) is `(nodeId: NodeId) -> (persistState: PersistState) -> (graph: Graph) -> Result<Graph, string>`.
[ ] `Gambol.Shared.EventJson.encodeNotice` in [EventJson.fs](../../src/Shared/EventJson.fs) is private; `(notice: Notice) -> Encode`.
[ ] `Gambol.Shared.EventJson.noticeFrom` in [EventJson.fs](../../src/Shared/EventJson.fs) is private; `(tag: string) -> (nodeId: NodeId) -> Result<Notice, string>`.
[ ] `Gambol.Shared.EventJson.encodeSingleNotice` in [EventJson.fs](../../src/Shared/EventJson.fs) is private; `(tag: string) -> (nodeId: NodeId) -> Encode` (used for single-node notices such as `nodePersisting`).
[ ] `Gambol.Server.Database.writeNoticeRow` in [Database.fs](../../src/Server/Database.fs) is `(connectionString: string) -> (notice: Notice) -> (graph: Graph) -> Task`.
[ ] `Gambol.Server.CoreMailboxApi.postNotice` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `internal`; `(host: MailboxHost) -> (msg: Notice) -> unit` (unchanged; no new caller in this slice).

## Match sites

### `Notice` union case (constructor)

[ ] [History.fs](../../src/Shared/History.fs) — add `NodeUnpersisted of nodeId: NodeId` on `type Notice` beside `NodePersisting`.

### Exhaustive `match` on `Notice` (each needs a `NodeUnpersisted` arm)

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `applyNotice` — `match msg with`; new arm should mirror `Notice.NodePersisting nodeId` (see Copy-from).
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `encodeNotice` — `match notice with`; new arm should use `encodeSingleNotice "nodeUnpersisted" nodeId` (parallel to `NodePersisting`).
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `noticeFrom` — `match tag with`; new arm `"nodeUnpersisted" -> Ok (Notice.NodeUnpersisted nodeId)` (parallel to `"nodePersisting"`).
[ ] [Database.fs](../../src/Server/Database.fs) `writeNoticeRow` — `match notice with`; new arm should call `writePersistState connectionString graph nodeId` like `Notice.NodePersisting nodeId`.

### `applyNotice` callers (no new `Notice` match; pass through)

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `dispatchItem` — on `CoreQueue.Notice msg`, after `CoreEventDispatch.logNotice`, calls `applyNotice context.persist msg`.
[ ] [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) `apply` helper — calls `CoreMailboxLoad.applyNotice persist msg`.
[ ] [PersistThreadTests.fs](../../tests/Server.Tests/PersistThreadTests.fs) local helper — calls `CoreMailboxLoad.applyNotice handlers notice`.

### Notice constructors in production (unchanged in this slice)

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `markUnparsedThenPush`, `writePullingFileUnpersisted`, `writeReceived`, `applyNodeParsed`, `writePersisted` — construct other `Notice` cases only.
[ ] [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) `markForPersist` — constructs `Notice.NodePersisting nodeId` inside `writeAxis` during Change apply (not `NodeUnpersisted`).
[ ] [PersistThread.fs](../../src/Server/Core/PersistThread.fs), [ParseThread.fs](../../src/Server/ParseThread.fs), [Api.fs](../../src/Server/Api.fs), [LazyLoadReconciliationServer.fs](../../src/Server/LazyLoadReconciliationServer.fs) — post existing notice cases via `CoreMailboxApi.postNotice`; no `NodeUnpersisted` today.

### Partial `Notice` matches (no compiler arm required)

[ ] [SyncLogic.fs](../../src/Shared/SyncLogic.fs), server and shared tests, and [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) — match subsets of `Notice` or `EventBody.Notice`; they stay valid when a new case is added.

## Existing tests

[ ] [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) ``NodePersisting leaves the Edit's changedVersion`` — `applyNotice` with `Notice.NodePersisting`; asserts `PersistState.Unpersisted`, unchanged `changedVersion`, Parsed parse state, no push.
[ ] [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) ``NodePersisting matches the Unpersisted Set`` — `applyNotice` with `Notice.NodePersisting`; compares node to `Graph.setPersistState id PersistState.Unpersisted` on the same graph.
[ ] [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) ``NodeParsing changes nothing`` — mailbox `postNotice` for `Notice.NodeParsing`; parse and persist state unchanged (73 pattern for no-op notice).
[ ] [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) ``axis write failure is returned`` — direct `CoreMailboxLoad.applyNotice` for missing node and invalid `NodePersisted` target.
[ ] [DbAgentNoticeTests.fs](../../tests/Server.Tests/DbAgentNoticeTests.fs) ``NodePersisting writes persist state to the database`` — mailbox apply of `Notice.NodePersisting`; DB row `persist_state` becomes Unpersisted.
[ ] [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs) ``Notice JSON round trip`` — round-trips a fixed list of notices including `Notice.NodePersisting nodeId` through `EventJson` encode/decode.
[ ] [PersistThreadTests.fs](../../tests/Server.Tests/PersistThreadTests.fs) ``persist thread posts the version it read`` and related tests — use `applyNotice` helper for persist notices; none name `NodeUnpersisted` today.
[ ] No existing test is named ``applyNotice marks Unpersisted`` or references `Notice.NodeUnpersisted`.

## Copy-from

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `applyNotice` — arm `Notice.NodePersisting nodeId`: `CoreEventDispatch.writeAxis persist msg (fun live -> writeUnpersisted nodeId live)`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `writeUnpersisted` — shared graph write for Unpersisted persist state (same helper the `NodePersisting` arm uses).
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `encodeNotice` / `noticeFrom` — `Notice.NodePersisting` / `"nodePersisting"` single-node wire shape via `encodeSingleNotice` and `decodeSingleNotice`.
[ ] [Database.fs](../../src/Server/Database.fs) `writeNoticeRow` — arm `Notice.NodePersisting nodeId` → `writePersistState connectionString graph nodeId`.
[ ] [73 — Add the NodeParsing and NodePersisting Notices](../issues/73-add-the-nodeparsing-and-nodepersisting-notices.md) — expand-only ticket: add cases, handlers, JSON, DB row, tests; defer producers to a later ticket.

## write-ticket

### Source files this slice would change

[ ] [History.fs](../../src/Shared/History.fs) — add `Notice.NodeUnpersisted of nodeId: NodeId`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) — `applyNotice` arm for `Notice.NodeUnpersisted` (same behavior as `Notice.NodePersisting`).
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) — `encodeNotice`, `noticeFrom` tag `"nodeUnpersisted"`.
[ ] [Database.fs](../../src/Server/Database.fs) — `writeNoticeRow` arm for `Notice.NodeUnpersisted`.

### Tests this slice would add or change

[ ] Add ``applyNotice marks Unpersisted`` (or equivalent name on [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs)) — call `CoreMailboxLoad.applyNotice` with `Notice.NodeUnpersisted nodeId`; assert the node’s `persistState` is `PersistState.Unpersisted` and parse state is unchanged (mirror ``NodePersisting matches the Unpersisted Set``).
[ ] Change [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs) ``Notice JSON round trip`` — include `Notice.NodeUnpersisted nodeId` in the notices list.
[ ] Optional follow-on in [DbAgentNoticeTests.fs](../../tests/Server.Tests/DbAgentNoticeTests.fs) — parallel to ``NodePersisting writes persist state to the database`` for `Notice.NodeUnpersisted` and `writeNoticeRow` (same projection as `NodePersisting`).
