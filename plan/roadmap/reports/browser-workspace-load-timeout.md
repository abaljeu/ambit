# Browser Workspace Load: 400 / 502 timeout

Diagnosis only. No product code change. Loop not run: needs a large DataDir Workspace on production (cPanel → Azure) or a replay of a real reconcile Change.

Related: [[changes-post-timeout.md]] (desktop `POST /ambit/changes` stubs), [[check-ignore-batch-history.md]], [[upload-dot-scratch-directory-stub.md]]. Not [[plan/client-start-time/project.md]] (F5 `/state` boot).

## What desktop Load already fixed (not this bug)

Desktop Load with a local mapping is `WorkspaceUploadAction.DesktopPush`: inventory, optional `POST /ambit/changes` stubs, then `POST /_desktop/workspace-push` (WebDAV + git).

Commits that targeted **that** path:

| Commit | What it fixed | Browser Workspace Load? |
| --- | --- | --- |
| `6ce817f` bypass php proxy for file uploads | Desktop/WebDAV around PHP | No — Browser API still uses `proxy.php` |
| `9992ec5` fix Upload performance | WebDAV `PROPFIND` git omit | No |
| `0bc6835` git process per uploaded file | Upload spawn | No |
| FileAgent `GitCheckIgnore.classify` | `POST /ambit/changes` disk-effect git | Browser Unloaded Load **skips** `/changes` |

[[tmp/load-performance-audit.md]] measured ~39s `workspace-push` (Depth-infinity PROPFIND). The Browser has no `/_desktop/*`.

## Browser path (Load on a named Workspace / Directory)

Planner: [[src/Shared/WorkspaceUpload.fs]] `plan` → `ReconcileServerDisk` when `canPush && hasLocalMapping` is false.

[[src/Client/UpdateWorkspaceLoad.fs]] `loadOp` → `Effect.ContinueDirectoryReconcile`.

[[src/Client/App.fs]] `ContinueDirectoryReconcile`: `postJson` **`POST /ambit/workspace/reconciliation/directory`** (no AbortController, no 60s submit watchdog). `postJson` does **not** retry 502 (unlike `ContinuePostUploadStructure` on `/changes`).

Empty `path` (Workspace focus) → [[src/Server/LazyLoadReconciliationServer.fs]] `reconcileWorkspace`:

1. `handle.getState`
2. `discoveredAddedPaths`: `DocumentPersistence.discoverArtifactRelatives` (`EnumerateFiles` AllDirectories) **plus** empty-dir `{rel}/.amb` walk (uncommitted empty-dir stub work)
3. Treat **every** discovered path as `Added`
4. `planChangedPathsWithArtifacts` (stubs + Directory File parse unless Current skip)
5. If `ops` non-empty: **`postGraphOnlyChange`** one Change
6. HTTP 200 `{failures}` or **400** `Results.BadRequest(err)`

Then [[src/Client/UpdateWorkspaceSync.fs]] `completeDirectoryReconcile` → `okDetailWithPoll` → **`POST /{file}/load`** (Fetch packages + Poll). `runLoadServer` treats any non-2xx as a silent `LoadDone` miss (no status in the UI).

## 400 vs 502 (both real)

### 400 — app `BadRequest`

Production Azure is `Persistence:Mode` `db` → [[src/Server/RouteRegistration.fs]] `AgentHandle.ofDb`.

[[src/Server/DbAgent.fs]] wraps **`applyBatch` in `FileAgent.runBounded` 8000 ms**. Graph-only still applies every op; it only skips live document persist. Overrun → `"change processing timed out"` → reconcile maps Error to **HTTP 400**. Same bound on changelog `persistBatch`.

File-mode `ofFileWithDbMirror`: FileAgent graph-only apply is **unbounded** (persist skip). DbAgent timeout is best-effort after FileAgent; the client still gets the file ack unless FileAgent itself errors (invalid ops, unchanged rejection, log write).

