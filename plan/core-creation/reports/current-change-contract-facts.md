# Current Change contract facts

Date: 2026-09-05

Purpose: Fact inventory for grilling [[03-define-typed-core-changes-contract]]. This report records what the code and tests do today. It does not choose a Core Changes contract.

## 1. Exact existing types

### Change and batch

- **Change** ([[src/Shared/History.fs]]): `{ id: int; changeId: System.Guid; ops: Op list }`. Comment on `changeId`: unique per network submission; used for server-side dedup.
- **ChangeBatch** ([[src/Shared/Serialization.fs]]): `{ changes: Change list }`. Wire key is `changes`. Decode fails when the list is empty (`"changes must not be empty"`).
- **Op** ([[src/Shared/History.fs]]): discriminated union (`NewNode`, `SetText`, `SetClasses`, `Replace`, `NewSpecialNode`, `SetName`, `SetDocumentState`, `SetUpdateTime`). Field CAS ops carry their own prior values (`oldText`, `oldClasses`, `oldChildren`, etc.).
- **ApplyResult** ([[src/Shared/History.fs]]): `Changed of State | Unchanged of State | Invalid of State * string`.

### Revision

- **Revision** ([[src/Shared/Model.fs]]): struct wrapper around `int`. `Revision.Zero` is 0.
- Server **State.revision** counts accepted Changes. Each successfully applied new Change in a batch increments revision by 1 ([[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]).
- Change **`id`** is the client-supplied base revision for that Change in the batch. The Browser rewrites pending queue ids into a contiguous chain from a base revision via [[src/Shared/SyncBatch.fs]] `toDeltaChain` / `toWireBatch`. The server does not reject stale `id` values; tests show `id = 5` accepted when server revision is 1 ([[tests/Server.Tests/StateEndpointTests.fs]] `POST with stale base revision and valid SetText succeeds`).

### Change identifiers

- **`changeId`**: `System.Guid`. Generated client-side for normal submits (`History.newChange`, undo/redo inverses). Optional on decode for backward compatibility; missing values get `Guid.NewGuid()` ([[src/Shared/Serialization.fs]] `decodeChange`).
- Server-side dedup keys on **`changeId`**, not on `id`.
- DB stores `change_uuid` with a **unique index** ([[src/Server/Database.fs]]). Lookup is by UUID only (`tryGetPersistedPayload`).

### Client pending / submit unit (not sent on the wire)

- **PendingChange** ([[src/Shared/ViewModelSync.fs]]): `{ change: Change; transition: PendingTransition option }`.
- **PendingTransition**: `{ recordId: int; submittedChangeId: Guid; kind: PendingKind }` where `PendingKind` is `Normal | Undo | Redo`.
- CONTEXT term **ChangeRequest** maps to this pending-queue unit ([[CONTEXT.md]]). POST body is `Change list` only; transition metadata is stripped by `SyncBatch.toWireBatch`.

### Success response

- **ChangeSuccessResponse** ([[src/Shared/ApiResponses.fs]]): `{ revision; buildEpochSec; pageBuildEpochSec; apiVersion; isReady; externalChanges; changes: Change list; message: string option; bootstrapHash: string option }`.
- Wire encoding uses short keys `r`, `b`, `p`, `v`, `c`, plus `externalChanges`, `ready`, optional `message`, optional `bootstrapHash` ([[src/Shared/ApiResponseSerialization.fs]]).
- POST success and Poll both use this shape. Poll sets `externalChanges = not changes.IsEmpty` ([[src/Server/Api.fs]] `getPoll`).

### Reject response

- HTTP **400** with JSON `{ "error": "<string>" }` ([[src/Server/Api.fs]] `agentErrorResult`, [[src/Client/UpdateCodec.fs]] `decodePostChangeError`).
- Internal server faults use **500** plain text starting with `"Internal server error"` ([[src/Server/Api.fs]]).
- Client maps POST failure to `SubmitRejected detail` and sets sync state **ServerRejected** ([[src/Client/Update.fs]]).
- Client-side ACK reconciliation uses **AckReconcile** ([[src/Shared/SyncLogic.fs]]): `Applied of ClientSyncState * SyncInfo * Effect list * Op list | Ignored | Rejected of string`. This is Browser-only; the server does not emit it.

### Parse success (not ChangeSuccessResponse)

- POST `/ambit/file/parse` returns `{"ok":true}` on success, or `{"ok":true}` when ops are empty ([[src/Server/Api.fs]] `postParseFile`). No Change list or revision in that response.

## 2. Browser-originated submit flow

1. Local edit: `SyncLogic.applyLocalChange` applies through `ResidentProjection.applyChange`, records ClientHistory, enqueues **PendingChange** ([[src/Shared/SyncLogic.fs]]).
2. `SyncPlanner.enqueuePending` / `tryStartSubmit` emit **SubmitPendingBatch (baseRevision, pendingChanges)** when idle and not blocked ([[src/Shared/SyncPlanner.fs]]).
3. Client effect runner POSTs to `/{currentFile}/changes` (Browser) or `/ambit/changes` (integration tests) ([[src/Client/App.fs]], [[src/Server/RouteRegistration.fs]]).
4. Body: `ChangeBatch` JSON. `SyncBatch.toWireBatch baseRev items` rewrites each Change `id` to `baseRev + index`; only `Change` records are encoded ([[src/Shared/SyncBatch.fs]], [[src/Client/UpdateCodec.fs]]).
5. Success path: decode **ChangeSuccessResponse**, dispatch `SubmitResponse (submitted, ack.changes, ack.revision, ack.externalChanges, ack.message)` ([[src/Client/App.fs]] `SubmitChangeCallbacks.onPostOk`).
6. ACK handling ([[src/Client/Update.fs]]):
   - If `externalChanges` **or** confirmed changes are not a prefix-echo of submitted ops (`SyncLogic.isConfirmationEcho`), use `reconcileExternalAck` (does not require op-by-op match).
   - Else use `reconcileAck` (requires matching changeIds in order, submitted ops as prefix of confirmed ops, stamp suffix rules).
7. Network failure / timeout schedules retry with the same pending batch ([[src/Client/App.fs]], [[src/Shared/SyncPlanner.fs]] `retryWaiting`). Comments state duplicate `changeId` retry is intentional for idempotency.

Upload structure POST also uses `SyncBatch.toWireBatch` + POST changes before workspace push ([[src/Client/App.fs]] `ContinuePostUploadStructure`).

## 3. Server-originated GraphOnly and Parse submit flow

### GraphOnly path

- Mailbox message **PostGraphOnlyChange** shares `handlePostChange` with `graphOnly = true` ([[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]).
- Skips `DocumentPersistence.validatePathMoves` / `validateGraphDiskEffects`.
- Skips synchronous document persist on FileAgent (`changed && not graphOnly`). DbAgent skips live document persist when `graphOnly`.
- Still applies Changes, amends, logs, increments revision, returns **ChangeSuccessResponse** JSON.

### Parse path

1. POST `/ambit/file/parse` body: `{ fileId: string; text: string option }` ([[src/Server/Api.fs]]).
2. Server reads current graph via `getState`, plans ops with `DocumentPersistence.planParseFile` ([[src/Shared/dotnet/ImportDocument.fs]] `planParseFile`).
3. Non-empty ops: encode one Change `{ id = currentRevision; changeId = Guid.NewGuid(); ops }` and call `handle.postGraphOnlyChange` ([[src/Server/Api.fs]] `encodeGraphOnlyChange`).
4. HTTP response is bare `{ok:true}`; caller must Poll (or GET state) to observe graph/revision changes.

### Lazy-load reconciliation (GraphOnly, multi-post)

- Plans ops, splits with [[src/Shared/GraphOnlyChangeChunks.fs]] (`maxOps = 80`), posts each chunk via [[src/Server/GraphOnlyChangePost.fs]].
- Each chunk is its own single-Change batch with a fresh `changeId`; revision increments per successful chunk (`revision + 1` between posts).

## 4. Duplicate changeId behavior

### File backend

- Before apply, `ChangeLog.tryFindByChangeId` scans the log ([[src/Server/ChangeLog.fs]]). On hit, returns the **stored Change** as confirmation without re-applying ([[src/Server/FileAgent.fs]] `applyBatch` step).

### Db backend

- `Database.tryGetPersistedPayload connectionString changeId` ([[src/Server/DbAgent.fs]]). Same idempotent return of stored Change.

### Observed semantics (tests)

- POST same Change twice: second returns **OK**, same revision, same confirmed Change payload ([[tests/Server.Tests/StateEndpointTests.fs]] `POST same changeId twice is idempotent`).
- Duplicate after intervening Changes: still OK; returns stored Change at current revision without duplicating graph effect (`POST duplicate changeId with stale revision stays idempotent`).
- Survives DB restart ([[tests/Server.Tests/StateEndpointTests.fs]] `DB restart keeps duplicate changeId idempotent`).
- DbAgent bootstrap duplicate returns identical ack; separate no-op Change with empty ops is **rejected** (`Unchanged submission is rejected`) ([[tests/Server.Tests/DatabaseProjectionContractTests.fs]]).
- Batch may include a trailing duplicate of an earlier changeId; duplicate returns prior confirmation, new changes still apply ([[tests/Server.Tests/FileAgentFailureTests.fs]] `trailing duplicate keeps stamps on last new Change`).

### Unchanged apply is rejected

- `ApplyResult.Unchanged` from amendment/apply yields Error `"Unchanged submission is rejected."` ([[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]).

## 5. Batch Changes: prior, revision, atomicity

### Shared prior

- There is **no batch-level prior field**. Each Op carries its own CAS prior. Amendment merges against the server's current graph state at apply time ([[src/Shared/ChangeAmendment.fs]]).

### Revision assignment inside a batch

- Changes in one POST are folded **sequentially** ([[src/Server/FileAgent.fs]] `applyBatch`).
- Each **new** Change increments server revision by 1. Confirmed Changes in the ack preserve server-assigned log ids (the `id` field on the stored Change reflects the revision slot written to the log).
- Client sends contiguous `id` values starting at `baseRevision`; server revision after a batch of N new Changes equals prior revision + N ([[tests/Server.Tests/StateEndpointTests.fs]] `POST changes batch with two changes bumps revision to 2`).

### Atomicity

- **All-or-nothing before persist**: if any Change in the batch fails apply, the handler returns Error and **does not** update state or log ([[tests/Server.Tests/StateEndpointTests.fs]] `POST changes batch with bad second change leaves state unchanged`).
- Within a successful batch, Changes are applied **independently in order**, not merged into one Change. Each produces its own log entry / confirmation item.
- After apply, persist runs once for the batch (FileAgent: combined ops from all fresh Changes; DbAgent: one transaction for all log entries + projection).

## 6. Amendment output and Poll-visible sequence

### Amendment produces one Change, not a sequence

- `ChangeAmendment.applyChange` returns `ApplyResult * bool * Change` where the bool is `amended` ([[src/Shared/ChangeAmendment.fs]]).
- On recoverable CAS collision (`old text/name/classes/span does not match`), server builds amended ops in-place and applies a **single** `{ change with ops = amendedOps }`.
- Stale concurrent text/name edits become an **amb-conflict** child node ([[tests/Server.Tests/StateEndpointTests.fs]] concurrent stale tests; [[tests/Shared.Tests/ChangeAmendmentTests.fs]]).
- Server sets `externalChanges = true` when any Change in the batch was amended ([[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]).

### Persist stamp suffix (not a separate Change)

- After document persist, `PersistStamp.opsBetween` may append `SetUpdateTime` ops to the **last** Change in the batch ([[src/Shared/History.fs]] `PersistStamp.appendToLast`).
- ACK overlays stamps onto confirmations by matching `changeId` ([[src/Server/FileAgent.fs]] `overlayFresh`).
- Tests require confirmed ops = submitted ops as **prefix**, suffix ops are **SetUpdateTime only** ([[tests/Server.Tests/StateEndpointTests.fs]] `assertExactPrefix`; [[tests/Server.Tests/FileAgentFailureTests.fs]] `ACK returns stamped complete Change equal to ChangeLog`).

### Poll / ChangeLog sequence

- Poll returns `getChangesSince clientRev`: Changes with log index **after** clientRev through current revision ([[src/Server/Api.fs]], [[src/Server/FileAgent.fs]] `GetChangesSince`).
- One persisted Change per accepted submission (including amended and stamped forms). Poll lists them in log order ([[tests/Server.Tests/StateEndpointTests.fs]] POST undo/redo batch then Poll matches ack changes).
- GraphOnly multi-chunk work produces **multiple** sequential Changes (one per HTTP post), each visible in Poll.

### Browser consumption of amendment

- When `externalChanges` is true (or ops are not a confirmation echo), client uses `reconcileExternalAck`: retires pending prefix, records catch-up baseline, does **not** require confirmed ops to match submitted ops ([[src/Shared/SyncLogic.fs]], [[tests/Shared.Tests/AckReconcileTests.fs]] `amended confirmation echo routes through external ACK not Reject`).
- After external ack with empty remainder, client may start Poll from catch-up baseline ([[src/Client/Update.fs]]).

## 7. Factual constraints a contract decision must preserve

1. **Dedup by changeId**: Idempotent retry of the same `changeId` must return the stored Change and must not double-apply graph effects. DB unique index on `change_uuid` enforces this for Db mode.
2. **Reject unchanged submissions**: Empty-op or no-effect Changes are errors, not silent success.
3. **Batch atomicity on failure**: A failed Change in a batch must not partially persist state or log entries.
4. **Sequential revision monotonicity**: Each accepted new Change advances revision by exactly 1; Poll tail is ordered by revision/log index.
5. **Amendment is success, not Reject**: Recoverable field CAS collisions merge into one accepted Change; `externalChanges` signals the Browser to Poll/reconcile rather than ServerRejected.
6. **Confirmation shape**: Browser echo path expects same changeIds in order, submitted ops as prefix, optional SetUpdateTime suffix only.
7. **Parse / GraphOnly producers today bypass full disk validation and often bypass document persist**; they still produce normal logged Changes consumable by Poll.
8. **Two apply implementations**: Server agents use `ChangeAmendment.applyChange` → `History.applyChange`; Browser projection uses `ResidentProjection.applyChange` (skips missing nodes, Loaded rules). Core contract must not assume identical apply entry points without acknowledging this split.
9. **Wire types already Shared**: `Change`, `ChangeBatch`, `ChangeSuccessResponse`, and serializers live in [[src/Shared/Serialization.fs]] and [[src/Shared/ApiResponseSerialization.fs]]; HTTP adapters currently pass JSON strings into agents ([[src/Server/RouteRegistration.fs]], [[src/Server/FileAgent.fs]]).
10. **Duplicate applyBatch logic**: FileAgent and DbAgent each implement their own batch fold, dedup, persist, and ack encoding ([[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]).
11. **Timeout / soft-fail persist behavior** on FileAgent and DbAgent live persist are current production constraints documented elsewhere ([[plan/core-creation/reports/core-wayfinder-fact-inventory.md]]); they affect when stamps and checkpoint revision advance.

## 8. Compile lists and focused tests

### Shared ([[src/Shared/Gambol.Shared.fsproj]])

- Types and logic: [[src/Shared/History.fs]], [[src/Shared/Model.fs]], [[src/Shared/Serialization.fs]], [[src/Shared/ApiResponses.fs]], [[src/Shared/ApiResponseSerialization.fs]], [[src/Shared/ChangeAmendment.fs]], [[src/Shared/SyncBatch.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Shared/SyncLogic.fs]], [[src/Shared/ViewModelSync.fs]], [[src/Shared/ResidentProjection.fs]], [[src/Shared/GraphOnlyChangeChunks.fs]].

### Server ([[src/Server/Gambol.Server.fsproj]])

- Agents and adapters: [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]], [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/ChangeLog.fs]], [[src/Server/GraphOnlyChangePost.fs]], [[src/Server/Database.fs]], [[src/Server/LazyLoadReconciliationServer.fs]].

### Focused tests

| Area | Test module |
|------|-------------|
| Change / batch serde | [[tests/Shared.Tests/SerializationTests.fs]] |
| Amendment | [[tests/Shared.Tests/ChangeAmendmentTests.fs]] |
| Client ACK reconcile | [[tests/Shared.Tests/AckReconcileTests.fs]] |
| Submit planner / delta chain | [[tests/Shared.Tests/SyncPlannerTests.fs]] |
| Poll outcome | [[tests/Shared.Tests/SyncLogicTests.fs]] |
| POST / Poll / idempotency / batch / amendment HTTP | [[tests/Server.Tests/StateEndpointTests.fs]] |
| Stamp suffix / batch duplicate | [[tests/Server.Tests/FileAgentFailureTests.fs]] |
| Db dedup / unchanged reject | [[tests/Server.Tests/DatabaseProjectionContractTests.fs]] |
| GraphOnly chunk post | [[tests/Server.Tests/GraphOnlyChangePostTests.fs]], [[tests/Shared.Tests/GraphOnlyChangeChunksTests.fs]] |
| Parse endpoint resilience | [[tests/Server.Tests/ChangeEndpointResilienceTests.fs]] |

## 9. Related CONTEXT terms

- **Change**: graph modification unit; multiple Ops; one Action kind ([[CONTEXT.md]]).
- **Revision**: number of an Action (Change, Undo, Redo) ([[CONTEXT.md]]).
- **Poll**: request for Actions since a known Revision ([[CONTEXT.md]]).
- **ChangeLog**: server's durable ordered log of Changes ([[CONTEXT.md]]).
- **ChangeRequest**: client pending-queue unit (Change, Undo, or Redo) ([[CONTEXT.md]]).
