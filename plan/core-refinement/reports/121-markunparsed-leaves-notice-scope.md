# 121 — MarkUnparsed leaves Notice scope

Scope for slice **one contract: remove `Notice.MarkUnparsed` and the `CoreMailboxLoad.applyNotice` arm**, for [121 — MarkUnparsed leaves Notice](../issues/121-markunparsed-leaves-notice.md). Mode: `write-ticket`. Blocked by [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md). This slice does not remove `Op.SetDocumentState`. This slice does not add `Notice.NodeUnpersisted`. Out of this ticket: no `SetDocumentState` message case (already absent); Parsed comes from `Notice.NodeParsed` (already implemented on `CoreMailboxLoad.applyNotice`).

## Signatures

[ ] `Gambol.Shared.Notice` in [History.fs](../../src/Shared/History.fs) includes `MarkUnparsed of nodeId: NodeId` among the other Node and persist cases.
[ ] `Gambol.Shared.EventBody.Notice of Notice` in [History.fs](../../src/Shared/History.fs) wraps a `Notice` for the event log.
[ ] `Gambol.Server.CoreMailboxLoad.applyNotice` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is `applyNotice (persist: PersistHandlers) (msg: Notice) : Result<unit, string>`.
[ ] `Gambol.Server.CoreMailboxLoad.writeParseState` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is private; `(persist: PersistHandlers) -> (notice: Notice) -> (nodeId: NodeId) -> (parseState: ParseState) -> Result<unit, string>`.
[ ] `Gambol.Server.CoreMailboxLoad.markUnparsedThenPush` in [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) is `(persist: PersistHandlers) -> (rawPush: NodeId -> unit) -> (nodeId: NodeId) -> Result<unit, string>`.
[ ] `Gambol.Server.CoreEventDispatch.writeAxis` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is `writeAxis (persist: PersistHandlers) (notice: Gambol.Shared.Notice) (mutate: Graph -> Result<Graph, string>) : Result<unit, string>`.
[ ] `Gambol.Server.CoreEventDispatch.logNotice` in [CoreEventDispatch.fs](../../src/Server/Core/CoreEventDispatch.fs) is `logNotice (context: Context) (notice: Gambol.Shared.Notice)` and appends `EventBody.Notice notice`.
[ ] `Gambol.Server.PersistHandlers.replaceGraph` in [CoreMsg.fs](../../src/Server/Core/CoreMsg.fs) is `Notice -> Graph -> unit`.
[ ] `Gambol.Shared.EventJson.encodeNotice` in [EventJson.fs](../../src/Shared/EventJson.fs) is private; `encodeNotice (notice: Notice)` maps each case to JSON.
[ ] `Gambol.Shared.EventJson.noticeFrom` in [EventJson.fs](../../src/Shared/EventJson.fs) is private; `noticeFrom (tag: string) (nodeId: NodeId) : Result<Notice, string>` decodes the `"notice"` tag.
[ ] `Gambol.Server.Database.writeNoticeRow` in [Database.fs](../../src/Server/Database.fs) is `writeNoticeRow (connectionString: string) (notice: Notice) (graph: Graph) : Task`.
[ ] `Gambol.Server.CoreMailboxApi.postNotice` in [CoreMailboxApi.fs](../../src/Server/Core/CoreMailboxApi.fs) is `postNotice (host: MailboxHost) (msg: Notice)` and enqueues `CoreQueue.Notice msg`.
[ ] `Gambol.Server.ParseThreadDeps.markUnparsed` in [ParseThread.fs](../../src/Server/ParseThread.fs) is `NodeId -> unit` on the deps record (producer removed in [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md)).

## Match sites

### `Notice.MarkUnparsed` case definition

[ ] [History.fs](../../src/Shared/History.fs) `Notice` discriminated union declares `MarkUnparsed of nodeId: NodeId`.

### `CoreMailboxLoad.applyNotice`

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `applyNotice` — arm `Notice.MarkUnparsed nodeId` calls `writeParseState persist msg nodeId ParseState.Unparsed`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `dispatchItem` — `CoreQueue.Notice msg` calls `CoreEventDispatch.logNotice` then `applyNotice context.persist msg` (any posted `Notice.MarkUnparsed` reaches that arm today).

