# Change-only Undo implementation plan

See also: [[undo-wayfinder.md]], [[undo-spec.md]], [[audit-optimistic-undo-safety.md]], [[server-change-augmentation-audit.md]], [[spec.md]], [[doc/arch.md]]

## Outcome

The Browser sends every normal command, Undo, and Redo as an ordinary `Change`. Undo and Redo remain local-first, the Server keeps no Undo state, and ChangeLog, Poll, and Load continue to carry complete ordered Changes.

[[undo-spec.md]] is the authoritative behavioral contract. This document defines only dependency order, implementation deltas, and verification checkpoints.

## Explicit deferrals

- Do not add durable Browser History, cross-session Undo, or pending-History restoration.
- Do not add an Undo endpoint, another action codec, compatibility decoding for explicit Undo or Redo JSON, or a new endpoint.
- Do not add invocation grouping. One local Change remains one History record, including each local phase of Load.
- Do not add detached-Node garbage collection. Inverse create Changes detach Nodes and keep their Headers in the Graph map.
- Do not add a workflow engine, generalized request coordinator, or a new persistence adapter.
- Do not change Revision rules, conflict policy, Poll, Load, residency scope, or Server partial residency.
- Do not optimize SiteMap, validation, encoding, network, or persistence paths until measurements show a failure.

## Ordered implementation slices

Implement the slices in order. Each slice depends on the seams established by the preceding slices.

### 1. Characterize semantics and the proven cost

Files: [[tests/Shared.Tests/HistoryTests.fs]], [[tests/Shared.Tests/LargeChangeApplyTests.fs]], [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]].

Delta: add tests for reachable Graph equality after create/paste Undo and Redo, nested Replace order, split, NewSpecialNode, and a 2,000-Node paste-shaped Change. Record baseline timings for current Undo and count the created Nodes that trigger full `Graph.fromNodes` rebuilds.

Why now: these tests lock current user-visible structure before stack and wire types change.

Checkpoint: the semantic tests pass and the baseline demonstrates K rebuild opportunities for K NewNode or NewSpecialNode Ops. Do not set a speculative failing wall-time budget yet.

### 2. Add ordinary inversion and ClientHistory

Files: [[src/Shared/History.fs]], new [[src/Shared/ClientHistory.fs]], [[src/Shared/Gambol.Shared.fsproj]], new [[tests/Shared.Tests/ClientHistoryTests.fs]], [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]].

Delta: add identity-supplied ordinary inversion and the four-function ClientHistory interface: `record`, `undo`, `redo`, and `clear`. ClientHistory stores submitted-only Changes and stable record identity, but no pending lineage or confirmation operation. Put [[src/Shared/ClientHistory.fs]] immediately after [[src/Shared/History.fs]] in compile order. Remove the current confirmation-amendment and pending-lineage implementation before Slice 3.

Why now: this creates the final pure seam without changing transport or runtime callers.

Checkpoint: focused tests prove normal, Undo, Redo, future folding, stable identity, exact names, submitted-only payloads, and no duplicate records. Create and paste Undo detach created Nodes without NewNode or NewSpecialNode inverse Ops; Redo reconnects the same Node IDs.

### 3a. Adapt the Browser queue and preserve the submit snapshot

Files: [[src/Shared/ViewModelSync.fs]], [[src/Shared/ViewModel.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Shared/SyncBatch.fs]], [[src/Client/App.fs]], [[src/Client/Update.fs]], [[src/Client/UpdateOps.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateWorkspaceDownload.fs]], [[tests/Shared.Tests/SyncPlannerTests.fs]], and VM test helpers.

Preserve: `SyncInfo.pendingChanges` remains the complete unacknowledged queue; `SyncState.Sending` remains the single-submit marker; `SubmitPendingBatch` carries the exact list selected before later actions append; `SubmitNetworkError` and `WaitingToRetry` preserve that list for retry; restored pending Changes are filtered, projected, saved, and resubmitted without recreating Browser History.

Delta: convert those queue, effect, retry, error, and save payloads from `ChangeRequest` to an ordinary Change plus optional PendingTransition. Retain the `SubmitPendingBatch` list through the success callback for Slice 5 reconciliation. Carry singleton workspace lineage in the existing synchronous local value and async workspace effect argument. Add no second submitted-list representation.

Why now: this converts the transport-owned client state before runtime History and the coordinated wire change.

Checkpoint: tests prove later actions do not alter the submitted or retry list; repeated record identity does not block C/U/Redo batching; restored Changes have no transition or History record; and synchronous and async workspace submissions retain exact singleton lineage before their requests.

### 3b. Wire runtime History and projected local flow

Files: [[src/Shared/ViewModel.fs]], [[src/Shared/SyncLogic.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Client/UpdateOps.fs]], [[src/Client/Program.fs]], [[tests/Shared.Tests/SyncLogicTests.fs]], and VM test helpers.

Delta: change `VM.history` to ClientHistory. Record normal local Changes with command provenance, and make Undo and Redo optimistic local operations. Route every inverse Change through the existing [[src/Shared/ResidentProjection.fs]] seam only; do not add another Graph application path. Keep the old runtime action functions only as the temporary branch-local compile bridge until their removal in slice 5.

