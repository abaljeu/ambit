# Look-ahead: remaining Undo slices after 3a

Source: live 3a tree on `w/selective-client-loading-undo` (no `implement-undo-slice-3a.md` yet). Plan left unchanged.

## 1. What 3a left on the table

**Already converted** (queue owns ordinary Change + optional transition):

- `PendingChange` / `PendingTransition` / `PendingKind` in [[src/Shared/ViewModelSync.fs]]
- `pendingChanges`, `WaitingToRetry`, `SubmitPendingBatch`, `SubmitNetworkError`, `SavePendingQueue`
- `SubmitResponse.submitted` threaded from `runSubmitPendingBatch` → `onPostOk` (logged only; `ackBatch` still uses ACK IDs)
- `ContinuePostUploadStructure` carries `PendingChange`; `applyAndPostSync` builds `workspaceSingleton` before POST
- Save/load of `PendingChange`; old `ChangeRequest` localStorage is a decode fallback
- `restorePending` strips transition, applies via `History.applyChange`, does not recreate History
- Tests in [[tests/Shared.Tests/SyncPlannerTests.fs]]: snapshot isolation, same-`recordId` C/U/Redo batching, restore, workspace singleton

**Still `ChangeRequest`** (wire / enqueue / Server):

- `PendingChange.toChangeRequest` / `SyncBatch.toWireBatch` map Undo/Redo *kinds* to Ops-less `ChangeRequest.Undo`/`Redo` (test-locked around `mixed C Undo Redo delta chain…`)
- `encodePendingBatchBody`, `ChangeBatch`, Server `applyBatch`, `History.applyAction`
- `applyAndEnqueueLocalAction` still takes `ChangeRequest`; `applyAndPost` / `undoOp` / `redoOp` still wrap that way
- Queue *bodies* already hold materialized Ops; the adapter throws them away on the wire

**Intentionally deferred:**

- ACK prefix validation (submitted list is present, unused). Workspace ACK still `PersistStamp.applyToGraph`; `completeUploadStructurePost` does `ignore submitted`
- `VM.history` still canonical `History`; `recordId` 0 on `fromRequest` (real ids are Slice 3b)
- Complete confirmations, Server History removal, provenance names, measurement

## 2. Slice-by-slice

### 3b — runtime History + projected local flow

Wire ClientHistory, optimistic Undo/Redo through ResidentProjection, Poll/Load/package-only rules. Compile-bridge: keep `History.applyAction` until 5.

- **3a coupling:** enqueue must stop going through `ChangeRequest` + `applyAction`; attach real `PendingTransition` from ClientHistory. Wire adapter still strips inverse Ops.
- **Complexity: high.** `VM.history` vs `State.history` split; `applyAndPost` / undo / redo / SyncLogic `History.empty`; Poll/Load guards; many Client callers.
- **Thrash if current order:** high unless the first 3b step always encodes queued items as `ChangeRequest.Change` (send Ops). Otherwise local `Change.inverse` and Server `Undo` materialization diverge on create/paste until 4.

### 4 — wire and Server to Change-only confirmations

Replace action batches and ACK fields; return durable complete Changes; keep stamp assignment and the route.

- **3a coupling:** delete `toChangeRequest` / `ChangeBatch` of `ChangeRequest`; duplicate retry must return stored complete Change (today duplicate ACK can omit stamps).
- **Complexity: high.** Codec + FileAgent + DbAgent + Serialization/StateEndpoint/FileAgent tests. No compatibility decoder.
- **Thrash:** medium if 4 maps complete Changes back down to IDs+`stampOps` and 5 rebuilds. 4 already lists `Update.fs` / `App.fs` / `UpdateCodec.fs`.

### 5 — ACK recon + remove legacy History

Use `SubmitResponse.submitted` (already there). Validate prefix/suffix, retire transitions, project suffixes, ignore late full duplicates, route workspace singletons, then delete `ChangeRequest`, Server History, ACK ID aggregates, direct stamp apply.

- **3a coupling:** plumbing done; validation and workspace `ignore submitted` are the work.
- **Complexity: high.** Test matrix (Normal/U/Redo, same-batch, residency, retry, late duplicate, rejection, both workspace paths) plus deletion sweep.
- **Thrash:** low if 4 already put complete Changes on `SubmitResponse`. High if 5 also invents the ACK message shape.

### 6 — command provenance and feedback

Pass resolved names at event sources; Undo/Redo result text.

- **Complexity: low.** Mechanical if 3b already records *some* name.
- **Thrash:** none. Do not pull this into 3b.

### 7 — verify and measure

Focused suites, Browser build, large-paste timings.

- **Complexity: medium** (measurement, not design). Needs 3b–5 present.

## 3. Realignment

| Option | What | Verdict |
| --- | --- | --- |
| Keep order | 3b → 4 → 5 → 6 → 7 | Right dependency order |
| Swap 3b/4 | Wire first | Blocked: Browser still sends Ops-less Undo/Redo |
| Merge 4+5 | One cut | Bigger, not easier |
| Split 5 | 5a recon / 5b delete | Optional later if 5 drowns; not now |
| Tiny pull-forward | 3b starts by encoding always `ChangeRequest.Change` | **Do this** |

**Recommend: keep the order.** The one easing is inside 3b’s first step (or a one-function leftover before it): stop mapping Undo/Redo kinds back to `ChangeRequest.Undo`/`Redo`. Queue already has Ops; send them. Then 3b can switch local inversion without waiting for 4, and 4 is ACK + dropping the DU cases rather than a second inversion cut.

Do not pull ACK validation into 3b. Do not convert retry off `ChangeRequest` as its own slice — the adapter dies in 4 once encoding is Change-only.

## 4. Complex slices

- **3b — high:** Client/Shared History split, runtime callers, Poll/Load, leftover wire adapter.
- **4 — high:** wire protocol, both backends, duplicate complete-Change return, large tests.
- **5 — high:** reconciliation rules + workspace paths + leftover deletion. Plumbing from 3a is not the hard part.
- 6 low, 7 medium.

## 5. Questions for the human

None. Spec already requires sending ordinary Changes; the leftover adapter is a sequencing seam, not a product fork.

## Proposed WORK.md mutation

- `add` [[plan/selective-client-loading/look-ahead-remaining-slices.md]] to Pending — remainder realignment note before Slice 3b.
