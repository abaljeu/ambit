# 10 — Merge in progress scope

Scope mode: `implement`. Story: [Merge in progress](plan/client-never-fails/spec.md) (user story 7). Architecture: [arch.md section 11 Merge in progress](plan/client-never-fails/arch.md).

### Signatures

[ ] `AckReconcile` in [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs): `Applied of ClientSyncState * SyncInfo * Effect list * Op list` | `Ignored` | `Rejected of string`.

[ ] `SyncLogic.isConfirmationEcho`: `Ev list -> Ev list -> bool` in [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs).

[ ] `SyncLogic.reconcileAck`: `Ev list -> Ev list -> Gambol.Shared.EventId -> ClientSyncState -> SyncInfo -> AckReconcile` in [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs).

[ ] `SyncLogic.reconcileExternalAck`: `Ev list -> Gambol.Shared.EventId -> ClientSyncState -> SyncInfo -> AckReconcile` in [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs).

[ ] `ChangeSuccessResponse` in [src/Shared/ApiResponses.fs](src/Shared/ApiResponses.fs): record with `eventId`, `externalChanges`, `events`, `nodes`, `childMap`, and related poll fields.

[ ] `Update.reconcileSubmit` (private) in [src/Client/Update.fs](src/Client/Update.fs): `Ev list -> ChangeSuccessResponse -> VM -> SubmitReconciliation`, where `SubmitReconciliation` is `{ outcome: AckReconcile; needsCatchUp: bool }`.

[ ] `Update.applyIgnoredSubmitAnswer` (private) in [src/Client/Update.fs](src/Client/Update.fs): `ChangeSuccessResponse -> VM -> VM * Effect list`.

[ ] `Update.applySubmitResponse` (private) in [src/Client/Update.fs](src/Client/Update.fs): `Ev list -> ChangeSuccessResponse -> VM -> VM * Effect list`.

[ ] `UpdateWorkspaceSync.reconcileWorkspaceAck` (private) in [src/Client/UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs): `Ev -> ChangeSuccessResponse -> Graph -> ClientHistory -> EventId -> AckReconcile`.

### Match sites

[ ] [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) `AckReconcile` type definition: cases `Applied`, `Ignored`, `Rejected`.

[ ] [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) `reconcileExternalAck`: match on empty submitted → `AckReconcile.Rejected`; else → `AckReconcile.Applied`.

[ ] [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) `finishReconcileAck`: `identityError` → `Rejected`; `collectSuffixes` → `Rejected`; `queueOutcome` → `Rejected`, `Ok "ignore"` → `Ignored`, `Ok _` → `Applied` via `projectSuffixes` and `retireSubmittedPrefix`.

[ ] [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) `reconcileAck`: empty submitted → `Rejected "missing confirmation"`; else delegates to `finishReconcileAck` after `applyReplyNotices`.

[ ] [src/Client/Update.fs](src/Client/Update.fs) `applySubmitResponse`: `reconciliation.outcome` → `Ignored` → `applyIgnoredSubmitAnswer`; `Rejected` → `rejectPending`; `Applied` → `finishAppliedSubmit`.

[ ] [src/Client/Update.fs](src/Client/Update.fs) `Update.update`: `SysMsg (SubmitResponse (submitted, response))` → `applySubmitResponse`.

[ ] [src/Client/UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) `applyAndPostSync`: match on `reconcileWorkspaceAck` → `Applied` | `Ignored` | `Rejected`.

[ ] [src/Client/UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) `completeUploadStructurePost`: match on `reconcileWorkspaceAck` → `Applied` | `Ignored` | `Rejected`.

[ ] [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) `expectApplied` / `expectRejected`: match all three `AckReconcile` cases.

[ ] [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) per-test matches on `AckReconcile.Ignored`, `Rejected`, or `Applied` (for example ``late duplicate response is ignored when identities are retired`` and ``Notice in the reply keeps the confirmation echo``).

[ ] Caller `SyncLogic.reconcileAck`: [src/Client/Update.fs](src/Client/Update.fs) `reconcileSubmit` (when `needsCatchUp` is false); [src/Client/UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) `reconcileWorkspaceAck` (same guard); [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) (direct unit calls).

[ ] Caller `Update.reconcileSubmit`: [src/Client/Update.fs](src/Client/Update.fs) `applySubmitResponse` only.

### Existing tests

[ ] ``late duplicate response is ignored when identities are retired`` in [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs): calls `SyncLogic.reconcileAck` with one submitted event, a confirmed event whose stamp uses a new `NodeId`, `SyncInfo.initial` (empty pending), and expects `AckReconcile.Ignored` while `eventId` and `history` stay unchanged.

[ ] ``Notice in the reply keeps the confirmation echo`` in [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs): builds `seededEdit` pending plus a trailing `EventBody.Notice`; asserts `SyncLogic.isConfirmationEcho [ pending ] confirmed` is true; `reconcileAck` yields `Applied` with no catch-up; a foreign change plus the same notice yields false echo and `reconcileExternalAck` yields catch-up `Applied`.

[ ] ``retry ACK removes only the submitted prefix and resubmits the remainder`` in [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs): pending `[ first; later ]`, reconcile only `first`, expects `Applied`, pending becomes `[ later ]`, `Sending 1`, and effects `[ SubmitPendingBatch (baseEventId, [ later ]) ]` — fixture shape for multi-item pending with a prefix ACK.

[ ] ``partial-overlap confirmation is rejected atomically`` in [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs): two-item pending with syncInfo pending only `[ second ]`, submitted prefix `[ first; second ]`, expects `Rejected` containing `"partial-overlap"`.

