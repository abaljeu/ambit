# Desktop app 401 after ticket 33 Spec-gap

## 1. Verdict

**Link to Spec finding 2.1.1 ([[plan/core-creation/reports/code-review-33-spec-gap-tip.md|code-review-33-spec-gap-tip]]): refuted as the Desktop 401 root cause.**

Finding 2.1.1 is real as a Spec wording gap (client “reseed / re-establish” only writes `window.__BUILD_TS__`). It is **not** why the Desktop app returned 401. Desktop never used DeployEpochSec / CodeOutdated to rebuild credentials. The 401 is from **proxied Browser APIs reaching the server without a usable `gambol_auth` Cookie header** after Spec-gap removed the closed-over `browserCredential` fallback.

## 2. Answers to the three questions

### 2.1 After server restart, does Desktop still send `gambol_auth`?

When [[src/Desktop/AuthStore.fs|AuthStore]] has user/pass, [[src/Desktop/LocalProxy.fs|LocalProxy]] rebuilds `gambol_auth=deriveToken(user,pass)` on **every** forwarded request. That value is the same as CoreRuntime boot seed (`AuthToken.deriveToken` from Auth config). Restart reseeds the same token; **no silent re-login and no DeployEpochSec cookie re-issue are required** for admit to succeed, if the Cookie header is actually sent.

When AuthStore is empty (typical local Development: Auth disabled, never logged in), the old LocalProxy sent **no Cookie at all**.

### 2.2 Does Desktop rebuild/send cookie on DeployEpochSec / CodeOutdated / restart signal?

**No.** Shared Fable client `reseedDeployEpochOnServerSignal` only updates `__BUILD_TS__` (+ log). Desktop AuthStore / LocalProxy do not subscribe to that signal. Cookie presentation is LocalProxy’s job on each HTTP forward.

### 2.3 Omit cookie entirely, or fail to re-establish after restart?

**Omit / fail to attach Cookie on the outbound proxy request** — not a post-restart credential identity mismatch.

Contributing Desktop mechanics:

1. Spec-gap: missing cookie → `withBrowserChanges` → HTTP 401 (no closed-over fallback).
2. LocalProxy stripped WebView `Cookie` / `Set-Cookie` and only injected AuthStore when present.
3. Local Development Auth is empty ([[src/Server/appsettings.Development.json|appsettings.Development.json]]); AuthStore often empty → no inject.
4. Auth-disabled page `SetCookie` uses `Secure=true`; Desktop targets `http://localhost` and previously relied on HttpClient `UseCookies` / WebView jar — Secure cookies do not reliably attach on HTTP. Spec-gap browser fix SetCookies on page; Desktop never forwarded that Set-Cookie into subsequent API Cookie headers when AuthStore was empty.

After restart with AuthStore populated: deriveToken still matches boot seed. Restart alone does not 401 if Cookie is sent.

## 3. Fix

Keep Spec: missing cookie still refuses; no closed-over server secret fallback.

1. [[src/Server/AuthToken.fs|AuthToken]] — `proxyCookieHeader` / Set-Cookie parse helpers: server-issued value wins; else AuthStore user/pass; else auth-disabled `deriveToken("","")` so Desktop always presents a request-carried cookie.
2. [[src/Desktop/LocalProxy.fs|LocalProxy]] — `UseCookies = false` (manual Cookie header); always attach `proxyCookieHeader`; capture server `Set-Cookie` `gambol_auth` into `issuedCookie` for the target server (wins over stale AuthStore when present); clear on logout.

## 4. Test proof

```
dotnet build src/Desktop -c Debug
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~AuthTokenTests|FullyQualifiedName~BrowserCredentialTests"
```

Result: Passed 18, Failed 0.

Covers: auth-disabled token when nothing stored; server-issued preferred over stored; stored matches boot-seed deriveToken (restart identity); Set-Cookie capture/clear; auth-disabled APIs refuse without cookie / accept with cookie; auth-disabled page Set-Cookie matches empty deriveToken. Mailbox foreign-secret refuse stays in CredentialedChangePostsTests.

## 5. Manual verify

1. `scripts/desktop.sh run-local` (or desktop: Run (local)) against Development Auth-disabled.
2. App load must reach `/ambit/state` without 401 (proxy sends auth-disabled or captured cookie).
3. Cloud / logged-in Desktop: AuthStore still supplies deriveToken; after server restart, same cookie still admits without DeployEpochSec cookie work.
