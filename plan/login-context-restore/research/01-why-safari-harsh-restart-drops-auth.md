# Findings: Why Safari harsh restart drops auth

Ticket: [[plan/login-context-restore/issues/01-why-safari-harsh-restart-drops-auth.md]]
Date: 2026-08-11
Branch: `w/login-context-restore`

## Gambol cookie under test

Server sets `gambol_auth` via HTTP `Set-Cookie` with:

- `HttpOnly = true`
- `SameSite = Strict`
- `Expires` ≈ now + 10 years
- `Secure` not set in code ([[src/Server/RouteRegistration.fs]] `setAuthCookie`; name in [[src/Server/AuthToken.fs]])

Unauthenticated `GET /ambit` redirects to `/ambit/login` ([[src/Server/RouteRegistration.fs]] `serveAmbitApp`). So the login form means the request arrived **without** a matching cookie value — not a client-only “forgot I’m logged in” route.

## Harsh restart vs Refresh

| Event | What primary sources say | Auth cookie likely? |
|-------|--------------------------|---------------------|
| Normal Refresh / reload in a live tab | Same browsing context continues; persistent cookies still apply | Yes, if cookie still stored |
| Kill Safari / MobileSafari then reopen last tabs | WebKit bug reports cookies (esp. SameSite) missing or not sent after tab/window recovery | Often no / not sent |
| History → “Reopen Last Closed Window” | SameSite=Strict may be invisible / not sent on some request types; Lax still visible | Strict fragile |
| Idle tab process reclaim then cold reload | Not a single Apple “discard API” doc; behaves like a new document load in a recovered tab | Treat like recovery |
| Device sleep only (process stays) | No ITP purge solely from sleep | Cookie should remain |
| 7+ days Safari use **without** interaction on the site | ITP deletes **script-writable** storage; HttpOnly cookies set in HTTP responses are **not** the 7-day client-side cookie cap | Auth cookie should remain; UI storage may not |

Sources: [WebKit bug 200345](https://bugs.webkit.org/show_bug.cgi?id=200345); [ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/); [Full third-party cookie blocking + 7-day script storage](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/).

## Does ITP alone explain a missing `gambol_auth` after ~30 min?

**Unlikely as the primary cause for this cookie shape.**

- ITP 2.1 caps **client-side** (`document.cookie`) persistent cookies to 7 days. Auth cookies “should be Secure and HttpOnly” and “Cookies created through `document.cookie` cannot be HttpOnly which means authentication cookies should not be affected” ([ITP 2.1](https://webkit.org/blog/8613/intelligent-tracking-prevention-2-0/)).
- The later 7-day purge of IndexedDB / LocalStorage / SessionStorage / Service Worker cache is also gated on **seven days of Safari use without user interaction** on the site ([Full third-party cookie blocking](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/)) — far longer than the destination’s “30+ min idle” vibe, and it targets script-writable storage, not HttpOnly HTTP cookies.
- Full third-party cookie blocking affects **cross-site** cookies; a first-party `gambol_auth` on the Gambol origin is not that class of cookie ([same post](https://webkit.org/blog/10218/full-third-party-cookie-blocking-and-more/)).

## SameSite=Strict + tab recovery (strongest primary lead)

[WebKit bug 200345 — “SameSite cookies missing after Safari Tab recovery”](https://bugs.webkit.org/show_bug.cgi?id=200345) (status NEW as of last comment 2021-07-08; still open in Bugzilla):

1. **iOS kill MobileSafari → relaunch → reload restored tab:** cookie time becomes nil (not served).
2. **macOS reopen last tabs on app start:** same class of failure reported.
3. **Reopen Last Closed Window:** SameSite=**Strict** cookie missing from Web Inspector / not sent on WebSocket; SameSite=**Lax** still visible. Comment hypothesizes recovered tabs are treated like non–top-level / “tainted” navigations that fail Strict’s same-site checks. Typing the URL + Enter (true top-level navigation) “fixes” the tab.

Gambol’s `SameSite=Strict` therefore aligns with a documented Safari/WebKit recovery failure mode that a normal in-tab Refresh does not hit.

## Symptom: cookie gone vs cookie present but login anyway

Because `/ambit` is server-gated on the cookie:

| Observation | Meaning |
|-------------|---------|
| Redirect to `/ambit/login` | Request had no valid `gambol_auth` (missing, expired, wrong value, or not sent) |
| Cookie absent in Web Inspector after recovery | Storage / SameSite visibility failure (matches bug 200345 reports) |
| Cookie visible but not on a particular subresource | Partial send bug (bug 200345 notes XHR vs WebSocket inconsistency) |
| Login form after client-only navigation without hitting `/ambit` | Out of scope for current server gate — not how shell load works |

Client `sessionStorage` UI restore ([[src/Client/SessionState.fs]]) does **not** gate auth; if the user sees the login HTML, that is the server redirect path.

## Open empirical gap (HITL)

Primary sources explain **plausible** mechanisms; they do not prove which one fires on today’s Safari for Gambol’s exact host (HTTP vs HTTPS, `Secure` unset, Strict). HITL should distinguish:

1. After harsh restart, is `gambol_auth` absent in Web Inspector Storage → Cookies?
2. If present, does the document navigation to `/ambit` send it (Network request Cookie header)?
3. Does changing only SameSite to Lax (test-only) change recovery behavior?

## Bottom line

For a long-lived **HttpOnly + SameSite=Strict** first-party cookie, ITP’s 7-day caps are a weak fit for short idle. The best primary-source match for “Refresh keeps me in; Safari restart of an inactive tab forces login” is **Safari tab/window recovery failing to apply or send SameSite=Strict cookies** ([bug 200345](https://bugs.webkit.org/show_bug.cgi?id=200345)), compounded possibly by missing `Secure` on non-HTTPS deployments and by separate `sessionStorage` loss for UI context (see research 02).
