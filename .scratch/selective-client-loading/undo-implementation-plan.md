# Change-only Undo implementation plan

See also: [[undo-wayfinder.md]], [[undo-spec.md]], [[spec.md]], [[server-change-augmentation-audit.md]], [[doc/arch.md]]

## Outcome

The Browser sends every normal command, Undo, and Redo as an ordinary `Change`. The Server applies, persists, and confirms that same unit. ChangeLog, Poll, and Load continue to carry complete ordered Changes.

Undo and Redo remain local-first. The Browser keeps client-only History records, creates inverse Changes with fresh request identities, and applies them through the existing resident-projection rules. The Server does not keep History and does not interpret Undo or Redo intent.

Each successful ACK returns complete confirmed Changes in request order. A confirmed Change keeps the submitted Ops as an exact prefix and can append Server persistence Ops. The Browser amends the existing History record instead of adding an ACK record.

## Explicit deferrals

- Do not add durable Browser History, cross-session Undo, or pending-History restoration.
- Do not add an Undo endpoint, another action codec, compatibility decoding for explicit Undo or Redo JSON, or a new endpoint.
- Do not add invocation grouping. One local Change remains one History record, including each local phase of Load.
- Do not add detached-Node garbage collection. Inverse create Changes detach Nodes and keep their Headers in the Graph map.
- Do not add a workflow engine, generalized request coordinator, or a new persistence adapter.
- Do not change Revision rules, conflict policy, Poll, Load, residency scope, or Server partial residency.
- Do not optimize SiteMap, validation, encoding, network, or persistence paths until measurements show a failure.

## Minimal architecture

The authoritative path has one modification type:

- `ChangeBatch.changes` is `Change list`.
- `ChangeBatchAck` contains `revision`, ordered `confirmedChanges`, and the existing optional persistence message.
- ChangeLog stores each complete confirmed Change.
- Poll and Load return those same complete Changes.

The Browser adds one pure, deep module in [[src/Shared/ClientHistory.fs]]. Its interface is `record`, `undo`, `redo`, `confirm`, and `clear`. It hides stack order, Emacs-style future handling, stable record identity, confirmation amendment, and dependent inverse re-derivation.

The minimal client-only state is:

- `HistoryRecord = { recordId; commandName: string; applied: Change }`.
- `ClientHistory = { past; future; nextRecordId }`.
- `PendingTransition = { recordId; submittedChangeId; kind }`, where kind is `Normal`, `Undo`, or `Redo`.
- A queued item contains one Change and an optional PendingTransition. Restored local-storage Changes have no transition and do not recreate History.

No History field or command name crosses the wire. `CommandEntry` stays in [[src/Shared/CommandEntry.fs]]. Browser command sources resolve its display name and pass a string to the Change-recording seam.

Ordinary inversion belongs with `Change` in [[src/Shared/History.fs]]. `Change.inverse` takes a supplied base Revision and fresh `changeId`, reverses the source Ops, swaps old and new values for Set and Replace Ops, and omits `NewNode` and `NewSpecialNode`. It does not call `Op.undo`, `Change.undo`, or `History.undo`.

An inverse create or paste first runs inverse Replace Ops. These Ops detach the created Nodes. Redo inverts the applied Undo Change and reattaches the same Nodes without another NewNode Op.

[[src/Shared/ResidentProjection.fs]] remains the only Graph application seam for Browser normal, Undo, Redo, ACK-extra, Poll, and Load Changes. Header Ops affect Resident Nodes. Replace affects only Loaded Children. Effects for Absent Headers and Unloaded Children are consumed without widening residency.

## Ownership and dependency order

### Shared

