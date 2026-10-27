# 01 — Notice.ParseFailed scope

Scope for [134 — Notice.ParseFailed](../issues/134-notice-parse-failed.md). Branch `ambot2/134-parse-failed`, rebased onto ticket 126 (`planParseFile` drops `SetDocumentState`). Present code has no `Notice.ParseFailed`.

### Signatures

[ ] `Notice` in [History.fs](../../src/Shared/History.fs): nine cases through `PersistFailed of nodeId: NodeId * message: string`; no `ParseFailed`.

```38:49:src/Shared/History.fs
type Notice =
    | NodeParsed of nodeId: NodeId
    | NodePersisted of
        nodeId: NodeId *
        persistedVersion: EventId *
        updateTime: System.DateTime
    | NodeUnparsed of nodeId: NodeId
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeParsing of nodeId: NodeId
    | NodePersisting of nodeId: NodeId
    | PersistFailed of nodeId: NodeId * message: string
```

[ ] `Ev.documentStateFromNotice` in [History.fs](../../src/Shared/History.fs): `NodeParsing`, `NodePersisting`, and `PersistFailed` map to `None` (no document-state write).

[ ] `ParseThreadDeps` in [ParseThread.fs](../../src/Server/ParseThread.fs): `postOps: NodeId -> Op list -> Async<Result<unit, string>>`.

[ ] `ParseThread.postParseOps` in [ParseThread.fs](../../src/Server/ParseThread.fs): `(handle: CoreChangesDoor) -> nodeId: NodeId -> ops: Op list -> Async<Result<unit, string>>`.

[ ] `ParseThread.parseFile` / `ParseThread.parseDirectory` / private `afterPost` / `ParseThread.bootCore` in [ParseThread.fs](../../src/Server/ParseThread.fs).

[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` in [LazyLoadReconciliationServer.fs](../../src/Server/LazyLoadReconciliationServer.fs): `(workspace: WorkspaceReconcile) -> changedPaths: LazyLoadReconciliation.ChangedPath list -> Async<Result<LazyLoadReconciliationReport.Failure list, string>>`.

[ ] `LazyLoadReconciliationServer.WorkspaceReconcile`: `{ handle: CoreChangesDoor; dataDir: string; workspaceLabel: string }`.

[ ] Private reconcile helpers: `postNotices`, `postBeforeChange`, `postAfterChange`, `postReconcileReport`, `reconcileListed` in [LazyLoadReconciliationServer.fs](../../src/Server/LazyLoadReconciliationServer.fs).

[ ] `EventJson.encodeNotice` / `decodeNoticeBody` / `encodePersistFailed` / `decodePersistFailed` in [EventJson.fs](../../src/Shared/EventJson.fs).

[ ] `CoreMailboxLoop.applyNotice` in [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs).

[ ] `Database.writeNoticeRow` in [Database.fs](../../src/Server/Database.fs).

[ ] `ResidentProjection.applyDocumentNotice` delegates to `Ev.documentStateFromNotice` in [ResidentProjection.fs](../../src/Shared/ResidentProjection.fs) (no direct `Notice` match).

[ ] `SyncLogic.persistFailedMessage` and `applyLoggedNotice` match only `PersistFailed` and `NodePersisted` among notices in [SyncLogic.fs](../../src/Shared/SyncLogic.fs).

[ ] `DocumentOpImpact.noticesBeside` in [DocumentOpImpact.fs](../../src/Shared/DocumentOpImpact.fs): derives notices from `Op.SetDocumentState` in a Change; not used for parse-thread notices.

[ ] `CoreDoor.postGraphOnly` in [CoreDoor.fs](../../src/Server/Core/CoreDoor.fs): returns `Error` when `door.writable` is false (`readOnlyMsg`).

### Match sites

[ ] **NodeParsing posted (parse file/directory)** — `ParseThread.postParseOps` calls `CoreMailboxApi.postNotice` with `Notice.NodeParsing nodeId` before the Change.

```156:173:src/Server/ParseThread.fs
    let postParseOps
        (handle: CoreChangesDoor)
        (nodeId: NodeId)
        (ops: Op list)
        : Async<Result<unit, string>> =
        async {
            CoreMailboxApi.postNotice
                handle.host
                (Notice.NodeParsing nodeId)
            if List.isEmpty ops then
                return Ok ()
            else
                let event = GraphOnlyChangePost.mint "Parse" ops
                match! CoreDoor.postGraphOnly handle event with
                | Ok _ -> return Ok ()
                | Error err -> return Error err
        }
```

[ ] **NodeParsed posted (parse thread success)** — `ParseThread.bootCore` `finishParse` posts `Notice.NodeParsed nodeId` after `postOps` returns `Ok ()`; `afterPost` calls `deps.finishParse nodeId` only on `Ok ()`.

```57:76:src/Server/ParseThread.fs
    let private afterPost
        (deps: ParseThreadDeps)
        (nodeId: NodeId)
        (push: NodeId list)
        (posted: Result<unit, string>)
        =
        match posted with
        | Error err -> reportPost err
        | Ok () ->
            push |> List.iter deps.markUnparsed
            enqueueIfRequeue deps.push push
            deps.finishParse nodeId
```

```202:206:src/Server/ParseThread.fs
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        core.host
                        (Notice.NodeParsed nodeId) }
