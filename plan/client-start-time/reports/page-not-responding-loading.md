# Page not responding — ROOT Loading...

Date: 2026-08-29
Branch: `selective-client-sync` (report only; no checkout)
Parent: [[../project.md]], [[cold-load-loading-hang.md]], [[bucket-3-post-state-work.md]], [[cache-first-boot-delayed-lcp.md]]
Screenshot: yellow **Starting up...**, green **DB synced**, expanded **ROOT** row, **Loading...**, Chrome **This page isn't responding** (Wait / Exit page) on Ambit.

## Direct answer (IndexedDB, Edge vs Chrome)

IndexedDB **does persist** across exiting the desktop process. Exiting WebView2/Edge does **not** clear it. There is **no** shutdown delete.

That **does not** explain an Edge-only hang on the **current** boot path. [[src/Client/Program.fs]] always calls `loadFromState` (`GET /state`). It never calls `BootCache.decideBootRead` or `BootCacheStore.readSnapshotAndLog` at startup. `BootCache.enabled = true` is unused. The cache is written **after** first paint (`setTimeout 0`). A huge persisted snapshot cannot decode on boot unless an older bundle still did cache-first read.

Edge/WebView2 hangs while Chrome does not because they are **different origins and profiles**, not because process exit wipes cache:

- Desktop WebView2 user data is `%LocalAppData%/Gambol/WebView2` ([[doc/current/desktop-local-files.md]], [[src/Desktop/Desktop.fs]] `userDataFolder`). IndexedDB, localStorage, and sessionStorage stay there across relaunch.
- Chrome at `/ambit` uses the Chrome profile. Empty cache vs a warm WebView2 profile is expected.
- Desktop also runs **blocking** `XMLHttpRequest` ledger sync (`getJsonSync` / `postJsonSync`). Chrome `/_desktop/capabilities` fails; that path does not run.

Daily git save is **not** implicated. [[src/Server/DailyGitSave.fs]] `register` uses `ApplicationStarted` + `Task.Run` `commitAll`. That is a server subprocess. It cannot freeze the browser thread unless the client then does huge sync compute after a response.

## Frozen frame — what each badge is

| Pixel | Source | Meaning |
| --- | --- | --- |
| Yellow **Starting up...** | [[src/Client/StatusView.fs]] `renderSyncStatus` | `not model.syncInfo.isServerReady`. Initial VM is false ([[src/Shared/ViewModelSync.fs]]). Set true only from `/state` or Poll/Load `ready` ([[src/Client/Update.fs]] `withServerReady`). |
| Green **DB synced** | same file `renderDatabaseStatus` | `window.__DB_PRESENT__ === "ok"` ([[src/Client/JsInterop.fs]] `readDbPresent`). HTML inject in [[src/Server/RouteRegistration.fs]] (`dbStatusText` Ok → `"ok"`). Independent of `isServerReady`. PostgreSQL matched files at **page** serve time. |
| **ROOT** + chevron | SiteMap zoom-root row | `View.render` / `makeRowElement`. Default zoom is `firstGraphChild` (first child of ROOT), not ROOT; a ROOT header means session restore set `zoomRoot` to ROOT ([[src/Client/SessionState.fs]] `z`) or ROOT had no children. |
| **Loading...** under ROOT | HTML placeholder | [[src/Server/wwwroot/gambol.template.html]] text node in `#amb-document` before `#hidden-input`. Not the StatusView `Loading…` (that is `syncState = Loading`, a Load in flight). |
| Chrome dialog | main-thread watchdog | Long synchronous JS. Not a waiting fetch. Fetch callbacks run only after the current turn ends. |

`renderStatus` runs from `renderSyncChrome` in the dispatch `finally` ([[src/Client/App.fs]]). Any completed dispatch (desktop capabilities **or** `StateLoaded`) can paint the two badges. The HTML **Loading...** stays until `View.render` removes previous siblings of `#hidden-input`.