- [[src/Shared/History.fs]] owns `Op`, `Change`, ordinary inversion, full-Graph Change application, and canonical ownership validation. Remove `ChangeRequest`, the old `History` record, stack application, `applyAction`, and destructive NewNode undo after callers move.
- [[src/Shared/ClientHistory.fs]] owns only Browser History records and confirmation lineage. It depends on `Change`, not on ViewModel or CommandEntry.
- [[src/Shared/ViewModelSync.fs]] changes `pendingChanges`, retry snapshots, submit effects, and save-queue effects from `ChangeRequest` to queued ordinary Changes.
- [[src/Shared/SyncBatch.fs]] keeps only the ordinary Change delta-chain function and removes `toActionDeltaChain`.
- [[src/Shared/SyncPlanner.fs]] selects the ready queue prefix, stops before a second unconfirmed transition for the same record, records the exact in-flight count, and reconciles ordered confirmations against that prefix.
- [[src/Shared/Serialization.fs]] removes explicit action encoding and decoding. It encodes Change-only batches and ACKs with `confirmedChanges`; `ackedChangeIds` and aggregate `stampOps` leave the contract.
- [[src/Shared/SyncLogic.fs]] keeps remote-tail behavior: a non-empty Poll or Load tail clears ClientHistory before projected application; an empty tail preserves it.
- [[src/Shared/ViewModel.fs]] changes `VM.history` to `ClientHistory` and changes `SubmitResponse` to carry the new ACK data.

Add [[src/Shared/ClientHistory.fs]] immediately after [[src/Shared/History.fs]] in [[src/Shared/Gambol.Shared.fsproj]]. This position lets ViewModelSync and ViewModel use client History without moving CommandEntry. Existing Serialization, SyncBatch, SyncPlanner, ResidentProjection, and SyncLogic order can then remain.

### Browser

- [[src/Client/UpdateHelpers.fs]] is the normal local Change seam. It projects the Change, records command provenance, adds a queued Change with its PendingTransition, and emits the existing submit and local-storage effects.
- [[src/Client/UpdateOps.fs]] asks ClientHistory for Undo or Redo, projects the returned inverse Change, updates `CmdLastResult` immediately, and enqueues the returned transition.
- [[src/Client/Update.fs]] validates and applies complete ordered confirmations. It projects only the required confirmation correction, amends ClientHistory, updates queued dependent Changes, advances Revision, and removes direct aggregate `PersistStamp.applyToGraph` ACK handling.
- [[src/Client/App.fs]] serializes only the planner's ready Change prefix. It restores local-storage Changes onto the scoped Graph without creating History records. The pending decoder reads only Changes; old data that contains explicit Undo or Redo fails closed and is cleared without a compatibility decoder.
- [[src/Client/UpdateCodec.fs]] becomes a thin Change-batch and complete-ACK codec.
- [[src/Client/UpdateWorkspaceSync.fs]] and [[src/Client/UpdateWorkspaceDownload.fs]] use the same complete-confirmation helper for their existing synchronous Change posts. A failed optimistic structure post enters the existing rejected/reload state; remove `undoLocalStructure`.
- [[src/Client/Controller.fs]] carries the resolved command name through `withDiagnostic`, uses `Paste` and `Cut` for clipboard events, and names prompt completion.
- [[src/Client/CommandDock.fs]] dispatches commands through the same named wrapper as keyboard and palette dispatch.

Keep the accepted fixed names at non-registry sources: `Edit node` in text commit, `Paste`, `Cut`, `Load` for each user-started local Load phase Change, and `Download` for explicit stamp alignment. Path-sync refresh and auto-download create no History record.

### Server

- [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] decode `Change list`, apply each Change to the full Graph, preserve atomic batch rejection, append persistence stamps to the last newly logged Change, persist, and return one complete confirmation for each request item.
- [[src/Server/ChangeLog.fs]] adds a focused lookup by `changeId` over its existing offset index for duplicate confirmation. Do not add a repository interface around this one file implementation.
- [[src/Server/Database.fs]] replaces the boolean duplicate query with a payload lookup by `change_uuid`. Decode that stored payload as the durable duplicate confirmation.
- [[src/Server/Api.fs]] keeps the existing AgentHandle and `/ambit/changes` route. Graph-only Server Changes use the same Change-only batch.
- [[src/Server/Database.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DocumentLoader.fs]], and [[src/Server/SavePrep.fs]] construct State without History. A Server restart therefore has no Undo state to restore.

Preserve current persistence-stamp assignment. Aggregate SetUpdateTime Ops remain appended to the last newly persisted Change in the batch. Update the matching confirmation by `changeId`, even when a later request item is a duplicate.

An unchanged first submission is invalid and rejects the whole batch. Current code acknowledges it without a ChangeLog row, but that result cannot satisfy the accepted durable-confirmation invariant. Normal Browser planners already suppress empty or unchanged Changes. A duplicate of a previously changed request is different and returns its stored complete Change.

## End-to-end data flow

