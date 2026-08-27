# Localhost timing — after scope-before-encode + gzip

Date: 2026-08-27  
Branch: `w/relaxed-concurrency`  
Environment: **localhost** (dev server, not production)  
Prior baseline: [[.scratch/client-start-time/reports/client-start-time-research.md]]  
Implementation: [[.scratch/client-start-time/reports/scope-before-encode.md]], [[.scratch/client-start-time/reports/server-state-compression.md]]

## Important caveat — not apples to apples

Localhost validates the **mechanism** (scope-before-encode + gzip on the server path). It does **not** validate the **magnitude** of production improvement. Production baseline used the **full workspace graph** (~3.7M characters decoded JSON); localhost used a **much smaller test database** (**400,000 characters** decoded JSON — user-provided). These sizes are **not comparable** (different DBs); do not infer production speedup from the ratio. **Production HITL after deploy on the same production data** is the real apples-to-apples test.

## Headline — localhost smoke test vs production baseline (different dataset)

| Metric | Production baseline (pre-fix, full graph) | Localhost smoke test (post-fix, small test DB) |
| --- | --- | --- |
| `/ambit/state` time | **3.50 s** TTFB | **199 ms** |
| `/ambit/state` size | **~3.7M characters** decoded JSON (full production graph, pre-fix) | **400,000 characters** decoded JSON; **88.9 kB** transferred (compressed, Network tab Size column) |

Do not extrapolate localhost numbers to production. After deploy, retest on production with the same workspace to measure real TTFB and transferred size.

## Production vs localhost — state request (different datasets)

| | Production baseline (full graph) | Localhost smoke test (small test DB) |
| --- | --- | --- |
| Endpoint | `GET /ambit/state?zoom=…` | same |
| Duration | 3.50 s (green = server TTFB) | 199 ms |
| Payload | ~3.7M characters decoded JSON (production, pre-fix) | 400,000 characters decoded JSON (user-provided); 88.9 kB transferred (Network tab Size column — compressed) |
| Initiator | [[src/Client/Program.fs]]:69 | same |
| Bottleneck role | Primary — dominated phase B "Loading..." | No longer the localhost waterfall bottleneck |

The production baseline measured waiting/TTFB before any byte arrived, with a full-graph encode/decode/re-encode pipeline on production data. Localhost confirms the optimized path (scoped single-encode + gzip/Brotli) works on a small test database — mechanism only; magnitude on production data is unknown until HITL.

## Full localhost waterfall (post-fix)

| Request | Size | Time | Notes |
| --- | --- | --- | --- |
| ambit (HTML) | 3.8 kB | 85 ms | document |
| style.css | 0 B | 0 ms | memory cache |
| user.css | 0 B | 0 ms | memory cache |
| Program.bundle.js | 150 kB | 116 ms | script |
| capabilities (1st) | 265 B | 26 ms | fetch, [[src/Client/Program.fs]]:40 |
| capabilities (2nd) | 232 B | 76 ms | fetch, [[src/Client/Program.fs]]:51 |
| **state?zoom=…** | **88.9 kB** | **199 ms** | fetch, [[src/Client/Program.fs]]:69 — **PRIMARY** |
| workspace-mappings | 534 B | 29 ms | xhr, [[src/Client/App.fs]]:509 |
| workspace-sync-ledger (×7) | 2.6–176 kB | 95–567 ms | xhr, [[src/Client/App.fs]]:533 |
| file-status | 270 B | 64 ms | fetch, [[src/Client/App.fs]]:580 |

Total waterfall span ~3 s, but **`/ambit/state` is no longer the long pole**. The outline should appear soon after state returns; remaining time is post-bootstrap client work.

## Observations

### Scope-before-encode + gzip on state

- On the small test DB, state returned in **199 ms** with **400,000 characters** decoded JSON and **88.9 kB** transferred (compressed) — consistent with scope-before-encode + gzip working; not comparable to production's ~3.5 s / ~3.7M characters baseline (different DBs).
- Bundle cost unchanged (~116 ms vs production ~153 ms) — expected; not the optimization target.

### New visible bottleneck: workspace-sync-ledger waterfall

After state loads, **seven** `workspace-sync-ledger` XHRs run sequentially or in overlapping bursts (95–567 ms each, 2.6–176 kB). This is new relative to the pre-fix profile where state TTFB hid everything downstream.

Possible follow-up (not blocking deploy validation):

- [[tmp/load-performance-audit.md]] — defer/narrow path-sync ledger waterfall after push ([[src/Client/App.fs]] `runWorkspacePathSyncSnapshot`, [[src/Shared/dotnet/WorkspaceSyncLedger.fs]] `liveStatusRows`)

### Localhost ≠ production

- **Different dataset** — localhost test DB is **400,000 characters** decoded JSON vs production's **~3.7M characters** full workspace graph (pre-fix); timing and size ratios here are not predictive.
- No cPanel proxy hop; loopback RTT is negligible.
- Same branch/code path, but production graph size, Azure cold/warm, and proxy `Content-Encoding` passthrough must be confirmed in the field.
- **Commit and deploy remain user-owned** — production HITL on the same data is the apples-to-apples test; this report does not substitute for it.

## What to check on production retest

After deploy, hard-reload `/ambit` in DevTools → Network and inspect **`/ambit/state`**:

1. **Content-Encoding** — response headers should show `br` or `gzip` (see [[.scratch/client-start-time/reports/server-state-compression.md]]).
2. **TTFB / Waiting** — green segment should drop from ~3.5 s toward sub-second (exact value depends on graph size and Azure CPU).
3. **Transferred size** — Size column should show hundreds of KB or less transferred vs ~3.7M decoded/resource size (DevTools shows both when compressed).
4. **Boot UX** — phase B "Loading..." in [[src/Server/wwwroot/gambol.template.html]] should clear much sooner; if total span is still ~3 s, profile ledger XHRs next.

## Status

- **Localhost:** measured — mechanism validated (scope-before-encode + gzip) on small test DB; magnitude not validated.
- **Production:** pending user deploy + HITL on same production data (see [[WORK.md]] Pending).
