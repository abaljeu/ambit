# Cache-first boot via poll — design

Date: 2026-08-27
Branch: `w/relaxed-concurrency`
Parent: [[.scratch/client-start-time/reports/reload-state-reuse-investigation.md]], [[.scratch/client-start-time/project.md]]
Explicitly **not**: two-phase visible-closure fetch ([[.scratch/selective-client-loading/reports/two-phase-state-loading-exploration.md]])
Related: [[.scratch/event-sourced-ops/overview.md]]

## Executive answer

**Feasible: yes**, under conditions. Warm reload paints from a local event-sourced replica: bootstrap snapshot **F₀** at revision **R**, plus cached **Change** events with `id > R`. The existing `GET /{file}/poll?rev={n}` endpoint is catch-up from the server log, not the source of this tab's own edits.

No new bootstrap API is required for the common warm-reload path.

**Conditions:**

| Condition | Why |
| --- | --- |
| Persist **bootstrap-scoped** snapshot (not arbitrary session residency) | Matches what `/state` installs today; avoids resurrecting Workspaces loaded only via Load unless the spec changes |
| Persist **Change** events after the snapshot revision | Accepted client edits must be in the local log so first paint is not the initial load Graph |
| Use **IndexedDB** (not localStorage) | Production bootstrap JSON is ~3.7M chars — above localStorage quotas |
| Fold local **Δ** onto **F₀** **before** first paint | Pending queue covers unacked edits only; accepted edits have left `gambol-pending-v1` |
| Run **immediate boot poll** after that paint | Catch events this tab did not cache (other actors); skip Change already in the local log |
| **Fallback to `/state`** on cache miss, decode failure, fold error, `poll.revision < snapshot.revision`, poll apply error, or scope/codec mismatch | Poll and the local log cannot repair a wrong or missing snapshot |
| Treat **CodeOutdated** as today — offer refresh; cache is advisory until new page stamps match | [[src/Shared/SyncLogic.fs]] `getPollOutcome` |
| **Spec decision** if cache ever stores full session Graph (Workspaces beyond bootstrap) | Conflicts with user story 11 ([[.scratch/selective-client-loading/spec.md]]:34) |

**What you gain:** eliminate `/state` network TTFB + download on warm F5 (~0.8–3.5 s measured). Local IndexedDB read + JSON decode (~200–1000 ms) remains on the critical path unless a faster binary cache is added later.

**What poll cannot do alone:** supply an initial Graph on cache miss; install **packages** (Load-only path); detect revision-equal Graph corruption without an optional integrity field; recover when snapshot revision is **ahead** of server; make first paint current for **this tab's** accepted edits (that is the local Change log).

---

## Event-sourced cache

This is the resident model [[.scratch/event-sourced-ops/overview.md]] already uses: **F' = apply(F, Δ)**. The cache is a local replica of that fold, not a second kind of state.

| Piece | In IndexedDB |
| --- | --- |
| Initial state **F₀** | One bootstrap-scoped Graph record at revision **R** (the `/state` JSON, or an equivalent snapshot after truncation) |
| Events **Δ** | `Change` values with `id > R` (append-only) |
| Fold | Existing `History.applyChange` / `ResidentProjection.applyChange` |
| First paint | Fold local **Δ** onto **F₀**, then merge still-pending items ([[src/Shared/SyncPlanner.fs]] `restorePending`) |
| Server | Authority for the log. Poll is catch-up. Skip any tail `Change` already in cached **Δ** (`changeId` / `id`) |

**Pending** (`gambol-pending-v1`) is the unacked prefix: keep it in localStorage until the server assigns an id, then append that `Change` to the IndexedDB log and drop it from the queue (same retire path as today).

**Do not persist `ClientHistory`.** That list is undo/redo. Reload continues to clear it ([[src/Client/Update.fs]] `StateLoaded`). Undo is a separate cursor over events; it does not need to survive F5 for first paint.

**Do not write the snapshot after every edit.** Each accepted batch is one small `Change` append. Rewrite **F₀** and drop the prefix of **Δ** only when the log is long, at idle (snapshot truncation).

**Do not use per-Node records for v1.** One snapshot record keeps **R** and the Graph in one transaction. A partial Node write at equal revision is silent corruption that poll cannot see.

**Do not mirror session hints into IndexedDB.** `gambol-session-v1` and `gambol-pending-v1` stay in localStorage / sessionStorage as today.

**Out of scope for hit-rate:** WebKit 7-day script-writable storage cap; Private Browsing. Miss → `/state`.

---

## What must be cached for correct reinit

