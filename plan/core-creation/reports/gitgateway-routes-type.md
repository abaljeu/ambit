# GitGateway Routes type

Date: 2026-09-15

Ticket: [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Status stays `coded`. Named-type fix for the git shell/flush/reconcile clump leftover after [[ambitapp-record.md|AmbitApp]] and `AppShellContext`. Did not reopen mailbox-door product work. Did not split [[src/Server/GitGateway.fs]]. Did not shorten `handlePackPost`. No commit.

## 1. Type shape

[[src/Server/GitGateway.fs]] `Routes` is the registration package reused by every function that already took that clump:

1. `shell` — `AppShellContext` (git auth and DataDir).
2. `flush` — `FlushFn` (same alias as before).
3. `reconcile` — `ReconcileFn` (same alias as before).

Name is `Routes` (same role as `registerRoutes`). Not a one-off `HandlePackPostArgs`. `FlushFn` / `ReconcileFn` stay aliases; they are fields. Per-request `service`, `ctx`, and `repoName` stay arguments. They are not on the record. ASP.NET binds `ctx` and `repoName`; the two `MapPost` sites pass `WorkspacePull` / `WorkspacePush`.

## 2. Call sites

1. [[src/Server/GitGateway.fs]] `registerRoutes (routes: Routes)` maps GET info/refs and POST upload-pack / receive-pack. It reads `routes.shell.AmbitApp` for `MapGet` / `MapPost` and passes the same record into the handlers.
2. `handleInfoRefs (routes: Routes) (ctx: HttpContext) (repoName: string)` reads `shell` and `flush`. It does not use `reconcile`.
3. `handlePackPost (routes: Routes) (service: Service) (ctx: HttpContext) (repoName: string)` reads all three fields. The handler body is unchanged except those field reads.
4. [[src/Server/RouteRegistration.fs]] `registerPersistenceAndRoutes` builds the record once (`shell` is the existing `AppShellContext` local; `flush` is `flushForGit`; `reconcile` is `reconcileGitPush`) and passes it to `GitGateway.registerRoutes`.

[[src/Server/GitGateway.fs]] stays one file (459 lines after this cut). `handlePackPost` stays one function.

Admission behavior is unchanged: git Basic auth still uses `Auth.IsGitAuthenticated`; flush and post-receive reconcile still run on the same paths.

## 3. Tests

Foreground:

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~GitGatewayTests|FullyQualifiedName~GitSave|FullyQualifiedName~WorkspaceGitTests"
```

Passed: 64. Failed: 0.

Client compile gate skipped: Client/Shared were not edited.
