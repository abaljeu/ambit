# Browser and App auth

Updated: 2026-09-14

This page records how the Browser and the App present authorization to the Server and to Core. The agreed design is [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]] and the App facts in [[plan/core-creation/reports/desktop-app-401.md]]. This page is the durable home. That ticket holds acceptance for Story path **Browser Change posts**.

## 1. Purpose

1. **Runtime description** — State how authorization runs for Browser HTTP, App proxy, and Core admit.
2. **Not a feature baseline** — Feature behavior stays in [[doc/current/]] and [[doc/arch.md]]. This Project describes coding and runtime.

## 2. Credential identity

1. **Cookie is the Core secret** — The cookie `gambol_auth` is the Browser Core credential. [[src/Server/wwwroot/login.html|Login.html]] and GET `/ambit` development auto-issue set that cookie. CoreCredentials holds that same value.
2. **Derived token** — The Server computes the value with `AuthToken.deriveToken` from Auth config username and password ([[src/Server/AuthToken.fs]]). Login and boot seed use the same function.
3. **Request-carried** — Every Browser API request carries `gambol_auth`. The Server reads the cookie from that request. The Server does not close over a boot secret and attach it when the cookie is missing.
4. **Missing cookie refuses** — A missing or blank cookie is refused. The Adapter returns HTTP 401 and does not call CoreMailbox.
5. **Cookie not in set refuses** — A present cookie that is not in the CoreCredentials allowed set is the same auth refuse (HTTP 401). The Adapter may `contains` at the door. That check is before CoreMsg enqueue. It is not persist logic. PersistHandlers stays after CoreMsg admit.
6. **Mailbox second line** — CoreMsg still admits Authority and secret for a bad or inactive cookie that reaches the mailbox. PersistHandlers is not called for that refuse.

## 3. Browser

1. **Login issues the cookie** — `POST /ambit/login` checks username and password. On success the Server sets `gambol_auth` to the derived token and adds that token to CoreCredentials. Empty Auth matches empty username and password. The Browser then loads `/ambit`.
2. **Development credential** — When Auth username and password are empty ([[src/Server/appsettings.Development.json]]), the token is still `deriveToken("", "")`. Boot seeds CoreCredentials with that value. `IsAuthenticated` requires the cookie. GET `/ambit` auto-issues that cookie so a Development Browser does not type a password. Later Browser APIs must send the cookie. There is no `auth.Disabled` skip. Git PAT `GitAuthDisabled` is a separate empty-Auth git-gateway path.
3. **Production login** — When Auth is set ([[src/Server/appsettings.json]]), `/ambit` redirects to [[src/Server/wwwroot/login.html|Login.html]] until the cookie equals the derived token.
4. **Browser APIs carry the cookie** — State, Poll, Load, and Change posts use fetch with `credentials: 'same-origin'`. The Browser sends `gambol_auth` as a cookie. Browser JavaScript does not rebuild the token.
5. **HTTP Adapter encodes the request** — [[src/Server/BrowserRequestCreds.fs]] reads the cookie. [[src/Server/RouteRegistration.fs]] `withBrowserChanges` returns HTTP 401 for a missing cookie and for a cookie that is not in the CoreCredentials set. It does not call CoreMailbox in those cases. A live cookie is passed into CoreRuntime `browserChanges`. Authority is `Browser`.
6. **Restart signal is not re-issue** — Poll and Load include `DeployEpochSec`. The HTML shell writes `window.__BUILD_TS__`. The Browser helper `reseedDeployEpochOnServerSignal` updates `__BUILD_TS__` when the Server process start time changes. That signal does not issue a new cookie and does not log the person in again.

## 4. App

1. **LocalProxy presents Cookie** — The App (Desktop project, [[src/Desktop/LocalProxy.fs]]) forwards `/ambit/*` to the Server. It always attaches a `Cookie` header. `HttpClient` uses `UseCookies = false` so the header is set in code.
2. **Cookie source order** — `AuthToken.proxyCookieHeader` picks one value: the captured server `Set-Cookie` `gambol_auth` when present; else `deriveToken` from [[src/Desktop/AuthStore.fs]] username and password; else `deriveToken("", "")` for the development credential.
3. **Secure cookie on HTTP localhost** — The Server sets the cookie with `Secure = true`. The App loads `http://localhost`. A Secure cookie does not attach reliably on HTTP. The App writes the Cookie header on each forward and does not rely on the WebView cookie jar.
4. **AuthStore vs empty Development Auth** — AuthStore holds user and password after a successful login. Typical local Development has empty Auth and an empty AuthStore. The App still sends the development derived token so Browser APIs are not refused.
5. **Capture and logout** — The App stores the Server `Set-Cookie` `gambol_auth` in `issuedCookie` for that Server. Logout clears AuthStore, the session, and `issuedCookie`.
6. **No DeployEpochSec cookie work** — The App does not rebuild credentials from the restart signal. LocalProxy attaches the Cookie on every forward.

## 5. Core

1. **Boot seed** — CoreRuntime adds `AuthToken.deriveToken` from Auth config user and password to CoreCredentials at Server start ([[src/Server/Core/CoreRuntime.fs]]).
2. **Mailbox validates before action** — CoreMailbox `postChange` takes the Change list plus Authority and secret. CoreMsg admits the secret against CoreCredentials before PersistHandlers ([[src/Server/Core/CoreMailboxBackend.fs]]).
3. **File and Db do not handle credentials** — After admit, PersistHandlers, FileAgent, and DbAgent see Changes only.
4. **Set membership** — CoreCredentials is an in-memory set. Boot seed and login `add` the Browser token. The set is not persisted. A restart creates a new set and seeds the same derived token.
5. **Other secrets** — CoreCredentials also holds non-Browser secrets (Parse, later Actors). Those secrets are not `gambol_auth`.

## 6. Restart

1. **Same token still admits** — After Server restart, boot seed recomputes the same derived token from Auth config. If the request still sends that Cookie value, Core admits it.
2. **No silent web re-login** — The Browser does not need an invisible stored-password login after restart. The existing cookie is enough when the Browser or the App sends it.
3. **Omit cookie fails** — Restart does not weaken request-carried admit. A request without `gambol_auth` is refused.

## 7. Related

1. **Core creation Story path** — Story path **Browser Change posts** and shared segment **Credentialed `PostChange` through CoreMsg** on [[plan/core-creation/arch.md|Core creation architecture]].
2. **Ticket** — Acceptance for the Browser path: [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]].
3. **App 401 report** — [[plan/core-creation/reports/desktop-app-401.md]].
4. **Map** — [[map.md]].

## 8. Out of scope

1. **Git PAT** — Basic username plus `deriveGitToken` for smart HTTP. Not the Browser cookie. Empty Auth still reports `GitAuthDisabled` and leaves the git gateway open.
2. **Actor live-table admit** — Actor secrets on the CoreActorPool live table. Not this page.
3. **Wiki home** — Whether this page later moves under [[doc/arch.md]] or another tree is [[issues/01-choose-wiki-home.md|Choose the architecture wiki home]].
