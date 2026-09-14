# Remove auth.Disabled bypass

**Ticket:** [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]]
**Status:** coded
**Runtime page:** [[plan/architecture/browser-and-app-auth.md|Browser and App auth]]

## 1. Verdict

1. **No Disabled skip** — `auth.Disabled` is gone from [[src/Server/RouteRegistration.fs]]. Empty Auth does not make `IsAuthenticated` true without a cookie.
2. **Development credential is real** — Empty Auth still uses `AuthToken.deriveToken("", "")`. Boot seeds CoreCredentials with that value. GET `/ambit` auto-issues that cookie. Later Browser APIs must send it. Core admits the boot-seeded secret.
3. **Git PAT unchanged** — `GitAuthDisabled` still reports when Auth is empty. Git smart HTTP stays open without a PAT. That path is not the Browser cookie bypass.

## 2. Behavior

1. **HTTP cookie check** — `IsAuthenticated` reads `gambol_auth` and compares it to `deriveToken` of Auth config. Empty user and password do not skip the check.
2. **GET `/ambit` auto-issue** — When Auth is empty and the request has no matching cookie, the Server sets `gambol_auth` to the development token and serves the HTML. That is automatic login, not a skip. When Auth is set, a missing cookie redirects to [[src/Server/wwwroot/login.html|Login.html]] and does not mint a cookie.
3. **Browser APIs** — State, Poll, Load, Changes, and capabilities return HTTP 401 when the cookie is missing. `withBrowserChanges` still does not call CoreMailbox for a missing cookie.
4. **Login POST** — Login succeeds when submitted user and password match Auth config, including empty Auth. `credentials.add` stays boot + login. GET `/ambit` auto-issue does not call `credentials.add`.
5. **Desktop** — [[src/Desktop/LocalProxy.fs|LocalProxy]] still attaches `deriveToken("", "")` when AuthStore and server-issued cookie are empty. That presents the development credential. It is not a Server skip.

## 3. Files

1. **[[src/Server/RouteRegistration.fs]]** — Removed `Disabled` from `Authentication`. `IsAuthenticated` always checks the cookie. GET `/ambit` auto-issues only for empty Auth. Capabilities no longer skip auth. Login POST accepts empty match. Git-token still returns `disabled` when Auth is empty.
2. **[[src/Server/AuthToken.fs]]** — Comment: development `deriveToken`, not auth-disabled.
3. **[[tests/Server.Tests/TestBackend.fs]]** — `withDevelopmentCookie` replaces `withAuthDisabledCookie`.
4. **[[tests/Server.Tests/BrowserCredentialTests.fs]]** — Empty Auth APIs refuse without cookie (including capabilities). GET `/ambit` auto-issues the development cookie. Auth-enabled GET `/ambit` without cookie lands on login and does not mint.
5. **[[tests/Server.Tests/AuthTokenTests.fs]]** and **[[tests/Server.Tests/ResponseCompressionTests.fs]]** — Rename development cookie wording.
6. **[[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]]** — Spec matches the locked rule. Checkbox **No auth-disabled skip** is `[x]`. Status `coded`.
7. **[[plan/architecture/browser-and-app-auth.md|Browser and App auth]]** — Development credential wording. Git PAT out of scope notes `GitAuthDisabled`.
8. **[[plan/core-creation/project.md]]** — Stage `build`. Actual `30h20m`. Status `coded` on ticket 33.

## 4. Tests

Focused Server tests (not the full suite). Client compile gate not run: Client and Shared were not edited.

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~AuthTokenTests|FullyQualifiedName~BrowserCredentialTests|FullyQualifiedName~GitGatewayTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~GitSaveEndpointTests|FullyQualifiedName~CredentialedChangePostsTests"
```

Passed 58. Failed 1: `AuthTokenTests.upload capability rejects tampering wrong user and expiry` — pre-existing flaky last-character tamper, not this change.

```
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~BrowserCredentialTests|FullyQualifiedName~GitGatewayTests.git-token|FullyQualifiedName~AuthTokenTests.proxyCookieHeader|FullyQualifiedName~AuthTokenTests.deriveToken|FullyQualifiedName~CoreRuntimeTests.CoreRuntime seeds"
```

Passed 15.

```
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~GitSaveEndpointTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~ResponseCompressionTests"
```

Passed 12.

## 5. Remaining risks

1. **Login.html after logout** — GET `/ambit` auto-issues the development cookie, so a Development Browser does not open Login.html on first visit. Logout still redirects to Login.html. The form has `required` username and password, so a person cannot submit empty fields. They must open `/ambit` again to get the cookie. Login POST would accept empty Auth if the form posted empty fields.
2. **Git gateway open** — Empty Auth still sets `isGitAuthenticated` true without a PAT and git-token still returns `{ disabled: true }`. That is `GitAuthDisabled`, not the Browser cookie skip.
3. **Secure cookie on HTTP** — Auto-issued `Set-Cookie` still has `Secure = true`. A Development Browser on `http://localhost` may not store it. Desktop LocalProxy still writes the Cookie header in code. Same as before.
4. **Review** — Status is `coded`, not `done`. `done` waits for review approval.

## 6. Next step

Review the implementation of [[plan/core-creation/issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]]. Do not commit unless Alan asks.
