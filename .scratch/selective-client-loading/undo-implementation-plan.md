# Change-only Undo implementation plan

See also: [[undo-wayfinder.md]], [[undo-spec.md]], [[audit-optimistic-undo-safety.md]], [[server-change-augmentation-audit.md]], [[spec.md]], [[doc/arch.md]]

## Outcome

The Browser sends every normal command, Undo, and Redo as an ordinary `Change`. The Server applies, persists, and confirms that same unit. ChangeLog, Poll, and Load continue to carry complete ordered Changes.

Undo and Redo remain local-first. The Browser keeps client-only History records, creates inverse Changes with fresh request identities, and applies them through the existing resident-projection rules. The Server does not keep History and does not interpret Undo or Redo intent.

Each successful ACK returns complete confirmed Changes in request order. A confirmed Change keeps the submitted Ops as an exact prefix and can append only Server `SetUpdateTime` persistence Ops. Browser History keeps exactly the submitted local Change; ACK suffix Ops are authoritative metadata that project through the resident-projection seam but never enter or alter Browser History. A client-submitted `SetUpdateTime` remains part of the submitted Change and is invertible.

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

The Browser adds one pure, deep module in [[src/Shared/ClientHistory.fs]]. Its interface is `record`, `undo`, `redo`, and `clear`. It hides stack order, Emacs-style future handling, stable record identity, and submitted-only applied Changes. It does not validate confirmations, own pending lineage, or amend records from ACK data.

The minimal client-only state is:

- `HistoryRecord = { recordId; commandName: string; applied: Change }`, where `applied` is exactly the last client-submitted local Change for that logical record.
- `ClientHistory = { past; future; nextRecordId }`.
- `PendingTransition = { recordId; submittedChangeId; kind }`, where kind is `Normal`, `Undo`, or `Redo`.
- A queued item contains one Change and an optional PendingTransition. Restored local-storage Changes have no transition and do not recreate History.
- SyncPlanner owns `PendingTransition` and forms one private `InFlightBatch` value that contains the exact ordered queued items selected for one HTTP submit. The same registration API creates a singleton in-flight value for synchronous workspace posts and the async upload-structure bypass before the request is issued. Its Changes form the payload, and its exact membership governs retry, ordered confirmation validation, and lineage retirement while the pending queue can grow. Do not expose a ready prefix and an in-flight count as separate concepts.

No History field or command name crosses the wire. `CommandEntry` stays in [[src/Shared/CommandEntry.fs]]. Browser command sources resolve its display name and pass a string to the Change-recording seam.

Ordinary inversion belongs with `Change` in [[src/Shared/History.fs]]. `Change.inverse` takes a supplied base Revision and fresh `changeId`, reverses the source Ops, swaps old and new values for Set and Replace Ops, and omits `NewNode` and `NewSpecialNode`. It does not call `Op.undo`, `Change.undo`, or `History.undo`.

An inverse create or paste first runs inverse Replace Ops. These Ops detach the created Nodes. Redo inverts the applied Undo Change and reattaches the same Nodes without another NewNode Op.

[[src/Shared/ResidentProjection.fs]] remains the only Graph application seam for Browser normal, Undo, Redo, ACK-extra, Poll, and Load Changes. Header Ops affect Resident Nodes. Replace affects only Loaded Children. Effects for Absent Headers and Unloaded Children are consumed without widening residency.

## Ownership and dependency order

### Shared

- [[src/Shared/History.fs]] owns `Op`, `Change`, ordinary inversion, full-Graph Change application, and canonical ownership validation. Remove `ChangeRequest`, the old `History` record, stack application, `applyAction`, and destructive NewNode undo after callers move.
- [[src/Shared/ClientHistory.fs]] owns only Browser History records, stack movement, stable record identity, and submitted-only applied Changes. It depends on `Change`, not on synchronization, ViewModel, or CommandEntry.
- [[src/Shared/ViewModelSync.fs]] changes `pendingChanges`, retry snapshots, submit effects, and save-queue effects from `ChangeRequest` to queued ordinary Changes.
- [[src/Shared/SyncBatch.fs]] keeps only the ordinary Change delta-chain function and removes `toActionDeltaChain`.
- [[src/Shared/SyncPlanner.fs]] preserves HTTP single-flight, owns PendingTransition confirmation lineage, and creates the private exact `InFlightBatch`. Batch selection takes every currently ready queued item in order; several transitions for one `recordId` may share a batch. It also registers singleton workspace submissions before their request effect runs.
- [[src/Shared/Serialization.fs]] removes explicit action encoding and decoding. It encodes Change-only batches and ACKs with `confirmedChanges`; `ackedChangeIds` and aggregate `stampOps` leave the contract.
- [[src/Shared/SyncLogic.fs]] keeps remote-tail behavior: any non-empty semantic Poll or Load Change tail clears ClientHistory before projected application, while an empty tail preserves it. Package-only Load residency expansion may preserve History only when its response Revision still equals the settled Browser Revision and there is no pending or in-flight local transition; a raced payload is refused as data-outdated and requires reload. Do not match Poll or Load Changes to History, and do not rebase History over them.
- [[src/Shared/ViewModel.fs]] changes `VM.history` to `ClientHistory` and changes `SubmitResponse` to carry the new ACK data.