1. **Normal:** The event source supplies a command name and a planned Change. The Browser projects it, adds one History record, queues it, and posts the ready prefix. The Server validates, applies, enriches, persists, and confirms it. The Browser projects appended Ops only while that direction is current, then amends the same record.
2. **Undo:** Commit dirty text first as `Edit node`. ClientHistory inverts the current complete applied Change with a fresh identity, moves the record to future, and returns its command name. The Browser projects and queues the inverse, then shows `Undo: <command name>`.
3. **Redo:** ClientHistory inverts the last applied Undo Change with a fresh identity, moves the same record back to past, projects and queues it, then shows `Redo: <command name>`.
4. **Dependent rapid inverse:** The Browser applies the new inverse immediately but keeps it behind the earlier unconfirmed transition for that record. On predecessor confirmation, ClientHistory folds the complete Change into the record, re-derives the queued inverse without changing its `changeId`, returns any projection correction, and lets SyncPlanner release it. Unrelated ready records can remain in the same earlier batch.
5. **Duplicate retry:** The Browser preserves Ops and `changeId`; only release-time base Revision can change. File mode reads the indexed ChangeLog payload and DB mode reads the row by `change_uuid`. The Server returns the exact original complete Change and does not apply or persist it again.
6. **Rejection or corrupt ACK:** A rejected batch, missing confirmation, reordered identity, unknown identity, changed submitted prefix, or duplicate with different content is not merged. Clear the persisted pending queue, mark Sync as rejected or data-outdated, and require reload.

## Ordered implementation slices

### 1. Characterize semantics and the proven cost

Files: [[tests/Shared.Tests/HistoryTests.fs]], [[tests/Shared.Tests/LargeChangeApplyTests.fs]], [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]].

Add tests for reachable Graph equality after create/paste Undo and Redo, nested Replace order, split, NewSpecialNode, and a 2,000-Node paste-shaped Change. Record baseline timings for current Undo and count the created Nodes that trigger full `Graph.fromNodes` rebuilds.

Why first: these tests lock current user-visible structure before stack and wire types change.

Checkpoint: the semantic tests pass and the baseline demonstrates K rebuild opportunities for K NewNode or NewSpecialNode Ops. Do not set a speculative failing wall-time budget yet.

### 2. Add ordinary inversion and ClientHistory

Files: [[src/Shared/History.fs]], new [[src/Shared/ClientHistory.fs]], [[src/Shared/Gambol.Shared.fsproj]], new [[tests/Shared.Tests/ClientHistoryTests.fs]], [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]].

Add identity-supplied inversion and the five-function ClientHistory interface. Test Normal, Undo, Redo, future folding, stable record identity, exact names, confirmation prefix validation, dependent inverse re-derivation, and no duplicate records. Prove that Undo of a create or paste detaches the created Normal and Special Nodes but keeps their Headers in Graph.nodes for a later Redo with the same Node IDs.

Why now: this creates the final pure seam without changing transport or runtime callers.

Checkpoint: all new behavior is proven through ClientHistory, and large create inversion contains no NewNode or NewSpecialNode Ops. Created Nodes remain in Graph.nodes but are unreachable from ROOT after Undo, and Redo reconnects the same Node IDs. Permanent orphan collection stays deferred to a future garbage-collection policy.

### 3. Convert the Browser queue and projected local flow

Files: [[src/Shared/ViewModelSync.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Shared/SyncBatch.fs]], [[src/Shared/ViewModel.fs]], [[src/Shared/SyncLogic.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Client/UpdateOps.fs]], [[src/Client/App.fs]], [[src/Client/Program.fs]], [[tests/Shared.Tests/SyncPlannerTests.fs]], [[tests/Shared.Tests/SyncLogicTests.fs]], and VM test helpers.

Change pending state to ordinary Changes plus optional client transitions. Add ready-prefix selection, exact in-flight count, same-record dependency blocking, restored-pending behavior, projected Undo and Redo, and remote-tail History clearing.

Why now: the Browser state machine can be tested before the coordinated wire cutover. Keep the old runtime action functions only as a branch-local compile bridge and delete them in slice 5; do not add a second codec.

Checkpoint: planner tests cover independent batching, dependent stop/release, retry identity, ordered ACK validation inputs, Poll and Load blocking, rejection, and empty versus non-empty remote tails.

### 4. Cut the wire and Server to Change-only confirmations