Minimum payload to reproduce today's `StateLoaded` → `restoreSessionState` → `mergePendingAfterLoad` → first render pipeline without `/state`, with first paint matching this tab's accepted edits:

| Field | Required | Source today | Notes |
| --- | --- | --- | --- |
| `graph` | **Yes** | `StateResponse.graph` | Must be **bootstrap-scoped** (ROOT closure + optional zoom Workspace), same shape as [[src/Shared/ResidentProjection.fs]] `bootstrapStateResponse` |
| `revision` | **Yes** | `StateResponse.revision` | Snapshot **R**; events start after this id |
| Change log | **Yes** | Accepted `Change` after submit retire | `id > R`; fold before first paint |
| `isReady` | Optional (stale OK) | `StateResponse.isReady` | Poll returns fresh `ready`; boot poll overwrites |
| `scopeKey` | **Yes** (metadata) | Derived from `tryReadSavedZoomId` / `?zoom=` | Invalidate snapshot when bootstrap widen target changes |
| `file` | **Yes** (metadata) | URL path segment | Per-document cache partition |
| `codecVersion` | **Yes** (metadata) | App constant bumped with wire-format changes | Invalidate when [[src/Shared/Serialization.fs]] / [[src/Shared/ApiResponseSerialization.fs]] break compat |
| `writtenAt` | Optional | Client clock | Debugging; optional TTL policy |

**Do not cache for boot reinit** (rebuilt or read elsewhere):