Implement the Poll, Load, package-only residency, and partial-projection rules from [[undo-spec.md]]. Keep ACK reconciliation deferred until Slice 5 has complete confirmations.

Why now: Slice 3a establishes transport ownership, so runtime History can change without changing the wire contract in the same slice.

Checkpoint: focused runtime and SyncLogic tests prove submitted-only normal recording, optimistic Undo and Redo through ResidentProjection, History preservation for empty Poll and Load Change tails, History clearing before projected application of every non-empty semantic tail, package-only preservation at the same settled Revision, and refusal of a raced package payload. ACK suffix reconciliation remains the Slice 5 checkpoint after complete confirmations exist.

### 4. Cut the wire and Server to Change-only confirmations

Files: [[src/Shared/Serialization.fs]], [[src/Client/UpdateCodec.fs]], [[src/Client/Update.fs]], [[src/Client/App.fs]], [[src/Server/ChangeLog.fs]], [[src/Server/Database.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]], [[src/Server/Api.fs]], [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/FileAgentFailureTests.fs]], and [[tests/Server.Tests/DatabaseProjectionContractTests.fs]].

Delta: replace action batches and old ACK fields in one coordinated slice. Return durable complete Changes for new and duplicate requests. Preserve stamp assignment, batch atomicity, optional persistence messages, and the existing route.

Why now: both sides already use ordinary local Changes, so this slice changes one transport contract without adding compatibility code.

Checkpoint: codec and both-backend endpoint tests prove request order, exact submitted prefixes, `SetUpdateTime`-only stamp enrichment, append-to-last-new assignment including trailing duplicates, duplicate retry after a lost ACK, restart-safe inverse Changes, ChangeLog equality, unchanged-request rejection, and atomic bad-second-Change rejection.

### 5. Reconcile ACKs and remove legacy History

Files: [[src/Shared/History.fs]], [[src/Shared/SyncPlanner.fs]], [[src/Client/Update.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateWorkspaceDownload.fs]], [[src/Server/Database.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DocumentLoader.fs]], [[src/Server/SavePrep.fs]], and affected constructors and tests.

Delta: extend `SubmitResponse` with the submitted queued items retained by the `SubmitPendingBatch` callback. Reconcile complete confirmations atomically: validate ordered identity and submitted prefixes, allow only `SetUpdateTime` suffixes, remove only the matching `pendingChanges` prefix, retire transitions, project suffixes, and advance Revision without changing ClientHistory. Ignore a fully valid response only when all submitted identities are already retired and its Revision is not ahead; reject partial overlap or any other mismatch. Route synchronous and async workspace singleton responses through this seam. Remove legacy action cases, Server History, ACK ID/stamp aggregates, and direct stamp application.

Why now: complete confirmations are available, so no caller needs the legacy intent or aggregate ACK paths.

Checkpoint: Normal, Undo, Redo, same-batch C/U/Redo, partial residency, retry, late duplicate response, rejection, and both workspace ACK paths pass. Tests reject missing, reordered, unmatched, changed-prefix, partial-overlap, forward-Revision, and forbidden-suffix confirmations atomically; allowed suffixes do not change History; no legacy action reference remains under `src`.

### 6. Wire command provenance and feedback

Files: [[src/Client/Controller.fs]], [[src/Client/CommandDock.fs]], [[src/Client/UpdateHelpers.fs]], [[src/Client/UpdatePaste.fs]], [[src/Client/UpdateWorkspaceSync.fs]], [[src/Client/UpdateWorkspaceDownload.fs]], [[src/Client/UpdateRename.fs]], [[src/Client/UpdateFileSearch.fs]], and other direct `applyAndPost` callers found by the final source search.

Delta: pass the resolved string at the command/event source. Keep CommandEntry in place. Set Undo and Redo result text on optimistic stack success, including `Undo: nothing to undo` and `Redo: nothing to redo`.

Why now: the History seam is stable, so this is mechanical provenance wiring rather than a second redesign.

Checkpoint: focused ClientHistory and CmdLastResult tests prove accepted text and names; source search finds no anonymous History-worthy local Change; CommandDock, prompts, paste, cut, text commit, Load, and Download use the required names.

### 7. Verify and measure

Files: [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]], [[tests/Server.Tests/Gambol.Server.Tests.fsproj]], [[src/Client/Client.fsproj]], and the measurement report for this project.

Delta: run the focused Shared and Server suites for the changed modules, DB tests when configured, and the Browser build. Repeat the same large-paste scenario and measure inverse planning, projected apply, SiteMap reconciliation, encoding, Server apply, persistence, ACK encoding, and total response. Add a stable inverse budget only after measurement.

Why now: all behavior and transport changes are present, so the final measurements compare the delivered path rather than intermediate bridges.

Checkpoint: required tests and build pass, the per-created-Node rebuild is absent, reachable Graph equality holds, and remaining measured phases are reported without speculative optimization.
