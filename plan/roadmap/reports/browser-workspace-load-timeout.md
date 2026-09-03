# Browser Workspace Load: 400 / 502 timeout

HITL confirmed **both** HTTP 400 (`change processing timed out`) and HTTP 502 (`Proxy error`) at different times (item 1). Item 2 is implemented: [[graph-only-reconcile-chunk.md]].

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
5. If `ops` non-empty: **`GraphOnlyChangePost.postChunks`** — split at `GraphOnlyChangeChunks.maxOps` (80); each chunk is one `postGraphOnlyChange` (new `changeId`, revision +1)
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

[[proxy.php]]: ordinary non-git routes **`CURLOPT_TIMEOUT` 60s**. Curl error → **HTTP 502** `Proxy error: …`. Git smart HTTP is 600s (`6ce817f` class). After item 2, `/workspace/reconciliation/` and `/load` also use 600s once the new [[proxy.php]] is on cPanel. Until that upload, custom-domain Browser still 502s at 60s.

Custom-domain Browser (`collaborative-systems.org/ambit`) always hits this. Desktop LocalProxy talks to Azure directly → 100s `HttpClient` default ([[plan/roadmap/reports/changes-post-timeout.md]]), so the same reconcile can finish in the App and 502 in the Browser.

Azure App Service can also 502 if the worker dies; the **60s** timing plus `Proxy error` text is PHP.

Discovery + plan run **before** the first `postGraphOnlyChange`. If that walk exceeds the PHP timeout, 502 with **no** 400. If plan is fast and one DbAgent apply exceeds 8s, **400** at ~8s (mitigated by 80-op chunks).

## Why it is slow

Same cost as [[changes-post-timeout.md]], different door: **many stub ops** (`NewSpecialNode` + `Replace` Children, n² JSON on the apply/log side). Browser never posts that on `/changes` when the Workspace is Unloaded; reconcile does it server-side from disk, now in 80-op Changes.

Repeat Load of **already Current** stubs is planned to no-op ([[tests/Shared.Tests/LazyLoadReconciliationTests.fs]] `rediscovered Current Directory Files skip parse`; 80 dirs `<100ms`). First Load (or Unparsed / missing stubs / new empty dirs) still plans a large op list, now posted in 80-op Changes.

`/load` can also exceed 60s if the Fetch package is large (PHP 502 until cPanel has the 600s `/load` timeout; client hides the status).

## Ranked hypotheses (falsifiable)

1. **Confirmed HITL:** first Load of a large DataDir Workspace on Azure `db` can show `POST …/reconciliation/directory` **400** with body `change processing timed out`.
2. **Confirmed HITL:** the same Browser path through `proxy.php` can show **502** `Proxy error` when wall time exceeds the PHP timeout (60s until cPanel upload).
3. **If** reconcile 200s and Fetch is the stall **then** `/load` is the 502 and reconcile was 200.
4. **If** the Workspace is already Current on the server **then** reconcile `ops=[]` and slowness is disk walk or `/load` — not apply timeout.
5. **Ruled out as primary:** desktop `workspace-push` / per-path `check-ignore` / Kestrel 100 MiB.

## What changed

Item 1 HITL: both 400 and 502. Item 2: [[graph-only-reconcile-chunk.md]] — 80-op graph-only chunks plus PHP 600s for reconcile and `/load`. Empty-dir discovery in [[src/Server/LazyLoadReconciliationServer.fs]] is still present and was not rewritten for this slice.

## Leftover risk

First Load is still slow. No production replay after this slice. cPanel must receive the new [[proxy.php]] or 502 at 60s remains. Chunking does not skip Current paths (item 3) or bulk stub ingest (item 4).

## Recommended next slice (small → large)

1. HITL confirm 400 vs 502 — **done** (both).
2. Chunk graph-only reconcile and raise PHP timeout for reconcile+`/load` — **done** in tree; **cPanel upload + production Load HITL** still open ([[graph-only-reconcile-chunk.md]]).
3. Skip discover+plan for paths already Current **before** `postGraphOnlyChange` (repeat Load). Does not fix first stub ingest.
4. Bulk stub ingest (avoid per-op `Replace`) — not surgical.

## Board mutations (parent applies)

- `remove` this report’s HITL confirm 8s 400 vs 60s 502.
- `add` [[graph-only-reconcile-chunk.md]] — HITL upload [[proxy.php]] to cPanel; large Workspace Load should not 400/502.
- Do not `remove` [[tmp/load-performance-audit.md]] (desktop PROPFIND) or [[upload-dot-scratch-directory-stub.md]] (leading-dot stub HITL).