Files: [[src/Shared/Serialization.fs]], [[src/Client/UpdateCodec.fs]], [[src/Client/Update.fs]], [[src/Client/App.fs]], [[src/Server/ChangeLog.fs]], [[src/Server/Database.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]], [[src/Server/Api.fs]], [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/FileAgentFailureTests.fs]], and [[tests/Server.Tests/DatabaseProjectionContractTests.fs]].

Replace action batches and old ACK fields in one coordinated slice. Return durable complete Changes for new and duplicate requests. Preserve stamp assignment, batch atomicity, optional persistence messages, and the existing route.

Why now: both sides already use ordinary local Changes, so this slice changes one transport contract without adding compatibility code.

Checkpoint: codec and both-backend endpoint tests prove request order, exact submitted prefixes, stamp enrichment, duplicate retry after a lost ACK, restart-safe inverse Changes, ChangeLog equality, unchanged-request rejection, and atomic bad-second-Change rejection.

### 5. Reconcile ACKs and remove legacy History

Files: [[src/Shared/ClientHistory.fs]], [[src/Shared/History.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Client/Update.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateWorkspaceDownload.fs]], [[src/Server/Database.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DocumentLoader.fs]], [[src/Server/SavePrep.fs]], and affected constructors and tests.

Project ACK extras or dependent corrections, amend records, release rewritten dependents, and feed confirmed SetUpdateTime Ops to existing auto-download accumulation. Remove `ChangeRequest.Undo/Redo`, old stack functions, process-local Server History, `ackedChangeIds`, aggregate `stampOps`, and direct ACK stamp application.

Why now: complete confirmations are available, so no caller needs the legacy intent or aggregate ACK paths.

Checkpoint: Normal, Undo, Redo, dependent rapid inverse, Absent Header, Unloaded Children, retry, rejection, and synchronous Load-phase ACK tests all pass with no legacy action reference under `src`.

### 6. Wire command provenance and feedback

Files: [[src/Client/Controller.fs]], [[src/Client/CommandDock.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Client/UpdatePaste.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateWorkspaceDownload.fs]], [[src/Client/UpdateRename.fs]], [[src/Client/UpdateFileSearch.fs]], and other direct `applyAndPost` callers found by the final source search.

Pass the resolved string at the command/event source. Keep CommandEntry in place. Set Undo and Redo result text on optimistic stack success, including `Undo: nothing to undo` and `Redo: nothing to redo`.

Why now: the History seam is stable, so this is mechanical provenance wiring rather than a second redesign.

Checkpoint: focused ClientHistory and CmdLastResult tests prove accepted text and names; source search finds no anonymous History-worthy local Change; CommandDock, prompts, paste, cut, text commit, Load, and Download use the required names.

### 7. Verify and measure

Run focused Shared tests for ClientHistory, History, large Change application, Serialization, SyncPlanner, and SyncLogic. Run focused Server tests for both persistence modes, including DB tests when `TEST_DB_CONNECTION_STRING` is available. Build the Browser through Fable.

Repeat the same large external paste before and after. Measure pure inverse planning, projected Browser apply, SiteMap reconciliation, request encoding, Server full-Graph apply, persistence, ACK encoding, and total keypress-to-render time.

Add a focused inverse budget only after the measured result gives a stable threshold. Its structural assertion must prove reachable Graph equality and that inverse planning/application does not rebuild once per created Node.

Checkpoint: the proven per-NewNode rebuild is absent. Treat Replace validation, non-append index work, SiteMap reconciliation, encoding, network, and persistence as hypotheses unless their measured phase exceeds the accepted budget.

## Risks and fail-safe behavior

- A confirmation can arrive after the same record moved again. Stable `recordId` and ordered transitions must amend the record by lineage, not by current stack position.
- Appended persistence Ops can belong to the last newly logged Change rather than the command that first dirtied a Document. Preserve the durable ChangeLog assignment and never spread aggregate Ops across records.
- A dependent re-derived inverse must keep its own `changeId`. Changing it would break idempotent retry.
- Detached Nodes remain Resident in the Graph map for the Session. This is intentional and can increase session memory; garbage collection remains deferred.
- Partial residency can make an inverse a projected no-op. Still consume and confirm the Change so Browser Revision and Server Revision remain ordered.
- Any ACK identity, order, prefix, or content mismatch requires reload. Do not attempt a best-effort merge or optimistic rollback.
