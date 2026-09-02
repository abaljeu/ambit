# Graph-only reconcile chunks and PHP Load timeout

Item 2 from [[browser-workspace-load-timeout.md]]. HITL already confirmed both HTTP 400 (`change processing timed out`) and HTTP 502 (`Proxy error`) at different times.

## HTTP contract

The Browser still sends **one** `POST /ambit/workspace/reconciliation/directory` (and the same for `added`). The server splits planner ops and calls `postGraphOnlyChange` once per chunk. Each chunk is its own Change (`changeId`, `id` = revision then +1). The client does not continue-reconcile. A later chunk Error still maps to HTTP 400; earlier chunks may already be in the graph.

Then the client still does `POST /{file}/load` (`/ambit/load` on the wire).

## Chunk bound

[[src/Shared/GraphOnlyChangeChunks.fs]] `maxOps` **80**. DbAgent wraps each `applyBatch` in 8000 ms. A 1119-op graph-only Change timed out; History apply of an 80-op stub-create batch is asserted under 2000 ms in [[tests/Shared.Tests/GraphOnlyChangeChunksTests.fs]]. Separate posts so each mailbox message gets its own 8 s bound (one ChangeBatch with many Changes would still share one timeout).

## PHP timeout

[[proxy.php]] keeps 60 s for ordinary API. Git smart HTTP stays 600 s. Paths that contain `/workspace/reconciliation/` and paths that end in `/load` also use 600 s. Other routes stay at 60 s. cPanel still serves the uploaded file; Azure code change does not update HostGator by itself.

## Files

- [[src/Shared/GraphOnlyChangeChunks.fs]]
- [[src/Server/GraphOnlyChangePost.fs]]
- [[src/Server/LazyLoadReconciliationServer.fs]] loops `GraphOnlyChangeChunks.split`
- [[proxy.php]], [[doc/reference/cpanel-transparent-proxy.md]]

Empty-dir discovery in the reconciler is unchanged.

## Tests

- `dotnet test tests/Shared.Tests -c Debug --filter FullyQualifiedName~GraphOnlyChangeChunksTests`
- `dotnet test tests/Server.Tests -c Debug --filter FullyQualifiedName~GraphOnlyChangePostTests`
- Client compile gate after Shared edit

## Leftover

First Load is still slow. Repeat-Load skip of Current paths and bulk stub ingest are items 3–4. Production: upload [[proxy.php]] to cPanel, then HITL a large DataDir Workspace Load (expect 200, not 400/502).

## Board mutations (parent applies)

- `remove` [[plan/roadmap/reports/browser-workspace-load-timeout.md]] — HITL confirm 8s 400 vs 60s 502 (confirmed both).
- `add` [[plan/roadmap/reports/graph-only-reconcile-chunk.md]] — HITL: upload [[proxy.php]] to cPanel; first Load of a large DataDir Workspace should be HTTP 200, not 400/502 (still may be slow).
- Do not `remove` [[tmp/load-performance-audit.md]] or [[upload-dot-scratch-directory-stub.md]].
