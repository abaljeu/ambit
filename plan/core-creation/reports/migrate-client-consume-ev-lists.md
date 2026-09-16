# Migrate client consume Ev lists

Date: 2026-09-16. Suggested next-seam **#1** from [[change-list-construction-sites.md|Change list construction sites]] lines 135–136. Change **list** left the client consume seams. Single Change remains as command product and as an apply/validation bridge. No commit.

## 1. Goal

Keep Ev on the Poll/POST consume path. [[src/Shared/ViewModel.fs]] `SubmitResponse` / `PollDone` and the Shared fold now take `Ev list`. Do not assemble a Change list from ack/poll events. IndexedDB boot log and server persist Change lists stay for a later seam.

## 2. Migrated sites

1. **Site 10 `decideBootPoll`** — [[src/Shared/BootCache.fs]] `decideBootPoll` filters `poll.events` with `novelEvents`. It no longer maps `Ev.asChange`. `BootPoll.ApplyNovel` is `Ev list * isReady`.
2. **Site 13 `onPostOk`** — [[src/Client/App.fs]] `SubmitChangeCallbacks.onPostOk` passes `ack.events` into `SubmitResponse`.
3. **Site 14 `runPollServer`** — [[src/Client/App.fs]] `runPollServer` passes `poll.events` into `PollDone`. Fail paths still pass `[]`.
4. **Site 15 `applySyncResponse`** — [[src/Shared/SyncLogic.fs]] folds `response.events` with `foldProjectedEvents`. No Ev→Change `List.map`.
5. **Site 16 `consumeCatchUpPoll`** — same fold. The function already took `Ev list`; it no longer builds a Change list first.
6. **ViewModel messages** — `SubmitResponse` field `confirmed` is `Ev list`. `PollDone` payload is `Ev list`.
7. **Update consumers** — [[src/Client/Update.fs]] `applySubmitResponse` and `PollDone` pass Ev to `reconcileAck` / `applyServerTail` / `consumeCatchUpPoll`. No `Ev.ofChange`. Auto-download collects ops with `Ev.ops` via `accumulateAutoDownloadFromOps`.
8. **Boot apply of novel tail** — [[src/Client/Program.fs]] `applyBootNovel` applies Ev with `applyServerTail`. Conversion to Change happens only at the IndexedDB write (section 4).

`ResidentProjection.applyOps` is the fold-via-ops helper. `applyChange` calls it. Consume no longer needs a Change record per event.

## 3. Problematic constructors (Change gone)

These build Ev from a Change (or Change fields). They are the long-term problem. This seam did not add a new one.

| # | File | Symbol | Why it is a problem |
| --- | --- | --- | --- |
| 3 | [[src/Shared/ViewModelSync.fs]] | `PendingChange.ofChange` | Wraps `Ev.ofChange ""`. Tests and command builders still enter pending through Change. |
| 4 | [[src/Shared/ResidentProjection.fs]] | `captureLoadResponse` | Maps a Change **list** with `Ev.ofChange ""` onto `LoadResponse.events`. Server load capture still thinks in Change lists. |
| 5 | [[src/Server/Core/FileAgent.fs]] | `accepted` | `confirmed` Change list → `List.map (Ev.ofChange "")` for `CoreChangesAccepted.events`. Persist apply still outputs Change, then upcasts. |
| 6 | [[src/Server/Core/DbAgent.fs]] | `accepted` | Same ofChange map as FileAgent. |
| 7 | [[src/Server/Core/CoreMailbox.fs]] | `eventFromChange` | Inline Change→Ev (`EventId 0`, empty authority/commandName, `EventBody.Change ops`) before `postEvent`. Persist door still accepts Change list. |
| 8 | [[src/Server/Core/CoreMailboxBackend.fs]] | `dispatchPostGraphOnlyChange` | Same inline Ev-from-Change as `eventFromChange` for graph-only post. |
| 9 | [[src/Shared/SyncLogic.fs]] | `applyLocalChange` | Builds Ev from Change fields (`EventId recordId`, `change.submissionId`, `EventBody.Change change.ops`). Local command path, not poll consume. Same problem as ofChange. |
| 10 | [[src/Shared/ClientHistory.fs]] | `record` | Same field copy: Change ops → `EventBody.Change` on a new Ev. |
| 11 | [[tests/Server.Tests/TestBackend.fs]] | `eventFromChange` | Test twin of CoreMailbox `eventFromChange`. |
| 12 | Tests (Shared) | `Ev.ofChange` / `PendingChange.ofChange` | Fixture assembly from Change: [[tests/Shared.Tests/BootCachePollTests.fs]] `mkPoll`, [[tests/Shared.Tests/SyncLogicTests.fs]], [[tests/Shared.Tests/SyncPlannerTests.fs]], [[tests/Shared.Tests/AckReconcileTests.fs]], [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]], [[tests/Shared.Tests/BootCacheTests.fs]], [[tests/Shared.Tests/WorkspaceUploadTests.fs]]. |
| 13 | Tests (Server) | `eventFromChange` callers | [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/CoreChangesTests.fs]], [[tests/Server.Tests/CoreRuntimeTests.fs]], [[tests/Server.Tests/CoreCredentialsTests.fs]], [[tests/Server.Tests/ChangeEndpointResilienceTests.fs]], [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]], [[tests/Server.Tests/Issue41CoreMailboxTests.fs]]. |

