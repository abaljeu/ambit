# Stale view — cited possibilities

Date: 2026-08-28
Branch: `selective-client-sync` (ahead 8); working tree clean per `status.sh`.

## Classification

**General client–server view sync** (cache-first boot, Poll, selective residency) — not [[.scratch/expression-language/reports/pipeline-examples.md]]; that page documents Expression syntax only and does not touch Browser Graph residency or Sync.

## Possibilities

1. **Warm F5 serves an IndexedDB bootstrap snapshot before the server confirms.** Cache hit folds snapshot F₀ plus Change log, paints, then boot Poll; truncation or novel-tail gaps can leave an older graph visible until Poll applies or falls back to `/state`. — **partial impl** ([[src/Shared/BootCache.fs]], [[.scratch/client-start-time/reports/implement-cache-first-boot-01-07.md]]).

2. **`bootstrapHash` mismatch refetches `/state` or skips confirm.** Fable vs .NET fingerprints diverged; fix uses empty cached hash after `/state`, HITL still open on Selection jumping to ROOT in a loop. — **wrong impl** (fix in tree, HITL pending) ([[.scratch/client-start-time/reports/poll-hash-fallback-loop.md]], [[src/Shared/BootCache.fs]] `cachedHashForBootPoll`).

3. **Poll tails are ignored while `syncState` is Loading.** `PollDone` returns early during Load so Revision is not advanced ahead of package apply; the UI can show pre-Load graph until Load finishes. — **known gap** ([[src/Client/Update.fs]] ~L220–225).

4. **Auto-sync blocked by pending Changes or a dirty edit field.** `isAutoSyncBlocked` skips `applyServerTail` on `DataOutdated`; server-ahead Changes are not merged until the queue clears or the user commits the edit. — **known gap** ([[src/Client/UpdateHelpers.fs]] `isAutoSyncBlocked`, [[src/Client/Update.fs]] ~L294–296).

5. **Selective loading: Browser holds a partial resident projection.** Unloaded Nodes are intentional hollow stubs; server has full Children. User may read this as “old” when content was never Fetched. — **partial impl** ([[.scratch/selective-client-loading/spec.md]], [[CONTEXT.md]] Loaded/Unloaded/Resident).

6. **Server structural Changes under Unloaded parents do not apply on the client.** `applyServerTail` skips structural Replace when the parent’s Children are Unloaded until Load brings the list. — **known gap** ([[tests/Shared.Tests/SyncLogicTests.fs]] ``applyServerTail skips structural Replace on Unloaded parent``).

7. **Load Workspace demotes rediscovered Added paths from Current to Unparsed.** Active fix: reconcile should keep Current when the server already parsed the file. — **wrong impl** ([[.scratch/parse-load-demote/issues/01-keep-current-on-rediscovered-added.md]], [[src/Shared/dotnet/LazyLoadReconciliationApply.fs]]).

8. **CodeOutdated / server restart via build stamps.** Poll compares `buildEpochSec` and `pageBuildEpochSec`; mismatch sets sync risk and does not auto-refresh (Ack does not reload code). — **partial impl** ([[src/Shared/SyncLogic.fs]] `getPollOutcome`, [[WORK.md]] page-stamp watch decision).

## Narrowed (idle tab, remote change, refresh fixes)

**User facts:** tab open and inactive a long time; Graph changed elsewhere; changes never appeared; hard refresh matches server. **Rules out** warm-F5 IndexedDB snapshot (refresh is correct authoritative `/state`).

### Model never changed (Poll / apply path)

1. **Poll gated off while inactive.** `pollForRemoteChanges` skips `PollTick` when `document.hidden` or no user activity for 15 min (`idleTimeoutMs`); hidden also clears `isPollingActive`. — **known gap** ([[src/Client/App.fs]] L94–95, L676–686, L704–711).

2. **`DataOutdated` stops all future Polls.** One `PollDone` that sets `syncState = DataOutdated` without applying (empty tail, `isAutoSyncBlocked`, or apply error) leaves `tryStartPoll` permanently idle-only-blocked; `AckSyncRisk` hides the banner but does not return to `Idle`. — **known gap** ([[src/Shared/SyncPlanner.fs]] L103–112, [[src/Client/Update.fs]] L293–305, [[src/Client/Overlays.fs]] L205).