Add [[src/Shared/ClientHistory.fs]] immediately after [[src/Shared/History.fs]] in [[src/Shared/Gambol.Shared.fsproj]]. This position lets ViewModelSync and ViewModel use client History without moving CommandEntry. Existing Serialization, SyncBatch, SyncPlanner, ResidentProjection, and SyncLogic order can then remain.

### Browser

- [[src/Client/UpdateHelpers.fs]] is the normal local Change seam. It projects the Change, records command provenance, adds a queued Change with its PendingTransition, and emits the existing submit and local-storage effects.
- [[src/Client/UpdateOps.fs]] asks ClientHistory for Undo or Redo, projects the returned inverse Change, updates `CmdLastResult` immediately, and enqueues the returned transition.
- [[src/Client/Update.fs]] and [[src/Shared/SyncPlanner.fs]] reconcile complete ordered confirmations against exact in-flight membership. For each item they validate identity and the submitted Ops prefix, accept only a `SetUpdateTime` suffix, retire its PendingTransition lineage, project the suffix through ResidentProjection, advance Revision, and leave ClientHistory unchanged. Validation and projection are atomic: any missing, reordered, unmatched, changed-prefix, or invalid-suffix confirmation rejects the whole ACK and requires reload.
- [[src/Client/App.fs]] serializes only the Changes in the planner's exact `InFlightBatch`. It restores local-storage Changes onto the scoped Graph with their current retry and recovery behavior, but gives them no PendingTransition and creates no Browser History. The pending decoder reads only Changes; old data that contains explicit Undo or Redo fails closed and is cleared without a compatibility decoder.
- [[src/Client/UpdateCodec.fs]] becomes a thin Change-batch and complete-ACK codec.
- [[src/Client/UpdateWorkspaceSync.fs]] and [[src/Client/UpdateWorkspaceDownload.fs]] register each workspace Change and its PendingTransition as a singleton in-flight submission before issuing synchronous or async requests, then use the same atomic complete-confirmation reconciliation seam. The async upload-structure path must register lineage before emitting `ContinuePostUploadStructure`. A failed optimistic structure post enters the existing rejected/reload state; remove `undoLocalStructure`.
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

1. **Normal:** The event source supplies a command name and a planned Change. The Browser projects it, stores exactly that submitted Change in one History record, queues it, and posts the planner's exact in-flight batch. The Server validates, applies, enriches, persists, and confirms it. The Browser validates and projects only the allowed suffix metadata, retires confirmation lineage, and does not change the History record.
2. **Undo:** Commit dirty text first as `Edit node`. ClientHistory inverts the current submitted-only applied Change with a fresh identity, moves the record to future, and returns its command name and stable record identity. The Browser projects and queues the inverse, then shows `Undo: <command name>`.
3. **Redo:** ClientHistory inverts the last applied Undo Change with a fresh identity, moves the same record back to past, projects and queues it, then shows `Redo: <command name>`.
4. **Rapid same-record transitions:** C, U, and Redo are complete submitted Changes when created and may share one batch. Ordered in-flight transitions retain their common `recordId` and distinct `changeId` values for confirmation lineage. ACK suffix metadata does not change any inverse payload, so confirmation never rewrites a queued or in-flight Change. Aggregate persistence stamps remain assigned to the last newly persisted Change in the batch.
5. **Duplicate retry:** The Browser preserves Ops and `changeId`; only release-time base Revision can change. File mode reads the indexed ChangeLog payload and DB mode reads the row by `change_uuid`. The Server returns the exact original complete Change and does not apply or persist it again.
6. **ACK reconciliation:** Validate the whole ordered ACK against the exact in-flight batch before publishing any state. Each confirmed Change must match identity, preserve submitted Ops as an exact prefix, and have a suffix made only of `SetUpdateTime`. Project suffixes in request order through ResidentProjection, retire transition lineage, advance Revision, and leave History unchanged.
7. **Rejection or corrupt ACK:** A rejected batch, missing confirmation, reordered identity, unknown identity, changed submitted prefix, forbidden suffix, or duplicate with different content is not merged. Clear the persisted pending queue, mark Sync as rejected or data-outdated, and require reload.

