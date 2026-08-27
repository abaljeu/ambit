# Client start time — research summary

Date: 2026-08-27  
Full report: [[.scratch/client-start-time/research.md]]

## User problem (production desktop refresh)

1. **Phase A** — blank delay before any UI  
2. **Phase B** — static **"Loading..."** in the document pane for **≥3s** until the outline appears  

**Environment:** production (custom domain → cPanel proxy → Azure). Not localhost.

## Measured baseline (Network tab)

| Request | Duration | Notes |
| --- | --- | --- |
| `/ambit?bust=…` HTML | ~438 ms | Shell paint; via proxy |
| `Program.bundle.js` | **153 ms** | Direct from Azure; not the bottleneck |
| `capabilities` (1st / 2nd) | 75 ms / 472 ms | Non-blocking; [[src/Client/Program.fs]] |
| **`/ambit/state?zoom=…`** | **3.50 s** | **Primary target** — green = server TTFB via proxy ([[src/Client/Program.fs]]:69); body **~3.7M characters** JSON |
| `file-status` | 426 ms | After state; polling ([[src/Client/App.fs]]:580) |

Waterfall: **green** = waiting/TTFB (dominated by state); **blue** = client post-network work until final render.

## Would compression help?

**No** for the ≥3 s gap — **worthwhile secondary win after TTFB is fixed**.

- Response compression is **not enabled** ([[src/Server/Server.fs]] — no `UseResponseCompression`).
- Measured **3.50 s is TTFB** (green = server waiting). Server still does full-graph encode → decode → scope → re-encode ([[src/Server/Api.fs]]) before any byte is sent; compression runs **after** that work.
- On production, repetitive JSON would gzip well (~3.7 MB → ~300–500 KB) and could save **~0.2–1.5 s download after first byte** on typical links — but **not** the ~3.5 s green wait. cPanel proxy ([[doc/reference/cpanel-transparent-proxy.md]]) would pass compressed bodies through if Azure sends `Content-Encoding`.
- **Scope-before-encode** cuts server CPU and payload at the source; **compression-only** leaves TTFB ~unchanged.

## Where "Loading..." comes from

**[[src/Server/wwwroot/gambol.template.html]]** line 24 — literal text inside `#amb-document`, below the sticky header. It is removed only when [[src/Client/View.fs]] `render` runs after `SysMsg (StateLoaded _)` in [[src/Client/Program.fs]].

Not the header sync bar (`Loading…` Unicode ellipsis is a different path in [[src/Client/StatusView.fs]] for explicit Load commands).

## What causes the ≥3s gap (phase B)

1. **Bundle download + eval ~153 ms** — gate before fetches start, but **not** the ≥3 s pain  
2. **`GET /ambit/state` ~3.50 s TTFB** — server encodes full graph, decodes it, scopes to RootClosure, re-encodes ([[src/Server/Api.fs]]) — **clear optimization target**  
3. **Client post-network work (blue segment)** — JSON decode, `buildSiteMapFrom`, session fold restore, synchronous `View.render` after state returns  

## Top prioritized fixes

1. **Scope graph before server JSON encode** — avoid full-graph decode/re-encode on every refresh in [[src/Server/Api.fs]] (addresses measured 3.50 s state TTFB)  
2. **Enable response compression (optional)** — production bandwidth win **after** (1); not a substitute  
3. **Defer session fold expansion** — first render minimal tree, expand folds after first paint (addresses blue-segment client work)  
4. **Instrument boot (validation)** — marks around `/ambit/state` and `View.render` to confirm server fix and quantify remaining client cost  

## Localhost validation (2026-08-27, post scope-before-encode + gzip)

Report: [[.scratch/client-start-time/reports/localhost-timing-after-optimizations.md]]

**Caveat:** Localhost used a much smaller test database than production's full workspace graph. Numbers below validate **mechanism only** — not expected production improvement magnitude.

| Metric | Production baseline (full graph, pre-fix) | Localhost smoke test (small test DB, post-fix) |
| --- | --- | --- |
| `/ambit/state` | 3.50 s TTFB, ~3.7M characters decoded JSON (production, pre-fix) | **199 ms**, **400,000 characters** decoded JSON, **88.9 kB** transferred (compressed) — different DB; not comparable magnitude |

On localhost, state is no longer the waterfall bottleneck. Post-bootstrap **workspace-sync-ledger** XHRs (×7, 95–567 ms each) now dominate total span (~3 s). **Production HITL on the same production data** after deploy is the apples-to-apples retest.

## Remaining questions

- Production HITL after deploy: Content-Encoding, TTFB, transferred size on `/ambit/state`  
- ROOT workspace node count and saved fold count on refresh? (informs defer-render gain)  
- Phase A severity vs Loading placeholder? (preload/CSS vs client deferrals)  
- Ledger waterfall: defer/narrow path-sync after push? ([[tmp/load-performance-audit.md]])
