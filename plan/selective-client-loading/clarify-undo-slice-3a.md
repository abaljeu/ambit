# Clarify Undo Slice 3a

## Actual synchronization evidence

- [[src/Shared/ViewModelSync.fs]] represents a submit awaiting a response as `SyncState.Sending of attempt: int`. `Sending` contains no submitted list or count. `SyncInfo.pendingChanges` contains every unacknowledged `ChangeRequest`, including actions appended while `Sending`.
- [[src/Shared/SyncPlanner.fs]] `tryStartSubmit` takes the current `syncInfo.pendingChanges`, sets `syncState` to `Sending 1`, and emits `SubmitPendingBatch(baseRevision, changes)` with that exact list. Because `isBusy` blocks another submit during `Sending`, later local actions append to `pendingChanges` without changing the already-emitted effect argument.
- [[src/Client/App.fs]] `runEffect` passes the `SubmitPendingBatch` arguments to `runSubmitPendingBatch`. That function applies `SyncBatch.toActionDeltaChain` to the exact `changes` argument and posts the result. Its timeout and fetch-failure callbacks dispatch `SubmitNetworkError(baseRev, changes, kind)` with the same retained list.
- [[src/Shared/ViewModelSync.fs]] `WaitingToRetry(attempt, baseRevision, changes)` is the current stored retry snapshot. [[src/Client/Update.fs]] puts the `SubmitNetworkError` list in this case, and [[src/Client/UpdateOps.fs]] `retryPendingOp` resends exactly that list in another `SubmitPendingBatch`. Actions appended later remain in `SyncInfo.pendingChanges` but do not enter that retry.
- [[src/Client/App.fs]] implements timeout as a separate timer that dispatches `SubmitNetworkError`; it does not abort the original `postJson`. The original and retry callbacks can therefore both return the same durable confirmation. After one response retires the queue prefix, reconciliation must ignore a later fully valid response when all its submitted identities are already absent and its Revision is not ahead. A partial overlap or forward Revision remains invalid. The retained submitted list and current queue identities provide this check without extra synchronization state.
- Current success handling does not retain the submitted list through the message boundary. `SubmitChangeCallbacks.onPostOk` dispatches only ACK IDs, Revision, aggregate stamps, and message in `SubmitResponse`. [[src/Shared/SyncPlanner.fs]] `ackBatch` converts ACK IDs to a set, filters matching IDs from all `pendingChanges`, and immediately emits the remaining list as the next `SubmitPendingBatch`.
- Complete ordered ACK validation needs the submitted bodies, not only ACK IDs. The minimum missing state is therefore not a new state field or type: pass the `changes` list already owned by `runSubmitPendingBatch` into its success callback and `SubmitResponse`. Reconciliation can then validate ordered confirmations against that retained list and, for an active response, require it to equal and remove only the current queue prefix.
- [[tests/Shared.Tests/SyncPlannerTests.fs]] already names the preserved concepts: `tryStartSubmit returns SubmitPendingBatch effect when queue is ready`, `tryStartSubmit returns no effects when already sending`, and `ackBatch dequeues acknowledged changes and schedules remainder`. Slice 3a adapts these tests to queued ordinary Changes and prefix-based complete ACK reconciliation.

## Workspace paths

- [[src/Client/UpdateWorkspaceSync.fs]] `applyAndPostSync` uses `postJsonSync`; one Browser update holds the submitted Change as a local value until the response is decoded. The minimum lineage addition is to construct the singleton queued item, including PendingTransition, before the call and pass that same item to common reconciliation.
- Async upload structure applies locally and emits `ContinuePostUploadStructure(change, scope, parseFileId)`. [[src/Client/App.fs]] retains that `change` in the effect and callback until `completeUploadStructurePost`. Change this existing effect argument from a bare Change to the singleton queued item so its exact lineage reaches common reconciliation.
- These workspace submissions remain outside `SyncInfo.pendingChanges`. Their existing local/effect arguments preserve exact singleton membership; they do not need an `InFlightBatch`, an in-flight count, or a new request coordinator.

## Files changed

