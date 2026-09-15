# AmbitApp record

Date: 2026-09-15

Ticket: [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Status stays `coded`. Named-type fix for the six-arg route clump leftover from the split. Did not reopen mailbox-door product work. Did not start remaining doors (second CoreChanges handle; pool lifecycle; two Histories). No commit.

## 1. Type shape

[[src/Server/AmbitApp.fs]] `AmbitApp` is an Adapter/host record (compiled before HTTP helpers so they can take it). It is not a Core door. Fields:

1. `Config` — `IConfiguration` (same meaning as before).
2. `Auth` — `Authentication` (factory admission false; mailbox admit after Core starts).
3. `PublicAssetBaseOpt` — `string option`.
4. `DataDirResult` — `Result<string, exn>`. `DataDir` reads the Ok path (empty on Error).
5. `App` — `WebApplication` field only. Helpers do not take a naked `WebApplication` parameter.
6. `HttpLogFile` — log path from [[src/Server/HttpResponseLog.fs]] `register`.

Members wrap the host APIs those helpers actually call: `WebRootPath`, `Lifetime`, `DataDir`, `Use`, `MapGet`, `MapPost`, `MapFallback`, `MapMethods`. [[src/Server/RouteRegistration.fs]] `CreateBoot` reads Auth, DataDir, and persist choice from `this` and returns [[src/Server/Core/CoreRuntime.fs]] `CoreBoot` (persist + auth seed + actors). `createPersistenceContext` calls `CreateBoot` once, maps persist fields into PersistenceContext, and passes the same record to `CoreRuntime.create`. `create`, `bootCallers`, and `startHost` take that record (plus pool and credentials next to `startHost`). They do not unpack persist/auth fields just to pass the same list one frame down. `CoreMailbox.host` stays PersistFilling + credentials (already composed). FileAgent/DbAgent stay primitive persist factories. Core does not take AmbitApp or AppShellContext. PersistenceContext is not an AmbitApp field or member.

`AmbitApp.create` takes the six fields once they exist. The record is immutable. When `Auth.IsAuthenticated` becomes mailbox `CoreMailbox.isAdmitted`, [[src/Server/RouteRegistration.fs]] `withMailboxAdmit` returns `{ this with Auth = ... }`. No `mutable`, no ref cell, no in-place field write.

## 2. Call sites changed

1. [[src/Server/Server.fs]] `configureApplication` builds `AmbitApp` then calls `RouteRegistration.registerPersistenceAndRoutes ambit`. Collapsed the one-line `RouteRegistration.createAuthentication` alias; factory is [[src/Server/RouteAuthentication.fs]] `create`.
2. [[src/Server/RouteRegistration.fs]] `registerPersistenceAndRoutes` takes `this: AmbitApp` and returns the updated record. GitGateway, WebDAV, diagnostics, HttpResponseLog error-report, DailyGitSave, and LazyLoad reconciliation register helpers take `this` as the one host parameter. They read `App`, `Auth`, `DataDir` / `DataDirResult`, and `HttpLogFile` inside. RouteRegistration does not rebuild a persist-mode / db-status / connection-string / actors list at the Core boot call. `this.CreateBoot persistenceMode` is the Adapter construction. PersistenceContext stays a local value, not an AmbitApp member.
3. [[src/Server/RouteTypes.fs]] `AppShellContext` holds `AmbitApp`, `Assets`, `Stamps`, and `Persistence`. It is not an AmbitApp field. `registerPersistenceAndRoutes` builds it once and passes it to `registerAuthRoutes`, `registerStateRoutes`, `registerSaveRoutes`, the error-report route, [[src/Server/RouteAppShell.fs]] shell helpers, and [[src/Server/GitGateway.fs]] `registerRoutes`. `handleInfoRefs` / `handlePackPost` take that record and read `Auth.IsGitAuthenticated` and `Persistence.DataDir` inside. Flush, reconcile, service, ctx, and repoName stay git-specific args. `serveUserCss` and `renderGambolHtml` take the same record (plus request `programFile` for the HTML). `createBuildStamps` still takes `AmbitApp`. HttpResponseLog stays on AmbitApp (that file compiles before RouteTypes); the call site still passes `AppShellContext`. GitGateway.fs compiles after RouteTypes so it can take the record.
4. [[src/Server/RouteAuthentication.fs]] unchanged. `loginThenSetCookie` still logins at the mailbox then SetCookie. `IsAuthenticated` at the factory is `fun _ -> false`.
5. Server bootstrap before the six fields exist (`useResponseCompression`, static files, HttpResponseLog.register, production-config guard) still takes `WebApplication`. Those calls collect the fields; they do not invent placeholder `Auth` / `DataDirResult` on a half-built record.

Admission behavior is unchanged: missing cookie is Adapter HTTP 401 without Core; present cookie is mailbox admit; no `deriveToken` equality as admission.

## 3. Tests

[[tests/Server.Tests/BrowserCredentialTests.fs]] `authFromMemory` calls `RouteAuthentication.create` (the factory). Test names still say `createAuthentication`. No WebApplicationFactory rewrite.

Foreground:

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~BrowserCredentialTests"
```

Passed: 47. Failed: 0.

Client compile gate skipped: Client/Shared were not edited.

## 4. How far CoreBoot reaches

1. Adapter: `AmbitApp.CreateBoot persistenceMode` → `createPersistenceContext` → `CoreRuntime.create boot`.
2. Core: `create` → `bootCallers boot` and `startHost boot pool credentials`.
3. Stops before `CoreMailbox.host` (PersistFilling + credentials) and FileAgent/DbAgent factories (dataDir / connection string only).
