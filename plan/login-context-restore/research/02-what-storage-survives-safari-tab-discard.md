# Findings: What storage survives Safari tab discard

Ticket: [[.scratch/login-context-restore/issues/02-what-storage-survives-safari-tab-discard.md]]
Date: 2026-08-11
Branch: `w/login-context-restore`

## Scope

Regular Safari tab (not Home Screen / PWA). Map storage durability across:

- Normal Refresh
- Harsh restart / tab recovery (Safari kill, reopen last tabs, process reclaim that cold-loads the tab)

## Storage matrix

| Store | Gambol use today | Normal Refresh | Harsh tab recovery / process reclaim | ITP 7-day no-interaction purge |
|-------|------------------|----------------|--------------------------------------|--------------------------------|
| HttpOnly cookie (`gambol_auth`) | Auth ([[src/Server/RouteRegistration.fs]]) | Survives | **Should** persist on disk as website data, but SameSite=Strict often **not sent / not visible** after recovery ([bug 200345](https://bugs.webkit.org/show_bug.cgi?id=200345)) | **Not** capped by client-side 7-day cookie rule if set via HTTP + HttpOnly ([ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/)) |
| Non-HttpOnly cookie | None for auth | Survives if persistent | Same recovery caveats; **is** subject to 7-day client-side expiry cap | Capped / purged with script-writable policy |
| `sessionStorage` | Zoom / folds / bootstrap widen ([[src/Client/SessionState.fs]] `gambol-session-v1`) | Survives (same tab session) | **Cleared** when the tab’s browsing session is destroyed / tab closed; recovered tabs behave like a **new** session for sessionStorage ([WebKit Private Browsing 2.0](https://webkit.org/blog/15697/private-browsing-2-0/): session storage scoped to current tab; destroyed when tab closed; [WHATWG Web Storage](https://html.spec.whatwg.org/multipage/webstorage.html#the-sessionstorage-attribute)) | Listed among script-writable forms deleted after 7 days without interaction ([Full third-party cookie blocking](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/)) |
| `localStorage` | Not used for this UI context | Survives | Survives process kill (origin-persistent) unless purged | Same 7-day script-writable purge |
| IndexedDB | Not used for auth/UI here | Survives | Survives unless purged | Same 7-day purge |
| Cache Storage / Service Worker | Not part of auth path | — | — | Registrations + cache in the 7-day purge list |

## Refresh vs harsh restart (plain language)

- **Refresh:** Same tab, same `sessionStorage` area, same cookie jar visibility. Matches Gambol’s intentional path: save on visibility hide, restore after `StateLoaded` ([[src/Client/SessionState.fs]] comments already name “iOS Safari tab eviction” as a motivation — but the chosen medium is still `sessionStorage`).
- **Harsh restart that recreates the tab/session:** Closest to “brand new tab” for **`sessionStorage`** (empty). **Persistent cookies / localStorage / IndexedDB** remain as website data **unless** ITP purge or SameSite recovery bugs intervene. Auth failure after harsh restart is therefore **not** explained by sessionStorage loss; UI-context loss **is**.

## Page Cache vs discard

WebKit Page Cache “pauses” a page in memory for back/forward ([Page Cache I](https://webkit.org/blog/427/webkit-page-cache-i-the-basics/)). That is **not** the same as killing Safari or reclaiming a tab into a cold load. Destination symptom (full reload + login) matches **cold load after recovery**, not bfcache restore.

## Mapping to destination

After the destination’s harsh restart:

| Need | Today’s store | Likely present? |
|------|---------------|-----------------|
| Stay authenticated | HttpOnly `gambol_auth` (Strict) | Often **fails to apply** under tab recovery despite long Expiry |
| Restore zoom/folds like Refresh | `sessionStorage` | **Empty** (new tab session) |

So auth and UI-context are **different failure modes** on the same event: cookie/SameSite recovery vs sessionStorage lifetime.

## Sources

- [WebKit — Full Third-Party Cookie Blocking and More](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/) (7-day script-writable list includes SessionStorage, LocalStorage, IndexedDB)
- [WebKit — ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/) (HttpOnly auth cookies vs client-side cap)
- [WebKit — Private Browsing 2.0](https://webkit.org/blog/15697/private-browsing-2-0/) (session storage tab-scoped; destroyed when tab closed)
- [WHATWG — `sessionStorage`](https://html.spec.whatwg.org/multipage/webstorage.html#the-sessionstorage-attribute)
- [WebKit bug 200345](https://bugs.webkit.org/show_bug.cgi?id=200345)
- In-repo: [[src/Client/SessionState.fs]], [[src/Server/RouteRegistration.fs]]
