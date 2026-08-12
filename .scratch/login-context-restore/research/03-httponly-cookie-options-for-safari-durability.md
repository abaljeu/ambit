# Findings: HttpOnly cookie options for Safari durability

Ticket: [[.scratch/login-context-restore/issues/03-httponly-cookie-options-for-safari-durability.md]]
Date: 2026-08-11
Branch: `w/login-context-restore`

## Purpose

Sidenote investigation only — **options and tradeoffs**, not a product decision (that is grilling 04).

## Current Gambol baseline

| Attribute | Value | Notes |
|-----------|-------|-------|
| Name | `gambol_auth` | [[src/Server/AuthToken.fs]] |
| HttpOnly | true | Good for credential cookies per WebKit guidance |
| SameSite | Strict | Strong CSRF posture; weakest fit for Safari tab recovery reports |
| Expires | ~10 years | Persistent, not session cookie |
| Secure | unset in `CookieOptions` | ASP.NET will not force Secure unless set / policy |
| Path | default (`/`) | Fine for site gate |
| Partitioned | no | N/A for first-party top-level |

## Option matrix (primary-source backed)

### 1. HttpOnly vs readable (`document.cookie`)

| Choice | Improves | Worsens / cost |
|--------|----------|----------------|
| **HttpOnly** (current) | Not stolen via XSS / speculative execution; **exempt** from ITP 2.1’s 7-day **client-side** cookie expiry cap ([ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/): auth cookies “should be Secure and HttpOnly”; client-side cookies cannot be HttpOnly) | Client cannot re-attach cookie from JS after recovery — only server Set-Cookie or alternative credential store |
| Readable cookie | Client could detect / rewrite after discard | ITP caps persistent client-side cookies to 7 days; WebKit explicitly warns credentials should not live in `document.cookie` |

### 2. Session cookie vs persistent (`Expires` / `Max-Age`)

| Choice | Improves | Worsens / cost |
|--------|----------|----------------|
| **Persistent long Expires** (current) | Intended to survive browser restarts and idle | Does not by itself fix SameSite recovery send bugs; still deleted if user clears website data |
| Session cookie (no Expires) | Cleared when “browser session” ends — sometimes desired for shared devices | **Worse** for harsh restart / reopen-last-tabs durability; session end is browser-defined |

### 3. SameSite: Strict vs Lax vs None

| Choice | Improves | Worsens / cost |
|--------|----------|----------------|
| **Strict** (current) | Cookie not sent on cross-site top-level lands; stronger CSRF | [Bug 200345](https://bugs.webkit.org/show_bug.cgi?id=200345): after Safari tab/window recovery, Strict cookies reported missing / not sent; Lax still visible. Recovered tabs hypothesized as failing Strict’s “same-site navigation” trust |
| **Lax** | More likely to be present after reopen-last-window class recoveries (same bug reports); still withheld on most cross-site subresources | Weaker than Strict for some CSRF cross-site GET navigations |
| **None** (+ Secure) | Cross-site embeds | Irrelevant / harmful for first-party gate; third-party cookies blocked by default in Safari ([Full third-party cookie blocking](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/)) |

### 4. Secure

| Choice | Improves | Worsens / cost |
|--------|----------|----------------|
| **Secure** | Required pairing for modern cross-site None; WebKit auth guidance pairs Secure + HttpOnly; avoids cleartext cookie on HTTP | Breaks cookie on pure `http://` origins (local/dev) unless HTTPS |

### 5. Path / Domain

| Choice | Notes |
|--------|-------|
| Path=`/` (default) | Correct for whole-app gate |
| Narrow Path | Can “lose” cookie on routes outside path — avoid for `/ambit` shell |
| Domain expansion | Increases attack surface; unused here |

### 6. Partitioned / CHIPS-style

ITP 2.1 **removed** partitioned cookies for classified third parties ([ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/)). First-party top-level auth should **not** rely on partitioned cookies. Not a durability lever for this destination.

### 7. Apple “stay logged in after idle” guidance

WebKit’s published developer guidance for auth durability is essentially:

1. Set auth cookies in **HTTP responses** as **Secure + HttpOnly** (not `document.cookie`) — [ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/).
2. Require **user interaction** with the first-party for long-lived storage expectations — [ITP intro](https://webkit.org/blog/7675/intelligent-tracking-prevention/).
3. Do not depend on third-party cookies; use first-party session after OAuth-style handoff — [Full third-party cookie blocking](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/).

There is **no** Apple doc found that promises SameSite=Strict cookies survive MobileSafari kill + tab restore. Bug 200345 remains the explicit counter-example.

Home Screen web apps are called out as **not** sharing Safari’s 7-day script-storage counter ([Full third-party cookie blocking](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/)) — out of scope unless destination redraws to PWA.

## Tradeoff summary for grilling (no recommendation)

- Keeping **HttpOnly + persistent** is aligned with WebKit’s stated auth advice and avoids the 7-day client-side cap.
- **SameSite=Strict** is the attribute most implicated by Safari **tab recovery** primary reports; **Lax** is the attribute change most often contrasted in those reports.
- Adding **Secure** matches WebKit wording but couples durability to HTTPS.
- Cookie attributes alone may still leave UI context broken if it stays on `sessionStorage` (research 02) — orthogonal to cookie knobs.

## Sources

- [WebKit ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/)
- [WebKit Full Third-Party Cookie Blocking and More](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/)
- [WebKit Intelligent Tracking Prevention](https://webkit.org/blog/7675/intelligent-tracking-prevention/)
- [WebKit bug 200345](https://bugs.webkit.org/show_bug.cgi?id=200345)
- In-repo cookie construction: [[src/Server/RouteRegistration.fs]]