`isReady` on the server is `DbAgent.ready.Task.IsCompletedSuccessfully` ([[src/Server/DbAgent.fs]] `stateResponse`). `GET /state` can return a graph with `ready: false` while the agent is still starting. Then the client can paint ROOT and still show **Starting up...**. File-backed `AgentHandle` reports `isReady = true` always.

## How the three appear together, then the watchdog

Parallel boot fetches ([[src/Client/Program.fs]]): `/_desktop/capabilities`, `/{file}/capabilities`, `GET /{file}/state` (optional `?zoom=`).

1. Desktop capabilities often finish first. Dispatch paints **Starting up...** + **DB synced**. Document still has HTML **Loading...**. `runEffects` then calls `runWorkspacePathSyncSnapshot` ([[src/Client/App.fs]]): **synchronous** GET mappings plus POST ledger once per mapped label ([[src/Client/JsInterop.fs]] `xhr.open(..., false)`). That turn does not yield. `/state`’s callback cannot run until it finishes. Chrome web: capabilities fail → `DesktopCapabilitiesDetected None` → no ledger.
2. When `/state` runs on the main thread: `decodeStateResponse` → `Graph.fromNodes` inside `decodeGraph` → `StateLoaded` `buildSiteMapFrom` → `restoreSessionState` (`applyFoldSession`) → `mergePendingAfterLoad` → `View.render`. All of that is synchronous before the next paint commit. Polling starts only after `finishPaint` returns (`ensurePolling`).

`View.render` **removes** the **Loading...** text node, then inserts rows. A frozen frame with **both** the ROOT row and the template string is a slight mismatch with that order. Most likely: last paint is a capabilities frame (badges + template **Loading...**) **or** `StateLoaded` began and the zoom-root row committed while the main thread was still inside decode / fold / remaining row creation. Do not treat that as a second Loading badge.

## Likely causes (confirm / reject)

| Candidate | Verdict |
| --- | --- |
| Large `/state` decode | **Plausible, not Edge-only.** `decodeStateResponse` is on the main thread before dispatch ([[src/Client/Program.fs]]). `Decode.resizeArray` already replaced O(n²) `Decode.list` ([[src/Shared/Serialization.fs]]). Remaining cost is per-node Thoth decode + `Graph.fromNodes`. Production scoped bootstrap was ~1800 nodes / ~3.7M chars ([[decode-list-append-hotspot.md]]). Enough to feel slow; watchdog (~10 s) needs a bigger graph, a huge fold expansion, or extra desktop work. |
| `Graph.fromNodes` | **Part of decode, not a separate boot read.** [[src/Shared/GraphBuild.fs]]: `requireValidChildrenStatus` (full `Map.iter`), `ensure*` system nodes, `buildParentMaps`. O(nodes). Same for Chrome and Edge given the same JSON. |
| `applyFoldSession` / `restoreFoldOccurrences` | **Root cause (corrected).** Node-keyed fold state (`e: NodeId[]`) expanded every runtime appearance of each saved NodeId. On a Ref cycle, each expand creates a fresh appearance; BFS never empties. Raw `SiteId` values are not stable across refresh, so occurrence identity must be parent index + child index + NodeId. Fixed: [[plan/client-start-time/reports/restore-fold-occurrences.md]]. DevTools `Map.ofSeq` in `buildParentInstanceIndex` was a secondary cost amplifier, not the fundamental loop. |
| SiteMap full walk | **Rejected as the first walk.** `buildSiteMapFrom` expands only the zoom root; children stay collapsed. A full visible tree is `applyFoldSession` then `getVisibleInstanceIds` / `View.render`. |
| Cache-first boot applying a huge graph on the main thread | **Rejected on current Program.fs.** Issue 09 removed the IndexedDB wait ([[cache-first-boot-delayed-lcp.md]], [[../issues/09-cache-first-boot-delayed-lcp.md]]). Persist still runs after paint. `requestIdleTruncate` reads IndexedDB at 2500 ms **after** `finishPaint` and may re-encode the graph on the main thread — too late to leave HTML **Loading...**. |
| Poll loop spinning | **Rejected for this frame.** `startPolling` is after `StateLoaded` dispatch returns. An infinite Elmish loop would need `dispatch` from `update`/`render`; `PollTick` only `tryStartPoll`. StatusView **Loading…** is Load-in-flight, not this screenshot’s document text. |
| Infinite Elmish update | **No evidence.** `runWorkspacePathSyncSnapshot` dispatches once when the XHRs finish. |
| Daily git | **Rejected.** Server `Task.Run` only. |