| Field | Why not |
| --- | --- |
| `siteMap`, `zoomRoot`, `selectedNodes`, `mode` | Rebuilt in `StateLoaded` ([[src/Client/Update.fs]]:121–146); zoom/folds from `gambol-session-v1` via [[src/Client/SessionState.fs]] |
| `syncInfo` (beyond pending queue) | `SyncInfo.initial` on load; polling restarted fresh |
| `ClientHistory` / `history` | Cleared on `StateLoaded` (`ClientHistory.clear ()`) |
| Full session residency (extra loaded Workspaces) | Violates user story 11 unless spec updated — see [Spec conflicts](#spec-conflicts) |
| `buildEpochSec` / page stamps | Live in HTML; compared at poll time via [[src/Shared/SyncLogic.fs]] `ClientPollContext` |

**Pending queue:** stays in `localStorage` (`gambol-pending-v1`); merged after snapshot+log fold, exactly as today ([[src/Client/App.fs]] `mergePendingAfterLoad`).

---

## Cache schema (IndexedDB)

Database: `gambol-boot-cache-v1`.

**Store `snapshots`.** Key: `{file}` (e.g. `ambit`).

Value (JSON document):

```json
{
  "codecVersion": 1,
  "file": "ambit",
  "scopeKey": "root|zoom:<guid-or-none>",
  "revision": 42,
  "isReady": true,
  "graph": { "...": "StateResponse.graph wire shape" },
  "writtenAt": "2026-08-27T14:00:00Z"
}
```

Alternative v1 shortcut: store **`stateJson`** (full encoded `StateResponse` string) instead of parsed `graph` — same decode path as `/state` ([[src/Client/UpdateCodec.fs]] `decodeStateResponse`), and the boot `/state` response body can be written with no re-encode.

**Store `changes`.** Key: `{file, changeId}` or `{file, id}`. Value: wire `Change`. Index by `file` + `id` so boot can read all events with `id > R` in order.

**Scope key rules:**

- `root` when no bootstrap widen (`tryReadSavedZoomId` is `None`).
- `root|zoom:{guid}` when session `b` (or legacy `z`) widens bootstrap ([[src/Client/SessionState.fs]]:37–52, [[src/Client/Program.fs]]:67–71).
- On read: if current `scopeKey` ≠ cached `scopeKey`, treat as miss → `/state`.

---

## Can `/poll` alone bring cached state current?

Server behavior ([[src/Server/Api.fs]]:114–132):

```fsharp
if rev > clientRev then handle.getChangesSince clientRev else []
```

Changes are rows with `server_revision_after > checkpointRevision` ([[src/Server/Database.fs]]:234–236).

After local fold, `clientRev` is **R plus last cached event id** (max `Change.id` in the log, or **R** if the log is empty).

Client outcome ([[src/Shared/SyncLogic.fs]]:30–42, [[src/Client/Update.fs]]:211–322):

| Scenario | Poll response | `getPollOutcome` | Boot action |
| --- | --- | --- | --- |
| **Revision matches** (warm F5, no other-actor edits) | `revision = clientRev`, `changes = []` | `None` | Snapshot+log confirmed; update `isReady` from poll |
| **Server ahead, tail is only events already in the log** | `revision > snapshot R`, changes duplicate local **Δ** | `DataOutdated` | Skip by `changeId` / `id`; no Graph work |
| **Server ahead, new events** | non-empty changes not in the log | `DataOutdated` | `applyServerTail` → incremental SiteMap patch; append those Change to the log |
| **Server ahead, tail apply fails** | changes present, apply `Error` | `DataOutdated` | **Fallback `/state`** |
| **Server restart / deploy** | any | `CodeOutdated` when stamps differ | Existing stale-page UX; user hard-refreshes |
| **Client revision ahead of server** | `revision < clientRev` | `None` (no `DataOutdated`) | **Dangerous silent mismatch** — detect and **fallback `/state`** |
| **Pending local changes on reload** | varies | Poll gated when pending in flight | `mergePendingAfterLoad` replays pending; poll runs when idle — same as today |
| **Bootstrap scope change** | N/A | N/A | Scope key mismatch → ignore cache → `/state?zoom=` |

**Revision match with empty changes:** poll is sufficient after local fold; no `/state`.

**Packages gap:** Poll returns **changes only**, never `packages` ([[src/Shared/ApiResponses.fs]]). Package install is not needed on boot unless the cached Graph was incorrectly scoped.

**Graph integrity at equal revision:** Poll does not include a Graph fingerprint. Single-record snapshot writes keep **R** and the Graph atomic. Optional **`bootstrapHash`** on poll remains a later integrity extension, not required for v1.

---

## Gaps where poll is insufficient

| Gap | Minimal remedy |
| --- | --- |
| Cache miss / first visit | **`GET /state`** (unchanged) |
| This tab's accepted edits since snapshot | **Local Change log** (not poll) |
| Cached revision > server revision | Boot rule: **fallback `/state`** |
| Tail apply `Error` / local fold `Error` | **Fallback `/state`** |
| Scope / codec mismatch | **Fallback `/state`** |
| Revision-equal Graph drift | Optional: **`bootstrapHash` on poll** |
| CodeOutdated | User refresh (new HTML stamps) |
| Need full session residency on F5 | **Spec change** + cache policy change — not solvable by poll or the log |
| Poll tail larger than a fresh bootstrap (other-actor flood) | **Fallback `/state`** when change count or `poll.revision - clientRev` exceeds a bound |

**No new API required** for the happy path. Optional hash field on existing poll response is the smallest integrity extension.

---

## Proposed boot sequence

```mermaid
%%{init: {'themeVariables': {'fontSize': '20px'}}}%%
sequenceDiagram
    participant Prog as Program.fs
    participant IDB as IndexedDB
    participant Sess as session/localStorage
    participant View as View.fs
    participant Srv as Server

    Prog->>Sess: tryReadSavedZoomId (scope key)
    Prog->>IDB: read snapshot + Change log
    alt cache hit valid
        IDB-->>Prog: F0 at R, Delta id greater than R
        Prog->>Prog: decode snapshot, fold Delta
        Prog->>Sess: restoreSessionState + mergePendingAfterLoad
        Prog->>View: first paint
        Prog->>Srv: GET /poll?rev=clientRev (immediate)
    else cache miss / invalid / fold error
        Prog->>Srv: GET /state(?zoom=)
        Srv-->>Prog: StateResponse
        Prog->>Prog: decodeStateResponse → StateLoaded
        Prog->>View: first paint
        Prog->>IDB: write snapshot, clear Change log
        Prog->>Srv: GET /poll?rev=... (immediate)
    end
    alt poll: rev match, empty changes
        Srv-->>Prog: ChangeSuccessResponse
        Prog->>Prog: confirm cache, update isReady
    else poll: rev ahead
        Srv-->>Prog: changes tail
        Prog->>Prog: skip ids already in log; apply rest; append
    else poll: CodeOutdated
        Prog->>Prog: stale banner (existing)
    else poll: apply fail OR rev regression
        Prog->>Srv: GET /state (fallback)
        Prog->>View: full re-render
        Prog->>IDB: replace snapshot, clear log
    end
    Prog->>Prog: startPolling interval (5s)
```

**Ordering principles:**

1. Capabilities fetches stay parallel and non-blocking ([[src/Client/Program.fs]]:42–61).
2. Cache path must run **fold of local Δ**, then **`restoreSessionState` and `mergePendingAfterLoad`**, before first render — same wrapper as [[src/Client/App.fs]] `StateLoaded`, with fold inserted before pending merge.
3. First paint must include this tab's accepted edits. Do not paint the snapshot alone and patch from poll.
4. Boot poll is **immediate** (do not wait for `startPolling` interval). It is catch-up for events **not** in the local log.
5. After successful `/state` boot, **write the snapshot** and **clear the Change log** so the next F5 hits. Do this while the page is alive. Do not use `pagehide` as the snapshot write trigger (IndexedDB commit of ~7.4 MB UTF-16 is async; iOS can freeze the page before commit). `saveSessionState` on `visibilitychange` / `pagehide` stays as today for the small session record.
6. After submit retire, **append** the accepted `Change` to the log. Do not rewrite the snapshot.

---

## Poll sync algorithm (boot-specific)

Pseudocode after cache read:

```
inputs: snapshot R, graph F0, log Delta, scopeKey
clientRev ← max(R, max id in Delta) or R if Delta empty
F ← fold applyChange F0 Delta
if fold Error: fallback GET /state(scopeKey)
dispatch StateLoaded({ graph: F, revision: clientRev, isReady: cachedIsReady })
restore session + pending
render

poll ← GET /poll?rev=clientRev

if poll.revision < clientRev:
  fallback GET /state(scopeKey)

outcome ← getPollOutcome(poll, clientRev, pageContext)

if outcome = CodeOutdated:
  set syncState CodeOutdated; stop (existing UX)

if outcome = None and poll.changes is empty:
  update isReady from poll; done

if outcome = DataOutdated:
  novel ← poll.changes not already in Delta by changeId or id
  if novel is empty: update isReady; done
  if novel count or rev gap exceeds bound: fallback GET /state
  result ← applyServerTail(novel, clientSyncState)
  if result is Error: fallback GET /state
  else: patch SiteMap + revision; append novel to log; done
```

Reuse [[src/Shared/SyncLogic.fs]] and existing `PollDone` logic where possible; boot may use a dedicated `BootPollDone` msg to allow full `/state` fallback without entering `DataOutdated` idle state first.

---

## Storage choice and write triggers

| Store | Fit |
| --- | --- |
| **IndexedDB** | **Required** for v1. Snapshot ~3.7M char JSON exceeds reliable localStorage headroom. Change appends are small. |
| localStorage | Keep for `gambol-session-v1` and `gambol-pending-v1` only |
| sessionStorage | Unchanged (session hints) |

**Write triggers:**

| Event | Action |
| --- | --- |
| After successful `/state` boot | Write snapshot; **clear** Change log |
| After submit **retire** (server assigned id) | **Append** that `Change` to the log |
| After boot poll applies novel tail | **Append** those Change |
| Idle truncation (log length or rev gap over bound) | Project live VM to bootstrap scope, write new snapshot, delete log prefix |
| `pagehide` / `visibilitychange` | **Session + pending only** — not the snapshot |

**Write content for snapshot:** persist **`bootstrapStateResponse`-equivalent Graph** — on `/state` boot, reuse the response string. On truncation, project live VM graph through `ResidentProjection.bootstrapGraph RootClosure savedZoom` so the snapshot never stores out-of-scope loaded Workspaces.

**Anticipated snapshot write cost** (after `/state`, no re-encode): structured clone of ~7.4 MB UTF-16 ~10–30 ms main thread; IndexedDB commit ~50–200 ms off thread. Measure with `[Gambol boot]` `perfNowMs()` in slice 1. Change append cost is negligible next to that.

---

## Invalidation rules

| Rule | Action |
| --- | --- |
| `codecVersion` ≠ current | Delete snapshot and log; `/state` |
| `file` ≠ current URL file | Miss |
| `scopeKey` ≠ current session widen | Miss |
| Decode / IDB read error / fold error | Miss |
| `poll.revision < clientRev` | Delete snapshot and log; `/state` |
| Tail apply error on boot | Delete snapshot and log; `/state` |
| Optional: cache age > N days | Miss (conservative; optional v2) |
| User clears site data | Implicit miss |

Do **not** invalidate on revision match alone — that is the hit case.

---

## Failure and fallback cases

| Failure | User-visible behavior | Recovery |
| --- | --- | --- |
| IDB unavailable (private mode, quota) | Slightly slower boot | Transparent `/state` path |
| Corrupt snapshot or log JSON | Same as miss | `/state` |
| Poll network error after cache paint | UI from snapshot+log; sync indicator | Retry poll; background `/state` if repeated failure |
| CodeOutdated | Stale banner | Hard refresh (existing) |
| Pending queue + server ahead | Pending replay + submit | Existing sync ([[src/Shared/SyncPlanner.fs]] `restorePending`) |
| Poll tail invalid on partial Graph | Brief flash of stale UI possible only for **other-actor** events | `/state` fallback + snapshot rewrite |

---

## Spec conflicts

[[.scratch/selective-client-loading/spec.md]] explicitly excludes client offline/startup caches (:121) and defines refresh as a **new residency session** (user story 11, :34): Workspaces loaded via Load in the prior session are **not** retained on F5.

| Cache policy | Spec alignment |
| --- | --- |
| **Bootstrap-scoped snapshot + Change log** (v1) | **Aligned** if snapshot writes project to ROOT + optional zoom Workspace — same residency as `/state`. Cached Change must not reinstall out-of-scope Workspaces. |
| **Full client Graph cache** | **Conflicts** — resurrects Load residency without Load; requires spec amendment |
| **IndexedDB boot cache** | **Conflicts** with Out of Scope :121 — needs spec update to move from "deferred/ out of scope" to "allowed for warm boot" |

Recommended spec edits if proceeding:

1. Amend user story 11: "refresh begins a new **Load** residency session; bootstrap-scoped residency may persist across reload for fast boot."
2. Remove or narrow Out of Scope item on IndexedDB/offline startup caches.
3. Document snapshot write projection rule (bootstrap scope only) and that the Change log is the same event type as poll.

---

## Implementation slices (smallest first)

| Slice | Scope | Verify |
| --- | --- | --- |
| **1. Snapshot write after `/state`** | IndexedDB module; write bootstrap `stateJson` after successful `/state`; no boot read; log clone vs `oncomplete` with `[Gambol boot]` | DevTools → Application → IndexedDB; size ~3.7M; scopeKey correct |
| **2. Change log append on retire** | After `retireSubmittedPrefix`, append accepted `Change`; no boot read yet | One record per accepted batch; pending queue still clears as today |
| **3. Boot read: fold then paint** | Program.fs: snapshot + Δ → fold → `StateLoaded` or `/state`; feature flag | Warm F5 first paint shows accepted edits; flag off = unchanged |
| **4. Immediate boot poll** | Poll after first paint; skip Change already in log | Network: no `/state` on warm F5; one `/poll`; no double-apply |
| **5. Novel tail + fallback matrix** | Other-actor `DataOutdated` → `applyServerTail` → append; rev regression, apply error, scope mismatch, oversized tail → `/state` + delete | Induced failures in tests |
| **6. Truncation** | Idle rewrite of snapshot when log length or rev gap exceeds bound; clear prefix | Long edit session; next F5 has short log |
| **7. Optional `bootstrapHash` on poll** | Server hash + client compare | Detect artificial equal-revision mismatch in test |

Slices 1–4 deliver the user-visible win with a current first paint. Slice 5 covers correctness. 6–7 are hardening.

---

## Relation to other tracks

| Track | Relationship |
| --- | --- |
| Server revision-keyed encode cache | Complementary — speeds `/state` fallback and cold miss; does not replace client cache |
| Bucket 3 defer fold restore | Orthogonal — improves post-paint work after Graph is available |
| Two-phase visible-closure fetch | **Not chosen** — smaller `/state`, still a round trip; open questions remain ([[.scratch/selective-client-loading/reports/two-phase-state-loading-exploration.md]]). Complementary later for cold miss only |
| [[.scratch/client-start-time/reports/reload-state-reuse-investigation.md]] | This doc is the concrete design for recommendation #4, revised to snapshot + events |

---

## Key file references

| Concern | Path |
| --- | --- |
| Boot fetch | [[src/Client/Program.fs]] |
| StateLoaded | [[src/Client/Update.fs]], [[src/Client/App.fs]] |
| Poll fetch + PollDone | [[src/Client/App.fs]], [[src/Client/Update.fs]] |
| Poll outcome + tail apply | [[src/Shared/SyncLogic.fs]] |
| Pending retire | [[src/Shared/SyncPlanner.fs]] |
| Server poll/state | [[src/Server/Api.fs]] |
| Wire codec | [[src/Shared/ApiResponseSerialization.fs]], [[src/Shared/Serialization.fs]] |
| Bootstrap scope | [[src/Shared/ResidentProjection.fs]] |
| Session hints | [[src/Client/SessionState.fs]] |
| Change / History apply | [[src/Shared/History.fs]] |
| Residency spec | [[.scratch/selective-client-loading/spec.md]] |
| Event model | [[.scratch/event-sourced-ops/overview.md]] |

## Status

Design revised (snapshot + Change log). No `src/` changes.