```

[ ] **Change post fails after NodeParsing (parse thread)** — `postParseOps` returns `Error err` from `CoreDoor.postGraphOnly`; `parseFile` / `parseDirectory` pass that to `afterPost`, which logs via `reportPost` and does not call `finishParse` (no `NodeParsed`).

[ ] **NodeParsing posted (git Load reconcile)** — `postBeforeChange` posts `Notice.NodeParsing` for each id in `report.parsedDirIds` (after `postContainerUnparsed` on those ids).

```152:159:src/Server/LazyLoadReconciliationServer.fs
    let private postBeforeChange
        (host: MailboxHost)
        (graph: Graph)
        (report: LazyLoadReconciliationReport.Report)
        =
        postContainerUnparsed host graph report.parsedDirIds
        postNotices host Notice.NodeParsing report.parsedDirIds
```

[ ] **NodeParsed posted (reconcile success)** — `postAfterChange` posts `Notice.NodeParsed` for `report.parsedDirIds @ listed.renamedDirIds` only when `postReconcileReport` reaches the success path.

[ ] **Change post fails after NodeParsing (reconcile)** — `postReconcileReport` runs `postBeforeChange` first; on non-empty `report.ops`, `GraphOnlyChangePost.postChunks` with `CoreDoor.postGraphOnly listed.handle` can return `Error err`; then `postAfterChange` is skipped (no `NodeParsed` for those dirs).

```178:201:src/Server/LazyLoadReconciliationServer.fs
    let private postReconcileReport
        (listed: ListedReconcile)
        (report: LazyLoadReconciliationReport.Report)
        =
        async {
            logFailures listed.workspaceLabel report.failures
            let host = listed.handle.host
            postBeforeChange host listed.graph report
            match report.ops with
            | [] ->
                postAfterChange host listed report
                return Ok report.failures
            | ops ->
                let! result =
                    GraphOnlyChangePost.postChunks
                        (CoreDoor.postGraphOnly listed.handle)
                        "Parse"
                        (GraphOnlyChangeChunks.split ops)
                match result with
                | Ok () ->
                    postAfterChange host listed report
                    return Ok report.failures
                | Error err -> return Error err
        }