### `CoreMailboxLoad.markUnparsedThenPush` (direct axis, not `applyNotice`)

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `markUnparsedThenPush` — passes `Notice.MarkUnparsed nodeId` into `writeParseState` (then `writeAxis` and `replaceGraph`); it does not call `applyNotice`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `pushReporting` — calls `markUnparsedThenPush handlers rawPush nodeId`.
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `makeMailBox` — binds `parsePush` to `markUnparsedThenPush handlers rawPush` and exposes it as the parse-push callback.

### `EventJson`

[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `encodeNotice` — arm `Notice.MarkUnparsed nodeId` uses `encodeSingleNotice "markUnparsed" nodeId`.
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) `noticeFrom` — tag `"markUnparsed"` returns `Ok (Notice.MarkUnparsed nodeId)`.

### `Database.writeNoticeRow`

[ ] [Database.fs](../../src/Server/Database.fs) `writeNoticeRow` — `Notice.NodeParsed nodeId` and `Notice.MarkUnparsed nodeId` share one arm and call `writeDocumentState`.

### Producers of `Notice.MarkUnparsed`

[ ] [ParseThread.fs](../../src/Server/ParseThread.fs) `ParseThread.bootCore` — `markUnparsed` closure calls `CoreMailboxApi.postNotice core.host (Notice.MarkUnparsed nodeId)` (target of [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md)).
[ ] [ParseThread.fs](../../src/Server/ParseThread.fs) `ParseThread.afterPost` — `List.iter deps.markUnparsed` on dependency ids after a successful directory reconcile post (uses the `bootCore` closure above).
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `markUnparsedThenPush` — constructs `Notice.MarkUnparsed nodeId` for `writeParseState` / `writeAxis` (stays after [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md) until this slice removes the case).
[ ] [ParseThreadLoadTests.fs](../../tests/Server.Tests/ParseThreadLoadTests.fs) — test `markUnparsed` stubs call `CoreMailboxApi.postNotice host (Notice.MarkUnparsed nodeId)` or `noticeLog.Add (Notice.MarkUnparsed nodeId)` at multiple sites (lines near 492, 856, 924, 1148).
[ ] [LoadSaveCommandTests.fs](../../tests/Server.Tests/LoadSaveCommandTests.fs) — parse boot `markUnparsed` posts `Notice.MarkUnparsed` through `CoreMailboxApi.postNotice`.

### Blocker and remaining producers after [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md)

[ ] [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md) **Status** is `defined` (still open).
[ ] [44 — Apply document state from Node events](../issues/44-apply-document-state-from-file-events.md) **Status** is `done` (blocker for 56 is satisfied).
[ ] After [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md), `src/Server/ParseThread.fs` no longer posts `Notice.MarkUnparsed`; `CoreMailboxLoad.markUnparsedThenPush` and tests that still construct `Notice.MarkUnparsed` remain until this slice.

### Comments naming `MarkUnparsed`

[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) — comment block above `applyNotice` includes the line `MarkUnparsed writes Unparsed only.`

## Existing tests

