# Production HITL — after deploy (9942ce7)

Date: 2026-08-27  
Deploy: commit **9942ce7** (`w/relaxed-concurrency`), Azure deploy ~12:44 UTC  
Environment: **production** — custom domain via cPanel proxy → Azure (same workspace as pre-fix baseline)  
Source: user Network tab screenshot (parent chat), confirmed as deployed report  
Prior baseline: [[plan/client-start-time/reports/client-start-time-research.md]]  
Post-fix estimates: [[plan/client-start-time/reports/state-further-optimization.md]]

## Measured numbers (production, post-deploy)

User hard-refresh Network tab waterfall (Time column visible; Size and Response Headers not captured in screenshot).

| Request | Duration | Initiator | Notes |
| --- | --- | --- | --- |
| `GET /ambit?bust=…` (HTML) | **469 ms** | navigation | vs pre-fix ~438 ms |
| `style.css` / `user.css` | **0 ms** | cache | unchanged pattern |
| `Program.bundle.js` | **124 ms** | ambit:221 | vs pre-fix **153 ms** |
| `/_desktop/capabilities` (1st) | **76 ms** | [[src/Client/Program.fs]]:40 | **red/failed** (non-blocking) |
| `GET /ambit/capabilities` (2nd) | **474 ms** | [[src/Client/Program.fs]]:51 | vs pre-fix 472 ms |
| **`GET /ambit/state?zoom=d28e665d…`** | **1.19 s** | [[src/Client/Program.fs]]:69 | **primary measurement** — same zoom id as pre-fix baseline |
| `file-status` | **422 ms** | [[src/Client/App.fs]]:580 | vs pre-fix 426 ms |

**Not captured in screenshot (still open):**

- `Content-Encoding` response header (`br` / `gzip` expected per [[plan/client-start-time/reports/server-state-compression.md]])
- Network tab **Size** column (transferred vs decoded JSON)
- Explicit time-to-outline (perceptual UX); infer from waterfall end ~**1.9 s** vs pre-fix ≥**3 s** phase B

**Not observed in this capture:** `workspace-sync-ledger` × N XHRs seen on localhost ([[plan/client-start-time/reports/localhost-timing-after-optimizations.md]]). Likely because desktop capabilities fetch failed (red row), so ledger sync did not run on this refresh.

## Comparison vs baseline and estimates

| Metric | Pre-fix production | Post-deploy estimate (9942ce7) | Post-deploy measured |
| --- | --- | --- | --- |
| **`/ambit/state` duration** | **3.50 s** | **0.8–1.5 s** | **1.19 s** |
| Decoded JSON size | **~3.7M characters** | ~3.7M (similar scoped payload) | not captured |
| Transferred size | ~3.7M (uncompressed) | ~300–500 KB compressed | not captured |
| `Content-Encoding` | none | `br` or `gzip` | not captured |
| Bundle | 153 ms | unchanged | **124 ms** |
| Total boot waterfall (visible) | state hid downstream; ≥3 s to outline | state no longer sole multi-second pole | **~1.9 s** span to last visible request |

**State TTFB improvement:** 3.50 s → 1.19 s = **~66% reduction** (~2.31 s saved). Falls inside the **0.8–1.5 s** estimate band from [[plan/client-start-time/reports/state-further-optimization.md]] and matches the prior **~1.19 s** screenshot referenced there.

## Verdict

### State endpoint — **met expectations**

Scope-before-encode + gzip (9942ce7) delivered the predicted magnitude on production data. **1.19 s** is a material win over **3.50 s** and within the modeled **~50–70%** TTFB reduction. Treat `/ambit/state` server work as **largely done for this phase** unless follow-up header/size checks show compression misconfiguration.

Sub-300 ms on this workspace remains unrealistic without payload reduction or revision cache / on-demand residency ([[plan/client-start-time/reports/state-further-optimization.md]]).

### Bucket 3 — **now the relative bottleneck for perceived boot**

State is no longer the multi-second long pole. Remaining gap to interactive outline is main-thread post-state work ([[plan/client-start-time/reports/bucket-3-post-state-work.md]]):

1. **`decodeStateResponse`** on ~3.7M-char JSON (scales with production graph)
2. **`applyFoldSession`** + synchronous **`View.render`**
3. Possible ledger overlap (Bucket 4) when desktop capabilities succeed — not exercised in this capture

Waterfall ends ~**1.9 s** vs pre-fix **≥3 s** `"Loading..."` phase — user-visible improvement confirmed at network level; exact paint time still uninstrumented.

## Recommended next steps

1. **Optional quick follow-up:** on same production refresh, click `state?zoom=…` → Response Headers → confirm `Content-Encoding: br` or `gzip`; note Size column (transferred KB vs decoded MB). Closes the open items from [[plan/client-start-time/reports/localhost-timing-after-optimizations.md]] checklist.

2. **Move to Bucket 3 implementation** ([[WORK.md]] Pending):
   - Defer `applyFoldSession` until after first paint ([[src/Client/App.fs]], [[src/Client/SessionState.fs]])
   - Add boot `performance.mark` to quantify decode vs render vs ledger on production data

3. **Do not pursue further `/ambit/state` micro-optimization** unless optional header check fails or a warm F5 retest shows TTFB still **>1.5 s** (then consider revision-keyed bootstrap cache per [[plan/client-start-time/reports/state-further-optimization.md]]).

4. **Ledger deferral** ([[tmp/load-performance-audit.md]]) — secondary; validate on a refresh where desktop capabilities succeed and ledger XHRs appear.

## WORK.md note (for parent — do not edit here)

Pending item *"HITL production refresh after user deploy"* ([[plan/client-start-time/reports/localhost-timing-after-optimizations.md]]) **can be marked complete** for state TTFB magnitude validation: **1.19 s measured on same zoom/workspace**. Optional sub-note: Content-Encoding and transferred size still unverified in screenshot.

## Status

Production HITL recorded. State endpoint validated. Bucket 3 is the next optimization target.

## Second deploy — decode `resizeArray` + boot timing (2026-08-27)

User console after deploy (`Decode.resizeArray`, `perfNowMs` boot logs). Same workspace: **1,886,978 chars**, **6396 nodes**.

| Phase | Before (boot logs) | After |
| --- | --- | --- |
| `decodeStateResponse` | **900 ms** | **163 ms** |
| `restoreSessionState` | (broken timer) | **8 ms** |
| `View.render` | (broken timer) | **9 ms** (18 rows) |
| `StateLoaded dispatch total` | (broken timer) | **25 ms** |

**Client post-state total:** ~**197 ms** (163 + 25, decode overlaps dispatch tail slightly).

**Verdict:** [[plan/client-start-time/reports/decode-list-append-hotspot.md]] fix exceeded estimate (~200–350 ms → **163 ms**). Rank #1 decode bottleneck cleared. Session restore + first render are negligible at 18 visible rows — defer `applyFoldSession` is lower priority unless saved folds expand to hundreds of rows.

**Remaining boot pole:** `/ambit/state` network TTFB (~1.19 s from prior capture) + capabilities/file-status. `/_desktop/capabilities` **404** on web host (expected; ledger sync skipped).

See [[plan/client-start-time/reports/boot-timing-instrumentation.md]].