Other 400s: invalid JSON body, missing workspace, apply `Invalid`.

Not Kestrel body size (100 MiB). Reconcile body is a tiny `{workspace, path}`.

### 502 — cPanel curl, not Kestrel

[[proxy.php]]: non-git routes **`CURLOPT_TIMEOUT` 60s**. Curl error → **HTTP 502** `Proxy error: …`. Git smart HTTP is 600s (`6ce817f` class of bypass; git only).

Custom-domain Browser (`collaborative-systems.org/ambit`) always hits this. Desktop LocalProxy talks to Azure directly → 100s `HttpClient` default ([[plan/roadmap/reports/changes-post-timeout.md]]), so the same reconcile can finish in the App and 502 in the Browser.

Azure App Service can also 502 if the worker dies; the **60s** timing plus `Proxy error` text is PHP.

Discovery + plan run **before** `postGraphOnlyChange`. If that walk exceeds 60s, 502 with **no** 400. If plan is fast and DbAgent apply exceeds 8s, **400** at ~8s.

## Why it is slow

Same cost as [[changes-post-timeout.md]], different door: **one Change with hundreds/thousands of stub ops** (`NewSpecialNode` + `Replace` Children, n² JSON on the apply/log side). Browser never posts that on `/changes` when the Workspace is Unloaded; reconcile does it server-side from disk.

Repeat Load of **already Current** stubs is planned to no-op ([[tests/Shared.Tests/LazyLoadReconciliationTests.fs]] `rediscovered Current Directory Files skip parse`; 80 dirs `<100ms`). First Load (or Unparsed / missing stubs / new empty dirs) still posts the giant Change.

`/load` can also exceed 60s if the Fetch package is large (PHP 502; client hides the status).

## Ranked hypotheses (falsifiable)

1. **If** first Load of a large DataDir Workspace on Azure `db` **then** Network shows `POST …/reconciliation/directory` **400** at ~8s with body `change processing timed out`.
2. **If** the same request runs through `proxy.php` and wall time **>60s** (discovery or FileAgent apply) **then** **502** `Proxy error` and no app 400.
3. **If** reconcile 200s and Fetch is the stall **then** `/load` is the 502 and reconcile was 200.
4. **If** the Workspace is already Current on the server **then** reconcile `ops=[]` and slowness is disk walk or `/load` — not apply timeout.
5. **Ruled out as primary:** desktop `workspace-push` / per-path `check-ignore` / Kestrel 100 MiB.

## What changed

Nothing in src/. No tests run (no fix). Uncommitted [[src/Server/LazyLoadReconciliationServer.fs]] empty-dir discovery **adds** walk work on this exact POST; inspect before merging as a perf fix.

## Leftover risk

No HITL HAR. No replay of a 1119-op graph-only batch through `ofDb`. Raising PHP timeout or `ChangeProcessingTimeoutMs` would hide 502/400 without making first Load cheap.

## Recommended next slice (small → large)

1. HITL: status, elapsed, URL, body (`change processing timed out` vs `Proxy error`).
2. Shared/server: chunk `postGraphOnlyChange` so each apply stays under 8s **and** raise `proxy.php` timeout for `/workspace/reconciliation/` and `/load` (cPanel upload). Still slow; stops the hard fail.
3. Skip discover+plan for paths already Current **before** `postGraphOnlyChange` (repeat Load). Does not fix first stub ingest.
4. Bulk stub ingest (avoid per-op `Replace`) — not surgical.

## Board mutations (parent applies)

- `add` [[plan/roadmap/reports/browser-workspace-load-timeout.md]] — HITL confirm 8s 400 vs 60s 502; next slice chunk graph-only reconcile and/or PHP timeout for reconcile+`/load` (not desktop push).
- Do not `remove` [[tmp/load-performance-audit.md]] (desktop PROPFIND) or [[upload-dot-scratch-directory-stub.md]] (leading-dot stub HITL).