This seam **removed** ofChange from [[src/Client/Update.fs]] submit/poll consume and from [[src/Client/Program.fs]] `applyBootNovel` apply. No new ofChange was added on the consume path.

## 4. Remaining Ev→Change on this seam (IndexedDB only)

These are `Ev.asChange` (Ev→Change), not ofChange. They exist so sites 7–9 and 11–12 can stay Change lists.

1. **POST ack → boot log** — [[src/Client/App.fs]] `SubmitResponse` handler maps `confirmed |> List.map Ev.asChange` into `BootCache.acceptedForLog`. Required to compile without migrating `acceptedForLog`.
2. **Novel poll tail → boot log** — [[src/Client/Program.fs]] `applyBootNovel` maps novel Ev with `Ev.asChange` before `BootCacheStore.appendChanges` and `bootLog <- bootLog @ …`.
3. **`PendingChange.change`** — [[src/Shared/ViewModelSync.fs]] member is `Ev.asChange this.event`. `acceptedForLog` still uses it when confirmed is empty.
4. **`novelChanges`** — still filters Change lists for the log. `decideBootPoll` uses the new `novelEvents` instead.

## 5. Intentionally not migrated

1. **Sites 7–9, 11–12** — `changesAfter`, `acceptedForLog`, `novelChanges`, [[src/Client/BootCacheStore.fs]] codec, [[src/Client/Program.fs]] `bootLog`.
2. **Sites 1–6** — server persist Change lists (CoreEventDispatch, FileAgent/DbAgent applyBatch, PersistStamp, overlayFresh).
3. **Single Change apply bridges** — [[src/Shared/SyncLogic.fs]] `undoPendingGraph` / `projectSuffixes` still build one Change for `ResidentProjection.applyChange`. [[src/Shared/SyncPlanner.fs]] `extractChange` still builds one Change for `ChangeValidation.applyChange`. [[src/Shared/History.fs]] `Ev.inverseOps` / `Ev.apply` still build one Change. Not Change **lists**.
4. **`accumulateAutoDownloadFromChanges`** — removed. Poll consume now calls `accumulateAutoDownloadFromOps`.

## 6. Identifier repairs (WIP `changeId` → `submissionId`)

These were already broken names. They blocked Shared.Tests and the Client compile gate. They are not consume-seam design.

1. [[tests/Shared.Tests/BootCachePollTests.fs]] `losubmissionIdissionId` → `local.submissionId`.
2. [[tests/Shared.Tests/SyncLogicTests.fs]] `serverChasubmissionIdissionId` → `serverChange.submissionId`.
3. [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]] `pending.chasubmissionIdissionId` → `pending.change.submissionId`.
4. [[tests/Shared.Tests/SyncPlannerTests.fs]] four mangled Guid lists / members → `change.submissionId` / `c2`/`c3`/`undo`/`redo` / `queued.change.submissionId`.
5. [[src/Client/BootCacheStore.fs]] `chasubmissionIdissionId` → `change.submissionId` (IndexedDB encode; site 11, compile-only).

## 7. Verification

1. **Shared** — `dotnet build tests/Shared.Tests -c Debug` succeeded (0 warnings, 0 errors). That build includes Shared.
2. **Focused tests** — `BootCachePollTests` and `SyncLogicTests`: 48 passed, 0 failed.
3. **Client compile gate** — `./scripts/client.sh build` succeeded (Fable + bundle).
4. **Server** — not built. This seam did not edit Server except that FileAgent/DbAgent ofChange remain as inventoried.
5. **Full suite** — not run.

## 8. Next seam

Boot IndexedDB log (inventory sites 7–9, 11–12): keep Ev in `bootLog` / `appendChanges` / `decodeChange`, then delete the two `List.map Ev.asChange` bridges in section 4.