## Most likely blocking call

**If this is desktop Edge/WebView2 and Chrome at `/ambit` does not hang:** warm `gambol-session-v1` with a **legacy Node-keyed** `e` array over a graph with Ref cycles — infinite expand before first paint. Secondary cost: `Map.ofSeq` in `buildParentInstanceIndex` per expand (mitigated on `w/sitemap-parent-index`). Occurrence-based `f` snapshots ([[restore-fold-occurrences.md]]) fix the loop; legacy `e` payloads restore collapsed.

**If both browsers hang on the same origin and empty session:** `decodeStateResponse` (including `Graph.fromNodes`) on a large `/state` body, still on the main thread.

**Not IndexedDB decode on current boot.** Persisted IDB can still make the **next** warm F5 expensive **if** cache-first read is turned back on, and truncate/re-encode can hitch 2.5 s after a successful paint.

## IndexedDB (what is stored)

Database `gambol-boot-cache-v1` ([[src/Shared/BootCache.fs]]). Stores: `snapshots` (key `file`), `changes` (key `[file,id]`, index `byFile`). Snapshot fields: `codecVersion`, `file`, `scopeKey`, `revision`, `isReady`, `stateJson` (bootstrap `/state` JSON), `writtenAt`, `bootstrapHash` (written empty). Change log: accepted `Change` JSON after submit retire.

**Not in IndexedDB:** zoom and folds (`gambol-session-v1` in sessionStorage + localStorage), pending queue (`gambol-pending-v1` localStorage). Those **also** persist in the WebView2 user-data folder.

Writes: `persistAfterState` after `/state` (not on `pagehide`). Append on submit retire. Delete: `deleteCache` only on boot Poll **fallback** to `/state` ([[src/Client/Program.fs]] `fallbackState`). No `indexedDB.deleteDatabase`. No exit/shutdown clear.

Can a persisted payload explain Edge hang and Chrome OK **in principle**? Yes, **if** boot decoded `stateJson` on the main thread (tickets 01–07). **Today it does not.** Warm **localStorage session** + desktop ledger are the current Edge-only main-thread extras.

## Same as [[cold-load-loading-hang.md]]?

**No.** That hang was a cold IndexedDB miss that never called back, so `GET /state` never started. The page stayed on HTML **Loading...** with the **main thread idle** (no Wait/Exit dialog). Fixed: split transactions, 2500 ms `decideBootReadWait`, paint `StateLoaded` first. This screenshot is a **blocked** main thread. Different mechanism. HITL for the miss-callback bug is still pending and does not cover this watchdog.

## HITL

On the hung Edge/WebView2 profile: DevTools console for `[Gambol boot] decodeStateResponse` / `restoreSessionState` / `View.render`. Network: whether `/state` finished before the dialog; whether `workspace-sync-ledger` POSTs ran. Application: IndexedDB `gambol-boot-cache-v1` size (survives relaunch; not read at boot). Compare Chrome Application storage (should be empty or smaller). Do not kill a live debug session to test this.

## WORK.md mutations (parent)