3. **Dirty edit or pending queue blocked merge.** `isAutoSyncBlocked` skips `applyServerTail`; non-empty `pendingChanges` also blocks `tryStartPoll`. Graph and Revision stay at the pre-remote baseline. — **known gap** ([[src/Client/UpdateHelpers.fs]] L205–221, [[src/Shared/SyncPlanner.fs]] L104–105).

4. **Structural Changes under Unloaded parents skipped.** `applyServerTail` does not install new Children there; refresh `/state` delivers the scoped resident subgraph. — **known gap** ([[tests/Shared.Tests/SyncLogicTests.fs]] ``applyServerTail skips structural Replace on Unloaded parent``).

### Model changed, view stale (user suspicion)

5. **Plausible for collapsed subtrees.** `reconcileSiteMapFrom` only walks expanded entries; collapsed nodes keep `children = []` and `childrenStale = true` even when `graph.nodes` gained descendants — Poll may advance Revision and Graph while visible rows omit the changed branch until expand. — **by design** ([[src/Shared/ViewModelSiteMap.fs]] L102–108, L173–176; [[src/Shared/ViewModel.fs]] L31).

6. **Less likely for visible-row text/header edits.** Successful `PollDone` runs `withSiteMap` then `patchDOM` on every dispatch (no hidden-tab render skip); `planPatchDOM` emits `SetText` when graph text differs, including on the editing row. — **render path exists** ([[src/Client/App.fs]] L628–662, [[src/Shared/ViewModelDomPlan.fs]] L198–202; [[tests/Shared.Tests/ViewModelTests.fs]] ``planPatchDOM editing row text change produces SetText not RecreateRow``).

7. **Refresh vs incremental patch.** F5 uses `StateLoaded` → full `View.render`; idle Poll uses incremental `patchDOM`. A caught `patchDOM` exception leaves DOM old while `model` holds the new Graph ([[src/Client/App.fs]] L669–671). Rare; distinguish by whether client Revision advanced without visible row change.

## Banner stuck on “Starting up…”

**User facts:** yellow `#sync-status` label **predates BootCache**; UI (outline, edits) can work while the banner never leaves “Starting up…”. **Not** the static `#amb-document` “Loading…” placeholder ([[.scratch/client-start-time/research.md]]).

**Label map:** `"Starting up…"` + class `amb-syncing` (yellow `#ff9`) when `not syncInfo.isServerReady` — **before** any `syncState` branch ([[src/Client/StatusView.fs]] L11–13; [[src/Server/wwwroot/style.css]] L296–298). After ready: `Idle` → `"synced"` / `"idle"` (green); `Polling` → `"Checking…"`; risk states → red stale copy. **No** `SyncState.Starting`; **Ack** only dismisses the blocking overlay, not this label ([[src/Client/Overlays.fs]] L168–205, [[src/Client/Update.fs]] L148–149).

**Pre-cache handshake (still current):** `SyncInfo.initial` has `isServerReady = false` ([[src/Shared/ViewModelSync.fs]] L74). Cleared only when a response sets `isReady = true`: `StateLoaded` from `GET /{file}/state` ([[src/Client/Update.fs]] L121–145, [[src/Client/Program.fs]] L85–203), or `PollDone` / `LoadDone` / `BootGraphApplied` via `SyncInfo.withServerReady` ([[src/Client/Update.fs]] L211–218, L324–343). Boot sequence: `/state` → paint → `runBootPoll` ([[src/Client/Program.fs]] L190–194). Server `isReady` = DbAgent startup projection sweep finished ([[doc/current/persistence-model.md]] L31–33, [[src/Server/DbAgent.fs]] L63–66, L416–419); `/state` may return the full Graph while `isReady` is still false.

**Ranked (pre-cache first):**

1. **`/state` returned `isReady = false` and no later Poll carried `isReady = true`.** Graph still installs; banner stays yellow. Boot poll failure is silent (`runBootPoll` empty `onPollFail` — [[src/Client/Program.fs]] L187–188); periodic poll failure sends `PollDone` with `readyOpt = None` ([[src/Client/App.fs]] L423–426), leaving `isServerReady` unchanged. — **known gap**

2. **Server maintenance never completes.** DbAgent `ready.TrySetResult` never runs; every state/poll keeps `isReady = false` ([[.scratch/owner-edge-db-repair/user-report.md]]). UI reads work; banner stuck indefinitely. — **known gap**