## Ordered implementation slices

### 1. Characterize semantics and the proven cost

Files: [[tests/Shared.Tests/HistoryTests.fs]], [[tests/Shared.Tests/LargeChangeApplyTests.fs]], [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]].

Add tests for reachable Graph equality after create/paste Undo and Redo, nested Replace order, split, NewSpecialNode, and a 2,000-Node paste-shaped Change. Record baseline timings for current Undo and count the created Nodes that trigger full `Graph.fromNodes` rebuilds.

Why first: these tests lock current user-visible structure before stack and wire types change.

Checkpoint: the semantic tests pass and the baseline demonstrates K rebuild opportunities for K NewNode or NewSpecialNode Ops. Do not set a speculative failing wall-time budget yet.

### 2. Add ordinary inversion and ClientHistory

Files: [[src/Shared/History.fs]], new [[src/Shared/ClientHistory.fs]], [[src/Shared/Gambol.Shared.fsproj]], new [[tests/Shared.Tests/ClientHistoryTests.fs]], [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]].

Add identity-supplied inversion and the four-function ClientHistory interface. `record`, `undo`, and `redo` return stable record identity for the queue seam, but ClientHistory stores no pending lineage and has no `confirm` operation. Test Normal, Undo, Redo, future folding, stable record identity, exact names, submitted-only payload retention, and no duplicate records. Prove that Undo of a create or paste detaches the created Normal and Special Nodes but keeps their Headers in Graph.nodes for a later Redo with the same Node IDs. Remove the existing confirmation-amendment, dependent-rewrite, and `pendingByRecord` implementation and tests before Slice 3.

Why now: this creates the final pure seam without changing transport or runtime callers.

Checkpoint: ClientHistory proves only submitted-local stack behavior and stable record identity. Large create inversion contains no NewNode or NewSpecialNode Ops. Created Nodes remain in Graph.nodes but are unreachable from ROOT after Undo, and Redo reconnects the same Node IDs. Confirmation behavior has no ClientHistory test or API; its validation belongs to SyncPlanner and ACK reconciliation. Permanent orphan collection stays deferred to a future garbage-collection policy.

### 3a. Adapt the Browser queue and exact in-flight state

Files: [[src/Shared/ViewModelSync.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Shared/SyncBatch.fs]], [[src/Client/App.fs]], [[tests/Shared.Tests/SyncPlannerTests.fs]], and VM test helpers.

Change pending state to queued ordinary Changes with an optional PendingTransition. Preserve current HTTP single-flight and move each selected ordered set into one private exact `InFlightBatch`; Changes that accumulate later remain pending. Select every currently ready item in order, including several transitions for the same `recordId`. Retry resends only the exact in-flight membership. Provide the same private registration seam for singleton workspace posts so confirmation lineage exists before their request effect runs.

Preserve the established restored-pending recovery sequence: filter saved Changes against the loaded Revision, project those still applicable, save the filtered queue, and resubmit it. The new representation gives each restored Change no PendingTransition and does not recreate Browser History. Do not add another recovery path or a second codec.

Why now: queue type conversion, exact in-flight membership, retry isolation, workspace singleton registration, and restored-pending adaptation form one independently verifiable transport slice before runtime History wiring and the coordinated wire cutover.

Checkpoint: planner tests prove that direct C then U keeps C in the current exact in-flight batch and U pending; A in flight followed by queued C, U, and Redo selects all three in order after A settles; repeated `recordId` values do not block selection; retry uses only the exact in-flight batch even when pending grows; singleton workspace posts register exact lineage before request effects; and restored Changes retain established recovery behavior without transitions or History recreation. Existing Poll, Load, and rejection tests remain green.

### 3b. Wire runtime History and projected local flow

Files: [[src/Shared/ViewModel.fs]], [[src/Shared/SyncLogic.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Client/UpdateOps.fs]], [[src/Client/Program.fs]], [[tests/Shared.Tests/SyncLogicTests.fs]], and VM test helpers.

Change `VM.history` to ClientHistory. Record normal local Changes with command provenance, and make Undo and Redo optimistic local operations. Route every inverse Change through the existing [[src/Shared/ResidentProjection.fs]] seam only; do not add another Graph application path. Keep the old runtime action functions only as the temporary branch-local compile bridge until their removal in slice 5.