```

[ ] **`parsedDirIds` in reconcile** — set by `LazyLoadReconciliationReport.planReceivedFiles` (and related planners); consumed in `postBeforeChange`, `postAfterChange`, and `loadedDirectoryIds` / `childContainerIds` in [LazyLoadReconciliationServer.fs](../../src/Server/LazyLoadReconciliationServer.fs). Producer already holds each `NodeId` when posting `NodeParsing`.

[ ] **EventJson Notice encode/decode** — `encodeNotice` / `noticeFrom` / `decodeNoticeBody` in [EventJson.fs](../../src/Shared/EventJson.fs); `PersistFailed` uses tag `"persistFailed"` and fields `nodeId`, `message` via `encodePersistFailed` / `decodePersistFailed`.

[ ] **`DocumentOpImpact.noticesBeside`** — maps `SetDocumentState` transitions to `NodeUploading`, `NodeReceived`, `NodeUnparsed`, or `NodeParsed`; parse failure must not infer notices from ops here.

[ ] **`SetDocumentState` and parse** — [ImportDocument.fs](../../src/Shared/dotnet/ImportDocument.fs) `planParseFile` emits no `SetDocumentState`; parse Changes rely on `Notice.NodeParsed` for `Current` ([NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) ``planDirectoryReconcile logs NodeParsed beside SetDocumentState``). `postParseOps` posts graph-only Change via `GraphOnlyChangePost.mint "Parse" ops`, not `SetDocumentState` ops from the planner.

[ ] **Exhaustive server/shared `Notice` matches to extend** — `CoreMailboxLoop.applyNotice` ([CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs)), `Database.writeNoticeRow` ([Database.fs](../../src/Server/Database.fs)), `Ev.documentStateFromNotice` ([History.fs](../../src/Shared/History.fs)), `EventJson.encodeNotice` and `decodeNoticeBody` ([EventJson.fs](../../src/Shared/EventJson.fs)). No other production `match` on full `Notice` DU besides nested single-case filters in `SyncLogic` (`PersistFailed`, `NodePersisted`).

```98:128:src/Server/Core/CoreMailboxLoop.fs
    let internal applyNotice
        (persist: PersistHandlers)
        (msg: Notice)
        : Result<unit, string> =
        match msg with
        | Notice.NodeParsed nodeId ->
            CoreMailboxFilesAxis.applyNodeParsed persist nodeId
        | Notice.NodeUploading nodeId ->
            CoreMailboxFilesAxis.writePullingFileUnpersisted persist nodeId
        | Notice.NodeReceived nodeId ->
            CoreMailboxFilesAxis.writeReceived persist nodeId
        | Notice.NodeParsing _ -> Ok ()
        | Notice.NodePersisting nodeId ->
            CoreEventDispatch.writeAxis persist msg (fun live ->
                CoreMailboxFilesAxis.writeUnpersisted nodeId live)
        | Notice.PersistFailed _ ->
            persist.noteWriteFailed ()
            Ok ()
        | Notice.NodeUnparsed nodeId ->
            CoreMailboxFilesAxis.writeParseState
                persist msg nodeId ParseState.Unparsed
        | Notice.NodePersisted (nodeId, persistedVersion, updateTime) ->
            ...
```

```437:447:src/Shared/History.fs
    let internal documentStateFromNotice (notice: Notice) =
        match notice with
        | Notice.NodeUploading nodeId -> Some(nodeId, PullingFile)
        | Notice.NodeReceived nodeId
        | Notice.NodeUnparsed nodeId -> Some(nodeId, Unparsed)
        | Notice.NodeParsed nodeId -> Some(nodeId, Current)
        | Notice.NodeParsing _
        | Notice.NodePersisting _
        | Notice.PersistFailed _
        | Notice.NodePersisted _ -> None
```

```577:596:src/Server/Database.fs
    let writeNoticeRow
        (connectionString: string)
        (notice: Notice)
        (graph: Graph)
        : Task =
        match notice with
        | Notice.NodeParsed nodeId
        | Notice.NodeUnparsed nodeId ->
            writeDocumentState connectionString graph nodeId
        ...
        | Notice.NodeUploading _
        | Notice.NodeParsing _
        | Notice.PersistFailed _ ->
            task { return () }
