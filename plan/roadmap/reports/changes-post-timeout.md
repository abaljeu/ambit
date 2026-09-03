# POST `/ambit/changes` timeout (351 KB)

Browser `POST /ambit/changes` to `127.0.0.1:14731`, `Content-Length: 351668`. DevTools body (screenshot) is the evidence. No timed server replay yet.

## Payload (DevTools)

The visible JSON is the **`ops` array of one Change** (indices **0–1118**, **1119 ops**), not a binary blob and not many Changes.

Create phase (example around 95–99), same parent `324ebaf0-7e43-4d7a-ac38-b375498b40dd`:

- `Replace` (95, 97, 99)
- `NewSpecialNode` `kind: "directory"` (96, 98) with new `nodeId`s

Collapsed hundreds of the same pair through about 1099.

Tail (1100–1105, still inside `[1100 … 1118]`): `SetDocumentState`, distinct `nodeId`s, **`oldState: "unparsed"`**.

That matches [[src/Shared/WorkspaceUploadStructure.fs]] `planStubOps` in order: create ops (`NewSpecialNode` + `Replace` via [[src/Shared/FileNodeOps.fs]] `appendOwnedOp`), then state ops. Tail `oldState: "unparsed"` is **directory promotion** `Unparsed` → `Current` (`markParentCurrent` / `promoteUnparsedDirsWithMembers`). File marks (`NoServerFile`) and first `Current` → `Unparsed` on new directories sit earlier in the same state block (not expanded in the shot).

Scale: about **1100 create-phase ops** → on the order of **500+ new Directory/File stubs** if each create is one `NewSpecialNode` plus one `Replace`. Under [[src/Shared/dotnet/WorkspaceSyncLimits.fs]] `maxStructurePaths` (1500). One POST, one `changeId`.

351 KB is **that many `Replace` Children lists**, not TCP bulk. Each `Replace` encodes full `oldChildren` and `newChildren` (`{"ref":"owner","id":"<guid>"}`). Sibling count *n* under one parent makes JSON about **n²**. Many directories with smaller *n* add the same way.

## What the request is

`ChangeBatch` with one Change. Server applies ops, persists, returns confirmation Changes (not the full Graph). [[doc/current/sync-mvp.md]], [[doc/api.md]].

Client: [[src/Client/UpdateWorkspaceSync.fs]] `completeUploadInventory` → `ContinuePostUploadStructure` in [[src/Client/App.fs]] (`encodePendingBatchBody` of that one Change). Not Load (`POST /ambit/load`). Not WebDAV (`POST /_desktop/workspace-push`).

`127.0.0.1:14731` is Desktop Local Proxy ([[src/Desktop/LocalProxy.fs]] Kestrel `Listen` loopback port 0). It copies the 351 KB body and `HttpClient.SendAsync` to cloud Ambit. Cookie `gambol_auth`. `X-Gambol-Client` from [[src/Client/UpdateHelpers.fs]].

## Server path for these 1119 ops

[[src/Server/RouteRegistration.fs]] `POST /ambit/changes` → [[src/Server/Api.fs]] `postChange` → FileAgent then optional Db mirror.

[[src/Server/FileAgent.fs]] mailbox `handlePostChange` (one message at a time):

1. Decode `{ changes: [{ id, changeId, ops }] }` — 351 KB, cheap.
2. `applyBatch` → `ChangeAmendment.applyChange` → `History.apply` **once per op, in order**. FileAgent: **no time bound**. DbAgent: `runBounded` **8 s** around the whole batch. Each op: inaccessible-document gate ([[src/Shared/History.fs]]); each `Replace` also `ownedSubtreeHasReservedArtifactPath` on new Owned children.
3. `validatePathMoves` then [[src/Server/IgnoredDestination.fs]] `validateGraphDiskEffects`. New File/Directory document-root paths go to one `GitCheckIgnore.classify` (`check-ignore --stdin`) per work tree. Empty path list: no git process. `.gitignore` destinations stay allowed. FileAgent thread, **no time bound**.
4. `persistGraphOps` ([[src/Server/DocumentPersistence.fs]]): live-save Directory Files / Files touched by the ops. Bound **8 s**. Overrun → HTTP **400** `"change processing timed out"`, not an open hang. Abandoned write may still run.
5. ChangeLog / DB append of the **same large Change**.

Mirror [[src/Server/Api.fs]] `ofFileWithDbMirror` waits on FileAgent first.

## Why it times out (not “351 KB is big”)

The bytes only record 1119 stub ops. Cost is **sequential apply + batched git classify + many Directory File writes**.

Upload structure POST has **no** 60 s watchdog. `postJson` has **no** AbortController. LocalProxy `HttpClient` default is **100 s**. Ordinary submit watchdog is 60 s ([[src/Shared/SyncRetry.fs]]) and does not apply to this Upload POST. Kestrel max body is 100 MiB; 351 KB is not the limit.

If DevTools shows timeout/fail at ~100 s with no HTTP status: proxy cancel while FileAgent still in step 2 or 3. If console `POST timeout 60000ms`: that is the **edit** submit path, not this Upload POST. If HTTP 400 with `change processing timed out`: step 4 (or DbAgent apply/persist 8 s).

## Upload / dotfiles

This **is** Upload stub structure after inventory. Load does not post this ops list.

Inventory [[src/Shared/dotnet/WorkspaceLocalInventory.fs]] skips **`.git` dirs only**. No general skip of `.` names. gitignore `classify` can drop ignored paths. `.amb` is a persist Directory File rule, not an inventory skip. Hundreds of non-ignored files (including `.env` / `.cursor` if not ignored) become this 1119-op Change.

## Hang ranking (updated)

1. Unbounded **apply of 1119 ops** (Replace + reserved-path checks) on FileAgent.
2. Proxy **100 s** while Azure/FileAgent still in apply; Browser fail at `127.0.0.1:14731`.
3. Persist of many Directory Files — **400 at 8 s** more than a hang.
4. Disk-effect ignore check is **one `classify` per work tree** (not ~500 processes). Still no FileAgent time bound on that step.
5. Client 60 s watchdog — **not** this Upload POST.

## Logging / loop

[[src/Server/HttpResponseLog.fs]] logs `/ambit/changes` after a response. A hang has no line until the request ends.

Shape is known (1119-op Upload stub Change). Batch ignore classify landed in [[src/Server/IgnoredDestination.fs]] (report: [[ignored-destination-batch-classify.md]]). Still missing: wall time per FileAgent step, or a replay of this `ops` list. Copy the full `ops` array from DevTools to pin apply vs persist.

## Next checks

- Network timing: ~60 s vs ~100 s vs 400 body `change processing timed out`.
- Count `NewSpecialNode` vs `Replace` vs `SetDocumentState` (console `ops.filter`).
- Whether Upload of the mapped workspace ran immediately before.