- `add` [[plan/client-start-time/reports/page-not-responding-loading.md]] — HITL: Edge/WebView2 Wait/Exit vs Chrome; confirm blocking XHR ledger and/or warm session fold vs `/state` decode; IndexedDB persists but is not boot-read (owner: parent)
- Keep [[cold-load-loading-hang.md]] HITL as the miss-callback case; do not merge the two
- Do not add daily-git work for this hang

## Addendum — Edge DevTools stack (2026-08-29)

Confirmed: after `/state` decode (already logged; not the hang), `StateLoaded` → `restoreSessionState` → `applyFoldSession` → SiteMap expand. Desktop ledger XHR is out (Edge browser, no local proxy).

| Frame (top = current) | Nearest function |
| --- | --- |
| Map.ofSeq — Map.js:1368 | Fable `Map.ofSeq` |
| ViewModelSiteMap.fs:14 | `buildParentInstanceIndex` |
| ViewModelSiteMap.fs:257 | `expandEntry` (rebuilds `parentByInstanceId`) |
| ViewModelSiteMap.fs:368 | `applyFoldSession` (BFS expand loop) |
| ViewModelOps.fs:42 | `applyFoldSession` re-export |
| SessionState.fs:105 | `restoreSessionState` |
| App.fs:594 | `dispatch` `StateLoaded` → `restoreSessionState` |
| Program.fs:189 | `finishPaint` → `dispatch (StateLoaded …)` |
| Program.fs:102 | `loadFromState` Ok → `finishPaint` |
| Program.fs:110 | `setTimeout` persist registration (adjacent; not on CPU) |
| Promise.then | fetch `/state` resolve |
| Program.fs:88 | `loadFromState` fetch success callback |
| Program.fs:198 | `finishPaint` / boot tail (line drift OK) |

On CPU: `Map.ofSeq` inside `buildParentInstanceIndex`, invoked once per `expandEntry` while `applyFoldSession` walks the warm fold set. Bounded BFS (not an unbounded loop); cost scales with expands × entries.

## Addendum — infinite vs quadratic (fix on `w/sitemap-parent-index`)

**Quadratic rebuild, not an infinite enumerator.** `Map.ofSeq` receives a finite sequence: every current SiteMap entry (`Map.toSeq` then `Seq.choose`). [[src/Shared/ViewModelSiteMap.fs]] `applyFoldSession` is a bounded BFS over the warm fold set. The enumerator ends. Each `expandEntry` then rebuilt `parentByInstanceId` with a full `Map.ofSeq`. That is O(expands × entries) Map construction. Decode was 162 ms / 6446 nodes. A large warm fold set makes the Fable tree rebuild look like it never ends. There is no skip-list for SYSTEM.

**Change:** `expandEntry` adds only the new instance→parent edges (`indexChildParents`). `buildSiteMapFrom` and `reconcileSiteMapFrom` still build the index once with `buildParentInstanceIndex`.

**Test:** [[tests/Shared.Tests/ViewModelTests.fs]] `SiteMap parent index matches full rebuild after many fold-session expands` — 40 owner-chain expands via `applyFoldSession`; index equals a full rebuild.

## Addendum — one `ofSeq` of 18568 (this pause)

`entries.size()` is **18568**. That number **disproves** a cyclic `entries` tree. Fable `FSharpMap.size` / `Count` is `MapTreeModule_size`: a Left/Right walk, not a stored count. A cycle would hang `size()` itself. The tree is finite and acyclic.

**SiteId comparer is finite and trichotomous.** [[src/Shared/ViewModel.fs]] `SiteId = Sid of int` compiles to a Fable `Union` (`tag = 0`, `fields = [int]`). `ofSeq` uses `compare` → `CompareTo` → `compareArrays` on that int. Same int ⇒ 0. No custom comparer. A bad comparer cannot livelock `Map.add`: add is one descent plus `rebalance` (fixed rotations, no loop). Duplicate keys replace and still count as one `MoveNext`.