```

### Existing tests

[ ] ``Notice JSON round trip`` — [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs); public seam `EventJson` encode/decode via `roundTrip`; lists all notice cases except a future `ParseFailed`.

[ ] ``NodeParsing changes nothing`` — [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); `CoreMailboxApi.postNotice` / `CoreMailboxLoop.applyNotice`; graph and persist axes unchanged after `NodeParsing`.

[ ] ``PersistFailed does not change persist state`` — [CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs); `CoreMailboxLoop.applyNotice` with `Notice.PersistFailed`.

[ ] ``PersistFailed leaves the graph`` — [SyncLogicTests.fs](../../tests/Shared.Tests/SyncLogicTests.fs); client `applyTail` on `EventBody.Notice PersistFailed`.

[ ] ``Failed write posts PersistFailed`` — [DbAgentFailureTests.fs](../../tests/Server.Tests/DbAgentFailureTests.fs) and [FileAgentFailureTests.fs](../../tests/Server.Tests/FileAgentFailureTests.fs); `throwingPersist` injects failure; public seam `CoreMailboxApi.getEventsSince` / event log poll.

[ ] ``planParseFile posts NodeParsing, the Change, then NodeParsed`` — [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs); `ParseThread.start` with `ParseThread.postParseOps`, `orderedEvents`, `assertParsingThenParsed`.

[ ] ``planDirectoryReconcile posts NodeParsing, the Change, then NodeParsed once`` — same file; directory push through parse stack.

[ ] ``git Load reconcile posts NodeUnparsed then the Change then NodeParsed`` — [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs); public seam `LazyLoadReconciliationServer.reconcileReceivedFiles` via `pullReceived`.

[ ] ``git load reconcile posts NodeParsing before and NodeParsed after the dir-info parse`` — same file; `assertParsingThenParsed` after reconcile.

[ ] No existing test asserts behavior when parse Change post returns `Error` after `NodeParsing` (file, directory, or reconcile).

### Copy-from

[ ] **Post failure notice** — [PersistThread.fs](../../src/Server/Core/PersistThread.fs) `runRequest`: on persist `Error message`, `deps.postNotice (Notice.PersistFailed(request.nodeId, message))`.

```40:49:src/Server/Core/PersistThread.fs
            match written with
            | Ok updateTime ->
                deps.postNotice (
                    Notice.NodePersisted(
                        request.nodeId,
                        state.eventId,
                        updateTime))
            | Error message ->
                deps.postNotice (
                    Notice.PersistFailed(request.nodeId, message))
