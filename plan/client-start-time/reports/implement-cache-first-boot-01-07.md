# Cache-first boot tickets 01–07 — implementation report

Date: 2026-08-27
Branch: `w/relaxed-concurrency`
Parent: [[cache-first-boot-via-poll.md]], [[plan/client-start-time/project.md]]

## Starting tree

`bash ./status.sh` showed project branch `w/relaxed-concurrency`. Working tree was clean at HEAD `d643d8b`. No files existed under [[plan/client-start-time/issues/]]. [[project.md]] was already `Stage: active`. No prior ticket or `src/` cache-first work was present, so this session published issues 01–07 and implemented them in order.

## Tickets published

All seven issues live under [[plan/client-start-time/issues/]]. Criteria are checked. [[project.md]] stays `Stage: active`. [[plan/index.md]] was regenerated.

## What each ticket delivered

01 Persist bootstrap snapshot after `/state`. Shared envelope (`gambol-boot-cache-v1`, `codecVersion`, `file`, `scopeKey`, `stateJson`). After a successful `/state` decode, the Browser writes store `snapshots` and clears store `changes`. `pagehide` still writes only Session and pending.

02 Append accepted Change on retire. Shared `changesAfter` / `acceptedForLog`. After submit retire (pending length dropped and not `ServerRejected`), the Browser appends the accepted Change. Pending still clears through `gambol-pending-v1`. Snapshot is not rewritten per edit.

03 Warm F5 fold then first paint. Shared `decideBootRead` / `foldLog` (`ResidentProjection.applyChange`). `BootCache.enabled = true`. Hit: IndexedDB read, fold Δ onto F₀, `StateLoaded`, Session restore, pending merge, first paint. Miss/codec/file/scope/decode/fold: `/state`. Flag off still fetches `/state`.

04 Immediate boot Poll after first paint. `GET /{file}/poll?rev={clientRev}` runs after first paint (interval still starts once). Shared `novelChanges` skips local `id` / `changeId`. Empty or duplicate-only tail updates `isReady`. CodeOutdated uses the existing banner.

05 Novel tail and `/state` fallback. Novel tail applies through `SyncLogic.applyServerTail`, then appends. Apply error, `poll.revision < clientRev`, or oversized novel count / Revision gap (64) deletes the cache and fetches `/state`. Scope/codec mismatch remains a slice-3 miss.

06 Idle snapshot truncation. Shared `shouldTruncate` (log length 32 or Revision gap 32) and `truncationGraph` (`bootstrapGraph RootClosure savedZoom`). After paint, retire, or novel apply, a 2.5s idle timer rewrites a bootstrap-scoped snapshot and clears the log. Load-only nested Workspace children are omitted. No snapshot write on `pagehide`.

07 Optional Poll `bootstrapHash`. `ChangeSuccessResponse.bootstrapHash` is `string option` (encoder omits None; missing field decodes as None). Shared `graphFingerprint` of ROOT-closure headers. Equal Revision with both hashes present and unequal falls back to `/state`. Server fills the field when Poll Revision equals the client Revision.

## Files changed

Shared: [[src/Shared/BootCache.fs]], [[src/Shared/Gambol.Shared.fsproj]], [[src/Shared/ApiResponses.fs]], [[src/Shared/ApiResponseSerialization.fs]], [[src/Shared/SyncLogic.fs]], [[src/Shared/ViewModel.fs]] (`BootGraphApplied`).

Browser: [[src/Client/BootCacheStore.fs]], [[src/Client/Gambol.Client.fsproj]], [[src/Client/Program.fs]], [[src/Client/App.fs]], [[src/Client/Update.fs]].

Server: [[src/Server/Api.fs]] (`getPoll` hash), [[src/Server/DbAgent.fs]], [[src/Server/FileAgent.fs]] (ack `bootstrapHash = None`).

Tests: [[tests/Shared.Tests/BootCacheTests.fs]], [[tests/Shared.Tests/BootCachePollTests.fs]], [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]], plus `ChangeSuccessResponse` constructors in [[tests/Shared.Tests/SyncLogicTests.fs]], [[tests/Shared.Tests/SerializationTests.fs]], [[tests/Shared.Tests/LargeChangeApplyTests.fs]].

## Tests

Focused `dotnet test tests/Shared.Tests -c Debug --filter FullyQualifiedName~BootCache`: 36 passed (envelope, fold, Poll decision, truncation, fingerprint, hash fallback).

`FullyQualifiedName~SyncLogicTests.getPollOutcome|FullyQualifiedName~SerializationTests.ChangeSuccess`: 11 passed (including omit and round-trip of `bootstrapHash`).

`dotnet build src/Client` and `dotnet build src/Server`: succeeded. Full suite was not run.

## Remaining gaps

HITL: warm F5 with IndexedDB present; Network shows no `/state` on a valid cache; first paint includes accepted edits; DevTools snapshot size.

Poll network failure after cache paint currently no-ops; design wants retry and background `/state` after repeated failure.

Equal-Revision `getPoll` calls `getState` to fingerprint; a revision-keyed hash on the agent would avoid that mailbox cost every 5s.

[[src/Client/App.fs]] was already over 400 lines and grew for retire-append and idle truncate.

## WORK.md mutations (parent should apply)

- `remove` [[plan/client-start-time/reports/cache-first-boot-via-poll.md]] from Pending (slices 1–7 are in the tree).
- `add` [[plan/client-start-time/reports/implement-cache-first-boot-01-07.md]] — HITL verify cache-first boot (IndexedDB snapshot, no `/state` on warm F5, first paint includes accepted edits).