[ ] ``MarkUnparsed sets Unparsed and leaves PersistState`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) — calls private `apply`, which wraps `CoreMailboxLoad.applyNotice`, with `Notice.MarkUnparsed id`; asserts parse state `Unparsed`, document enum `Unparsed`, persist state unchanged, and no persist push.
[ ] ``MarkUnparsed writes parse state to the database`` in [DbAgentNoticeTests.fs](../../tests/Server.Tests/DbAgentNoticeTests.fs) — posts `Notice.MarkUnparsed id` through the live mailbox after `Notice.NodeParsed`; asserts DB parse state becomes `Unparsed` and persist state and update time stay unchanged.
[ ] ``Notice JSON round trip`` in [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs) — notice list includes `Notice.MarkUnparsed nodeId` and round-trips each notice through `EventJson.encode` / decode.
[ ] [ParseThreadLoadTests.fs](../../tests/Server.Tests/ParseThreadLoadTests.fs) — several scenarios wire `markUnparsed` to post or record `Notice.MarkUnparsed` (not dedicated test names; exercises ParseThread deps).
[ ] [LoadSaveCommandTests.fs](../../tests/Server.Tests/LoadSaveCommandTests.fs) — Load/save harness posts `Notice.MarkUnparsed` via `CoreMailboxApi.postNotice` in `markUnparsed`.
[ ] No existing test is named ``MarkUnparsed leaves Notice`` (that name is acceptance for this ticket only).
[ ] ``NodeParsed pushes a persist request`` and related `Notice.NodeParsed` tests in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs) already cover Parsed from `Notice.NodeParsed` via `applyNotice` (out of scope for this slice’s new behavior).

## Copy-from

[ ] Prior Notice-case removal contract: [84 — Remove the NodeStamped case](../issues/84-remove-the-nodestamped-case.md) (**Status** `done`).
[ ] Same file set pattern: delete the case from [History.fs](../../src/Shared/History.fs); update exhaustive matches in [EventJson.fs](../../src/Shared/EventJson.fs) (`encodeNotice`, decode tag), [Database.fs](../../src/Server/Database.fs) `writeNoticeRow`, and [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `applyNotice`.
[ ] [84 — Remove the NodeStamped case](../issues/84-remove-the-nodestamped-case.md) test pattern: drop the case from ``Notice JSON round trip`` in [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs); add a decode-failure test for the retired JSON tag (see ``An old nodeStamped row is skipped on read`` in the same file); delete notice-specific core-loop tests that applied the removed case.
[ ] [84 — Remove the NodeStamped case](../issues/84-remove-the-nodestamped-case.md) legacy rows: old `"nodeStamped"` notice JSON fails decode; readers skip or reject undecodable rows (same approach applies to `"markUnparsed"` for this slice).
[ ] This slice does not rename `PersistHandlers` fields (unlike `noteStamped` → `notePersisted` in [84 — Remove the NodeStamped case](../issues/84-remove-the-nodestamped-case.md)).

## write-ticket

### Source files this slice would change

[ ] [History.fs](../../src/Shared/History.fs) — remove `Notice.MarkUnparsed`.
[ ] [EventJson.fs](../../src/Shared/EventJson.fs) — remove encode arm and `"markUnparsed"` decode from `noticeFrom`.
[ ] [Database.fs](../../src/Server/Database.fs) — `writeNoticeRow` matches only `Notice.NodeParsed` for document-state write (drop shared `MarkUnparsed` arm).
[ ] [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) — remove `applyNotice` `MarkUnparsed` arm; update `markUnparsedThenPush` so it no longer constructs `Notice.MarkUnparsed`; update comments above `applyNotice`.
[ ] [ParseThread.fs](../../src/Server/ParseThread.fs) — no `Notice.MarkUnparsed` posts remain when [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md) lands first (verify at implement time).

### Tests this slice would add or change

[ ] Delete or replace ``MarkUnparsed sets Unparsed and leaves PersistState`` in [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs).
[ ] Delete or replace ``MarkUnparsed writes parse state to the database`` in [DbAgentNoticeTests.fs](../../tests/Server.Tests/DbAgentNoticeTests.fs).
[ ] Change ``Notice JSON round trip`` in [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs) — drop `Notice.MarkUnparsed` from the notice list.
[ ] Add ``MarkUnparsed leaves Notice`` (acceptance name) — `Gambol.Shared.Notice` has no `MarkUnparsed` case; `CoreMailboxLoad.applyNotice` has no `MarkUnparsed` arm; `EventJson` has no `"markUnparsed"` tag (mirror [84 — Remove the NodeStamped case](../issues/84-remove-the-nodestamped-case.md) coverage style).
[ ] Add an old-row decode test for legacy `"markUnparsed"` notice JSON (mirror ``An old nodeStamped row is skipped on read`` in [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs)).
[ ] Update [ParseThreadLoadTests.fs](../../tests/Server.Tests/ParseThreadLoadTests.fs) and [LoadSaveCommandTests.fs](../../tests/Server.Tests/LoadSaveCommandTests.fs) — remove `Notice.MarkUnparsed` from `markUnparsed` stubs once the case is gone; align with [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md) if those files change there first.