**`instanceId` vs map keys cannot make `ofSeq` loop.** `toSeq` yields `(mapKey, entry)`. `Seq.choose` keeps `Some (e.instanceId, parent)`. If `instanceId` ≠ map key, the new map can drop or overwrite keys. The enumerator still has at most 18568 items. `Seq.choose` only skips `None` (root); it does not invent items. `ofSeq` is `mkFromEnumerator`: one `Map.add` per `MoveNext`, then stop.

**One call is O(n log n).** n = 18568, height ≈ 14, about 2.6×10⁵ persistent node allocations. That must finish in well under a second in normal JS. It is not an infinite enumerator.

**What DevTools “never exits” still means.** The debugger is **inside this one finite `ofSeq`**. Step / Step Over of 18568 adds looks like it never ends. Hit **Continue**: this frame must return. If the page still sits in `Map.ofSeq` after Continue, that is the **next** `expandEntry` rebuild (same function, new call), not this call looping. This pause is not “too many expands”; it is one 18k pass. The production fix is still incremental `parentByInstanceId` on expand so we do not pay 18k `ofSeq` on every expand.

**Test:** [[tests/Shared.Tests/ViewModelTests.fs]] `buildParentInstanceIndex of 18568 entries returns a finite index` — one call, Count = 18567, root has no key.

## Addendum — corrected root cause (2026-08-29)

The hang is not primarily `Map.ofSeq` quadratic cost. **Node-keyed session fold state** (`e: string[]` of expanded NodeIds) made `applyFoldSession` expand every runtime appearance of each saved Node. In a Ref cycle, each expand creates a new appearance of the same NodeId; the BFS queue never empties. `SiteId` values are not stable across refresh, so NodeId sets cannot identify which appearance to reopen.

**Fix:** `FoldOccurrenceSnapshot` records (parent index, child index, NodeId) in parent-first order; `captureFoldOccurrences` / `restoreFoldOccurrences` in [[src/Shared/ViewModelSiteMap.fs]]; session payload field `f` in [[src/Client/SessionState.fs]]. Legacy `e` payloads decode without `f` and restore collapsed. Report: [[restore-fold-occurrences.md]].

## Addendum — stale IndexedDB Graph vs Fold vs Zoom (2026-08-29)

IndexedDB holds a Revisioned Graph snapshot plus Change log, not Fold or Zoom. Stale cached Nodes are resolved by Change replay (`BootCache.foldLog`) or by falling back to `/state`; valid cache records are not deleted merely because later Changes remove Nodes.

Fold and Zoom live in `gambol-session-v1` (sessionStorage/localStorage):

- **Fold:** occurrence snapshots validate against the post-load Graph; missing parents, missing child indices, and NodeId mismatches are skipped. Finite restore cannot re-enter the Ref-cycle hang.
- **Zoom:** saved `z` / preferred Zoom may be absent after scoped bootstrap or after Graph replacement. [[src/Shared/ViewModelOccurrence.fs]] `resolveZoomRoot` / `retargetZoomIfMissing` keep a Resident preferred Zoom, otherwise the StateLoaded default (`firstGraphChild`). [[src/Client/UpdateHelpers.fs]] `withSiteMap` normalizes before `reconcileSiteMapFrom` and rebuilds `zoomIngress` when Zoom falls back. Covers `BootGraphApplied` and ordinary Poll/Load deletes.
- **Server `?zoom=` / `b`:** missing widen id still falls back to ROOT bootstrap scope ([[src/Shared/ResidentProjection.fs]]).

## WORK.md mutations (parent)

- Update [[plan/client-start-time/reports/page-not-responding-loading.md]] — HITL: warm F5 with occurrence-based `f` restore; legacy `e` collapses safely; confirm Zoom fallback when preferred Node is absent after replay (owner: parent)
- Keep [[bucket-3-post-state-work.md]] as optional first-paint deferral of fold restore