3. **Same inactive-tab Poll gate as narrowed idle case.** `pollForRemoteChanges` skips ticks when hidden or idle >15 min ([[src/Client/App.fs]] L676–686) — if boot left `isServerReady = false`, the handshake Poll that would flip the banner may never run on a long-inactive tab. — **known gap** (orthogonal to BootCache)

4. **BootCache (secondary only):** cache hit could paint from a snapshot with stored `isReady = false` before a confirming poll — same banner semantics, not the origin of the label ([[src/Shared/BootCache.fs]] L136–139). User says yellow label is older; treat as amplifier, not root cause.

**Naming:** **Not** “sync download broken” — `/state` download can succeed. Correct name: **server readiness (`isReady`) never reported true on the client**, or **banner-only** if Polls merge Changes but `isReady` stays false. **Weak link to missed remote Changes:** banner stuck alone does not block `applyServerTail`; missed Changes still fit Poll-gated-when-hidden / `DataOutdated` trap unless Polls are also not running.

**F5#1 yellow whole session, F5#2 green (user):** First `GET /state` after process start can return the full Graph with `isReady = false` while DbAgent’s projection sweep still runs on `Task.Run` ([[tests/Server.Tests/DbAgentTests.fs]] ``DbAgent serves reads while sweep buffers FIFO mutations then trims``, [[src/Server/DbAgentStartup.fs]] L19–40); duration is DB-sized (tests allow up to ~2 s, not fixed). Client paints, sets `isServerReady` from that false, fires one boot Poll ([[src/Client/Program.fs]] L190–194); if sweep is still running or boot Poll **silently fails** (decode/network — L187–188), banner stays yellow even after the server is ready unless a **later** Poll delivers `isReady = true` ([[src/Client/Update.fs]] L212–218). F5#2 then hits `/state` with sweep done → `isReady = true` immediately → green **synced** without waiting. Looks random when it is mostly **race-with-server-start** plus **non-deterministic boot Poll drop**; hash fallback is BootCache-only (secondary). **Idle-tab missed Changes:** same Poll gate can co-occur; banner-only readiness is a **separate first-handshake bug** if periodic Polls still merge tails while `isReady` stays false.

## Clarifying questions (for parent → user)

1. **Which view is wrong?** Whole graph after F5, one Workspace after Load, one Node’s text, or SiteMap/fold layout only?
2. **What happened immediately before?** Hard refresh, Load Workspace, Parse, server restart, esbuild/Fable rebuild, or edit in another session?
3. **Does Ctrl+F5, clicking Ack on the sync banner, or completing Load fix it?** Distinguishes cache-first boot vs blocked auto-sync vs intentional Unloaded residency.
4. **While stale, was sync status “idle” / “Data changed on server”, or still “synced”?** Separates Poll-never-ran (model stale) from Revision advanced (model may have changed, view projection lag).
5. **Was the changed Node under a collapsed fold?** Separates collapsed SiteMap omission from a true patch miss on a visible row.
6. **Does F5 clear the yellow “Starting up…” or does it stay yellow after refresh?** Stays yellow → server `isReady` or poll handshake; clears → idle-tab Poll gate on a session that already passed startup.

## Azure idle / first GET

Production is **Azure App Service** Web App `Amble` (Linux, .NET 10) per [[doc/reference/deploy-azure.md]], [[doc/reference/postgres-environments.md]]; cPanel only proxies `/ambit`. Repo documents **Postgres** stop/start for cost ([[scripts/azure.sh]] restarts the web app after DB start/stop); it does **not** record Always On, App Service idle timeout, or scale-to-zero — [[.agents/skills/azure/SKILL.md]] has no idle facts either. **Fresh ASP.NET process:** DbAgent re-runs the projection sweep; `/state` can return the full Graph with `isReady = false` until sweep completes ([[doc/current/persistence-model.md]] L31–33). **Same warm process:** in-memory `isReady` stays true; no sweep. F5#1 yellow whole session then F5#2 green **matches cold process + sweep race**, not warm reactivation. Distinguish: `buildEpochSec` (`DeployEpochSec` = process start — [[src/Server/RouteRegistration.fs]] L197–218) changes only on new process; Azure Log stream shows full startup after idle; warm wake would show `isReady = true` on first `/state`.