[ ] [tests/Server.Tests/BrowserResidencyClientTests.fs](tests/Server.Tests/BrowserResidencyClientTests.fs) existing facts call `Update.update` with `SysMsg (PollDone …)` and assert `syncInfo.syncState`, `Want.compose`, or `PollServer` effects — no `SubmitResponse` test yet.

### Copy-from

[ ] [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) ``retry ACK removes only the submitted prefix and resubmits the remainder``: helpers `seededEdit`, `confirm`, `sending`, `expectApplied`, and the `[ first; later ]` pending queue for a prefix submit.

[ ] [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) ``late duplicate response is ignored when identities are retired``: pattern for asserting `AckReconcile.Ignored` and unchanged client revision fields via direct `SyncLogic.reconcileAck`.

[ ] [src/Shared/SyncPlanner.fs](src/Shared/SyncPlanner.fs) `retireSubmittedPrefix`: emits `SubmitPendingBatch` when a prefix retires and pending remains — today wired through `AckReconcile.Applied` effects in ``retry ACK removes only the submitted prefix and resubmits the remainder``, not through the `Ignored` path in `Update.applyIgnoredSubmitAnswer`.

### implement

#### Inputs verification ([10 — Merge in progress](plan/client-never-fails/issues/10-merge-in-progress/10-merge-in-progress-ticket.md))

[ ] Input 1 — `SyncLogic.reconcileAck` returns `AckReconcile`; `finishReconcileAck` maps `queueOutcome` `Ok "ignore"` to `AckReconcile.Ignored` in [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) `finishReconcileAck`. **Matches today.**

[ ] Input 2 — `Update.reconcileSubmit` calls `SyncLogic.reconcileAck` when `needsCatchUp` is false (`not response.externalChanges` and `SyncLogic.isConfirmationEcho submitted response.events`) in [src/Client/Update.fs](src/Client/Update.fs) `reconcileSubmit`. **Matches today.**

[ ] Input 3 — `SyncLogic.isConfirmationEcho` returns true when `identityError` is none and `collectSuffixes` succeeds in [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs); ``Notice in the reply keeps the confirmation echo`` asserts a matching reply with a trailing Notice. **Matches today.**

[ ] Input 4–5 — ``late duplicate response is ignored when identities are retired`` in [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs) expects `AckReconcile.Ignored` for a retired submission id. **Matches today.**

[ ] Input 6 — ``Notice in the reply keeps the confirmation echo`` covers a matching post reply through `reconcileAck`. **Matches today.**

[ ] Input 7 — **Flag:** a different post reply while more changes stay on `syncInfo.pending` does not yield `AckReconcile.Ignored` today. Examples: ``partial-overlap confirmation is rejected atomically`` → `Rejected "partial-overlap"`; non-echo replies route `Update.reconcileSubmit` to `reconcileExternalAck` instead of `reconcileAck`. The `Ignored` constructor exists; the merge-in-progress scenario from [arch.md section 11](plan/client-never-fails/arch.md) is not implemented yet.

[ ] Input 8 — **Flag:** [src/Client/Update.fs](src/Client/Update.fs) `applyIgnoredSubmitAnswer` runs `SyncAnswer.applyChangeSuccess` and `continueAutoWant` only; it does not emit `SubmitPendingBatch` or otherwise post the rest of pending after `AckReconcile.Ignored`.

[ ] Input paths `reconcileAck` in [src/Shared/SyncLogic.fs](src/Shared/SyncLogic.fs) and post-`Ignored` client handling in [src/Client/Update.fs](src/Client/Update.fs) are the edit surfaces named on the ticket. **Paths match; behavior for Input 7–8 does not.**

#### Ticket claims that differ from the code today

[ ] [10 — Merge in progress](plan/client-never-fails/issues/10-merge-in-progress/10-merge-in-progress-ticket.md) process step 2: accept a reply that carries our own changes, and return `AckReconcile.Ignored` for a reply with different changes while more changes are pending — differs from current `Rejected`, `reconcileExternalAck`, or retired-id-only `Ignored` paths above.

[ ] Process step 3: after `Ignored`, the Browser posts the rest of pending and waits for a matching reply — differs from `applyIgnoredSubmitAnswer`, which applies the reply graph only and returns no submit effects.

[ ] Architecture point 11.3 ([arch.md](plan/client-never-fails/arch.md)): Browser posts the remainder after ignore — not present on the `Ignored` arm in `applySubmitResponse`.

[ ] Named output tests ``different post reply while more changes are pending is Ignored`` and ``Ignored reply posts the rest of the pending changes`` are absent from the tree today.

#### Named new tests at public seams

[ ] ``different post reply while more changes are pending is Ignored`` in [tests/Shared.Tests/AckReconcileTests.fs](tests/Shared.Tests/AckReconcileTests.fs): call public `SyncLogic.reconcileAck` (same seam as existing AckReconcile facts) with a multi-item pending fixture (copy ``retry ACK removes only the submitted prefix and resubmits the remainder``) and a confirmed list that represents a different post reply while `later` remains queued; assert `AckReconcile.Ignored` instead of `Rejected` or `Applied`.

[ ] ``Ignored reply posts the rest of the pending changes`` in [tests/Server.Tests/BrowserResidencyClientTests.fs](tests/Server.Tests/BrowserResidencyClientTests.fs): drive public `Update.update` with `SysMsg (SubmitResponse (submitted, response))` (same entry as production post ACK handling in [src/Client/Update.fs](src/Client/Update.fs)); build a `VM` whose `syncInfo.pending` holds at least two events and whose submitted prefix matches the ticket scenario; after the update, assert an effect `SubmitPendingBatch (_, events)` whose `events` are the remaining pending changes (public effect type from [src/Shared/ViewModelSync.fs](src/Shared/ViewModelSync.fs)), and that sync state stays in-flight for the remainder rather than stalling idle with pending still queued.