Keep the exact upstream rule at this runtime seam: ACK confirmation never changes History; slice 5 validates and projects allowed suffix metadata after slice 4 supplies complete confirmations. Any non-empty semantic Poll or Load Change tail clears History before projected application, while an empty tail preserves it. Package-only residency expansion preserves History only when the response Revision is still the settled Browser Revision and no local transition is pending or in flight; otherwise refuse the payload and require reload. Do not add Poll or Load History matching or rebase semantics.

Why now: Slice 3a establishes exact transport ownership without a same-record dependency mechanism, so the Browser can adopt submitted-only local History and projection seams without changing the wire contract in the same slice.

Checkpoint: focused runtime and SyncLogic tests prove submitted-only normal recording, optimistic Undo and Redo through ResidentProjection, History preservation for empty Poll and Load Change tails, History clearing before projected application of every non-empty semantic tail, package-only preservation at the same settled Revision, and refusal of a raced package payload. ACK suffix reconciliation remains the Slice 5 checkpoint after complete confirmations exist.

### 4. Cut the wire and Server to Change-only confirmations

Files: [[src/Shared/Serialization.fs]], [[src/Client/UpdateCodec.fs]], [[src/Client/Update.fs]], [[src/Client/App.fs]], [[src/Server/ChangeLog.fs]], [[src/Server/Database.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]], [[src/Server/Api.fs]], [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/FileAgentFailureTests.fs]], and [[tests/Server.Tests/DatabaseProjectionContractTests.fs]].

Replace action batches and old ACK fields in one coordinated slice. Return durable complete Changes for new and duplicate requests. Preserve stamp assignment, batch atomicity, optional persistence messages, and the existing route.

Why now: both sides already use ordinary local Changes, so this slice changes one transport contract without adding compatibility code.

Checkpoint: codec and both-backend endpoint tests prove request order, exact submitted prefixes, `SetUpdateTime`-only stamp enrichment, append-to-last-new assignment including trailing duplicates, duplicate retry after a lost ACK, restart-safe inverse Changes, ChangeLog equality, unchanged-request rejection, and atomic bad-second-Change rejection.

### 5. Reconcile ACKs and remove legacy History

Files: [[src/Shared/History.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Client/Update.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateWorkspaceDownload.fs]], [[src/Server/Database.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DocumentLoader.fs]], [[src/Server/SavePrep.fs]], and affected constructors and tests.

Validate every complete confirmation against the exact ordered in-flight submission before changing the model: identity must match, submitted Ops must be an exact prefix, and every suffix Op must be `SetUpdateTime`. Project suffixes through ResidentProjection, retire PendingTransition lineage, feed suffix stamps to existing auto-download accumulation, advance Revision, and leave ClientHistory unchanged. Reconcile synchronous and async workspace posts through this same seam. Remove `ChangeRequest.Undo/Redo`, old stack functions, process-local Server History, `ackedChangeIds`, aggregate `stampOps`, and direct ACK stamp application.

Why now: complete confirmations are available, so no caller needs the legacy intent or aggregate ACK paths.

Checkpoint: Normal, Undo, Redo, same-batch C/U/Redo, Absent Header, Unloaded Children, retry, rejection, synchronous workspace ACK, and async upload-structure ACK tests all pass. Tests reject missing, reordered, unmatched, changed-prefix, and non-`SetUpdateTime` suffix confirmations atomically. Allowed suffix projection does not change History, and no legacy action reference remains under `src`.

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

- A confirmation can arrive after the same record moved again. Exact ordered in-flight transitions retire lineage by stable `recordId`, direction, and `changeId`, but ACK handling must not inspect or amend the record's current stack position.
- Appended persistence Ops can belong to the last newly logged Change rather than the command that first dirtied a Document. Preserve the durable ChangeLog assignment and never spread aggregate Ops across records.
- A complete confirmation may append only `SetUpdateTime`. Any other suffix kind is corruption and requires reload. Client-submitted `SetUpdateTime` remains inside the exact submitted prefix and stays invertible.
- Synchronous and async workspace posts currently bypass the normal queue. They must register exact singleton in-flight lineage before issuing a request and use the common atomic ACK seam.
- A package-only Load may preserve History only at the same settled Revision with no pending or in-flight local transition. Refuse a raced payload instead of installing it over optimistic state.
- Detached Nodes remain Resident in the Graph map for the Session. This is intentional and can increase session memory; garbage collection remains deferred.
- Partial residency can make an inverse a projected no-op. Still consume and confirm the Change so Browser Revision and Server Revision remain ordered.
- Any ACK identity, order, prefix, suffix-kind, or content mismatch requires reload. Do not attempt a best-effort merge or optimistic rollback.