```

[ ] **EventJson for paired notice** — [EventJson.fs](../../src/Shared/EventJson.fs) `encodePersistFailed` / `decodePersistFailed` / `decodeNoticeBody` branch `"persistFailed"`.

[ ] **Observe failure notice in tests** — [DbAgentFailureTests.fs](../../tests/Server.Tests/DbAgentFailureTests.fs) `persistFailed` helper filters `EventBody.Notice (Notice.PersistFailed (nodeId, message))`; ``Failed write posts PersistFailed`` polls `getEventsSince` until the notice appears.

[ ] **Apply axis for no-op failure notice** — [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) `Notice.PersistFailed _` calls `persist.noteWriteFailed ()` and returns `Ok ()`; graph document state unchanged via `documentStateFromNotice`.

### implement

[ ] Ticket adds `Notice.ParseFailed of nodeId: NodeId * message: string` beside `PersistFailed`; code has no such case.

[ ] Ticket posts `ParseFailed` at each site that posted `NodeParsing` when the Change post fails; today `afterPost` only `eprintfn`s and reconcile `postReconcileReport` returns `Error` with no closing notice.

[ ] Ticket posts no `NodeParsed` for that node on failure; present code already skips `finishParse` / `postAfterChange` on `Error`.

[ ] Ticket builds each notice from ids the producer already holds (`nodeId` in `postParseOps`; each entry in `report.parsedDirIds` in reconcile); no inference from ops.

[ ] Ticket: `ParseFailed` leaves document state unchanged; mirror `PersistFailed` in `documentStateFromNotice` and `CoreMailboxLoop.applyNotice` (same as `NodeParsing` / `PersistFailed` no enum write).

[ ] **Test 1 — file parse Change fails** — Public seam: `ParseThread.start` + push (pattern `startParse` in [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs)) or direct `ParseThread.postParseOps`. Observe `CoreMailboxApi.eventsSince` / `orderedEvents`: `NodeParsing`, then `ParseFailed`, zero `NodeParsed` for that id. Force Change failure: call `ParseThread.postParseOps parseHandle fileId (validOps @ [invalidOp])` so `CoreDoor.postGraphOnly` returns `Error`, or use `postOps` that delegates to `postParseOps` with ops that fail apply (missing node id in `Op.SetText`). Node id at site: `fileId` from `parseFile`.

[ ] **Test 2 — directory parse Change fails** — Same module and helpers as test 1; push directory id through parse stack or `postParseOps parseHandle directoryId failingOps`. Node id: `directoryId` from `parseDirectory`.

[ ] **Test 3 — git Load reconcile Change fails** — Public seam: `LazyLoadReconciliationServer.reconcileReceivedFiles` ([NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) `pullReceived` fixture from ``git Load reconcile posts NodeUnparsed then the Change then NodeParsed``). Use `CoreDoor.create false host testCaller` for `workspace.handle` so `CoreDoor.postGraphOnly` returns `Error readOnlyMsg` after `postBeforeChange` posted `NodeParsing` for each `parsedDirIds` entry. Observe event log: one `ParseFailed` per parsed directory id, no `NodeParsed` for those ids. Ids: each member of `report.parsedDirIds` at failure time.

[ ] **Test 4 — EventJson round-trip** — Extend ``Notice JSON round trip`` in [EventJsonTests.fs](../../tests/Shared.Tests/EventJsonTests.fs): add `Notice.ParseFailed(nodeId, "message")` to the list; public seam `EventJson` encode/decode `roundTrip`.

[ ] **Change post failure injection (pattern)** — Persist path uses `throwingPersist` returning `Error` or raising ([DbAgentFailureTests.fs](../../tests/Server.Tests/DbAgentFailureTests.fs)). Parse path has no equivalent test yet; use real `postGraphOnly` `Error` (invalid ops or read-only `CoreChangesDoor`) rather than `DocumentOpImpact.noticesBeside`.

[ ] **Ids at failure sites** — `ParseThread.postParseOps`: `nodeId` parameter. `parseFile`: `fileId`. `parseDirectory`: `directoryId`. `postReconcileReport`: `report.parsedDirIds` (list) after `NodeParsing` for each.

[ ] **Document state on parse failure today** — `NodeParsing` alone leaves graph document state unchanged ([CoreLoopNoticeTests.fs](../../tests/Server.Tests/CoreLoopNoticeTests.fs)). Failed Change does not apply ops. Reconcile failure after `postBeforeChange` may have already applied `NodeUnparsed` via `postContainerUnparsed` on `parsedDirIds` before the failed Change; that is separate from `ParseFailed` semantics in the ticket.

[ ] **Source file line counts (F# under 800 lines per file)** — [History.fs](../../src/Shared/History.fs) 755; [EventJson.fs](../../src/Shared/EventJson.fs) 353; [ParseThread.fs](../../src/Server/ParseThread.fs) 207; [LazyLoadReconciliationServer.fs](../../src/Server/LazyLoadReconciliationServer.fs) 331; [CoreMailboxLoop.fs](../../src/Server/Core/CoreMailboxLoop.fs) 441; [Database.fs](../../src/Server/Database.fs) 596. Test [NodeEventExpandTests.fs](../../tests/Server.Tests/NodeEventExpandTests.fs) is 1033 lines (over 800); prefer new tests in a smaller file or an extension with line budget in mind.

[ ] Ticket sentence vs code: ticket names `History.fs` for exhaustive match; production `Notice` apply match for graph lives in `Ev.documentStateFromNotice` / `applyNotice` in the same file. Ticket does not require `SyncLogic.persistFailedMessage` to mention parse failures unless product wants client-visible parse error text.