- [[undo-implementation-plan.md]] uses actual submit and retry names once in Slice 3a and the minimum callback/queue names needed by Slice 5 ACK reconciliation.
- [[undo-wayfinder.md]] states only the destination, rationale, and major decisions and points mechanics to the implementation plan.
- [[undo-spec.md]] is the compact authoritative behavioral contract and does not prescribe synchronization program names.
- [[clarify-undo-slice-3a.md]] records the evidence and final boundary.

## Final Slice 3a scope

Preserved behavior:

- `tryStartSubmit` emits the whole current queue and sets `Sending 1`.
- A single submit remains active while later local actions append to `pendingChanges`.
- `runSubmitPendingBatch` owns the submitted list used for Revision chaining and HTTP encoding.
- `SubmitNetworkError`, `WaitingToRetry`, and `retryPendingOp` preserve the exact failed list.
- Restored pending Changes are filtered, projected, saved, and resubmitted without recreating Browser History.

New behavior:

- Convert queue, submit, retry, error, and persistence payloads from `ChangeRequest` to ordinary Change plus optional PendingTransition.
- Retain the existing `SubmitPendingBatch` item list through the success callback and include it in `SubmitResponse`.
- Validate ordered complete confirmations against that retained list and the current queue prefix, then remove only that prefix. Ignore only a fully valid response for identities that are all already retired when its Revision is not ahead; this prevents a late timed-out attempt from moving Revision backward or applying its suffix twice. C, Undo, and Redo with the same `recordId` remain allowed in one submission.
- Carry a singleton queued item through synchronous workspace locals and `ContinuePostUploadStructure` so common ACK reconciliation has exact lineage.

Explicit non-scope:

- No `InFlightBatch` type.
- No `inFlightCount` field or term.
- No separate ready-prefix representation.
- No same-record dependency stop or inverse rewrite.
- No change to submitted-only Browser History or `SetUpdateTime`-only ACK suffix semantics.

## Consolidation

Task-start and final newline counts (`wc -l`):

- [[undo-implementation-plan.md]]: 198 → 107 lines, down 91.
- [[undo-wayfinder.md]]: 167 → 57 lines, down 110.
- [[undo-spec.md]]: 31 → 40 lines, up 9 because it now contains the complete authoritative contract.
- Total active guidance: 396 → 204 lines, down 192.

Sections removed or condensed:

- [[undo-implementation-plan.md]] removed the empty cleanup heading, Minimal architecture, Ownership and dependency order, End-to-end data flow, and Risks and fail-safe behavior. Outcome and deferrals remain; architecture, ownership, flow, and risk requirements now appear only in the slice that implements them or in the behavioral contract.
- [[undo-wayfinder.md]] removed code-level current-state evidence, local call-path audit, detailed invariants, ACK callback choreography, retry mechanics, performance detail, duplicate incremental slices, and resolved-decision repetition. It now contains only destination, rationale, major decisions, client-History shape, command feedback, delivery pointer, and deferrals.
- [[undo-spec.md]] condensed the delivered explicit-action history to Status and replaced the implementation-oriented destination note with one compact behavioral contract. It contains no submit-state, queue-field, callback, or effect names.

Authoritative homes:

- Behavioral invariants, including submitted-only History, suffix restrictions, batching, retry and late-response safety, Poll/Load rules, package-only guards, rejection, restoration, and workspace lineage: [[undo-spec.md]].
- Destination and rationale: [[undo-wayfinder.md]].
- Dependency order, file ownership by slice, implementation deltas, and checkpoints: [[undo-implementation-plan.md]].
- Existing submit/retry program names: Slice 3a only. ACK reconciliation program names: Slice 5 only.
- Actual-source evidence for those names and boundaries: this report.

Final verification:

- All eight ordered slices, counting 3a and 3b separately, retain Files, Delta, Why now, and Checkpoint, including the verification slice.
- Active-doc searches find no `InFlightBatch`, `inFlightCount`, same-record blocking requirement, or dependent re-derivation requirement.
- Submit/retry program names occur only in implementation-plan Slices 3a and 5; the wayfinder and spec contain no callback choreography.
- Final diff checks pass and the active guidance has a net reduction of 192 lines.

## Proposed WORK.md mutation

- `remove` [[clarify-undo-slice-3a.md]] from Active because the actual synchronization boundary is established and the active Undo planning documents are consistent with it.
