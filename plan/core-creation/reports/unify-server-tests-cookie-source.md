# Unify Server.Tests cookie source

The 401s were not two cookie helpers. HTTP tests already sent [[src/Server/AuthToken.fs]] `cookieHeaderValue`. Admission compared the full [[src/Server/Core/CoreChanges.fs]] Caller. [[src/Server/BrowserRequestCreds.fs]] built `name = "Browser"` while [[src/Server/Core/CoreRuntime.fs]] seeded `name = ""`. `Set.contains` failed. The comment on `callerFromSecret` already said empty name is the cookie session key.

## Cookie source

One source: `AuthToken.cookieHeaderValue` / `AuthToken.deriveToken`.

[[tests/Server.Tests/TestBackend.fs]] is the test attachment point:

- `withAuthCookie username password` sets the request `Cookie` header from `AuthToken.cookieHeaderValue`.
- `withDevelopmentCookie` is the empty-Auth alias (`""` / `""`) that factories already used.
- `browserCallerFromAuth username password` is the mailbox Caller for that same secret (`BrowserRequestCreds.callerFromSecret` + `deriveToken`).

HTTP empty-Auth clients (`createClientForDir`, `createDbClientForDir`, …) still pipe `withDevelopmentCookie`. Named Auth clients that must be admitted pipe `withAuthCookie user pass`. `createClientForDirWithoutCookie` stays cookie-less. `createClientForDirWithAuth` still does not attach a cookie (401 facts need that). Mailbox-direct `testCaller` (`Authority "Test"`) is not a Browser cookie; it is unchanged.

## Files changed

- [[src/Server/BrowserRequestCreds.fs]] — `callerFromSecret` uses `name = ""` so HTTP matches CoreRuntime boot and `CoreMailbox.login host "" secret`.
- [[tests/Server.Tests/TestBackend.fs]] — single cookie helper + matching Browser Caller.
- [[tests/Server.Tests/CoreRuntimeTests.fs]] — `browserCallerFromAuth` (alice/secret still matches that runtime's `AuthUser` / `AuthPass`).
- [[tests/Server.Tests/CredentialedChangePostsTests.fs]] — live cookie Callers go through `browserCallerFromAuth`.
- [[tests/Server.Tests/BrowserCredentialTests.fs]] — live cookie via `withAuthCookie`; removed a local `addLiveCookie`.
- [[tests/Server.Tests/WorkspaceWebDavTests.fs]] — ForceAsync client uses `withDevelopmentCookie`.
- [[tests/Server.Tests/GitGatewayTests.fs]] — named Auth cookie via `withAuthCookie`.

## How tests get the cookie

Empty Auth HTTP: factory → `withDevelopmentCookie` → `gambol_auth=deriveToken("", "")`. CoreRuntime seeds the same token with empty name.

Named Auth HTTP: `createClientForDirWithAuth` then `withAuthCookie user pass` → `deriveToken(user, pass)`.

Core in-process: `browserCallerFromAuth user pass` → `{ authority = Browser; name = ""; secret = deriveToken(user, pass) }`.

## Focused tests

Build: `dotnet build tests/Server.Tests -c Debug` — green.

Original 401 filter:

```
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~LazyLoadReconciliationServerTests|FullyQualifiedName~CoreChangesTests|FullyQualifiedName~CoreCredentialsTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~ChangeEndpointResilienceTests|FullyQualifiedName~StateEndpointTests"
```

Passed: 97. Failed: 9. Skipped: 0. Total: 106.

Admission modules edited here (`CoreRuntimeTests|BrowserCredentialTests|CredentialedChangePostsTests|CoreCredentialsTests|CoreChangesTests`): Passed: 33. Failed: 0.

Wider filter (original plus GitGateway / WorkspaceWebDav / BrowserCredential / CredentialedChangePosts): Passed: 154. Failed: 9. Total: 163.

## Remaining Unauthorized

None. The 9 failures are not 401.

All nine are [[tests/Server.Tests/StateEndpointTests.fs]]: eight Expected OK / Actual BadRequest on concurrent stale text/name/class and same-parent structural collision (File and Db); one log-prefix fact (`00000000`). Those POSTs now pass admission. They fail later (amendment / event-log format). Previously 401 hid that path.

## Next step

Diagnose the nine StateEndpointTests BadRequest / log-format facts. Do not treat them as cookie work. No commit unless asked.
