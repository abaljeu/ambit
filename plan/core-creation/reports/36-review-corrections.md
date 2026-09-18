# 36 review corrections

Date: 2026-09-15

Ticket: [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Status stays `coded`. No commit.

## 1. Spec — present-cookie IsAuthenticated

[[src/Server/RouteAuthentication.fs]] `create` (public factory [[src/Server/RouteRegistration.fs]] `createAuthentication`) sets `IsAuthenticated` to `fun _ -> false`. A present cookie is not authenticated at the factory. Missing cookie is false. If Core does not exist yet, a present cookie is not admitted.

[[src/Server/RouteRegistration.fs]] `registerPersistenceAndRoutes` still replaces `IsAuthenticated` with mailbox `CoreMailbox.isAdmitted` after Core starts. Bool-gated routes use that one mailbox query. Missing cookie stays Adapter HTTP 401 without Core.

## 2. Spec — logout vs development auto-issue

Compatible with [[../issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]]: `GET /ambit` may auto-issue; `credentials.add` stays boot + login.

[[src/Server/RouteAuthentication.fs]] `loginThenSetCookie` calls `CoreMailbox.login` first, then `SetCookie`. It does not SetCookie a secret the mailbox set does not contain. Development `GET /ambit` auto-issue and HTTP login POST both use that helper. After logout, a reissued development cookie is admitted only because login put the Caller back.

## 3. Standards — 100-char line

Wrapped the Internal server error loading state interpolator in [[src/Server/RouteRegistration.fs]] to ≤100 characters.

## 4. Standards — 400-line files

Split so this work does not leave the door-cut files longer:

1. [[src/Server/Core/CoreMsg.fs]] — `CoreMsg` and `PersistHandlers` (was in [[src/Server/Core/CoreMailboxBackend.fs]]).
2. [[src/Server/RouteTypes.fs]] — `Authentication`, `PersistenceContext`, `RouteAssets`, `BuildStamps`.
3. [[src/Server/RouteAuthentication.fs]] — factory admission and `loginThenSetCookie`.
4. [[src/Server/RouteAppShell.fs]] — stamps, CSS, `/ambit` shell, development auto-issue.

Registered in [[src/Server/Gambol.Server.fsproj]]. Line counts after split: CoreMailboxBackend 381; RouteRegistration 387.

## 5. Refer by name

[[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] Comments name [[../issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]].

[[mailbox-single-door.md]] remaining-door sentences name the second CoreChanges handle; pool as second lifecycle door; two Histories.

## 6. Unused MailboxHost.post

Deleted unused `MailboxHost.post`. `postAndAsyncReply` stays as the private-processor accessor.

## 7. isReady vs isReadyFn

Deleted `isReadyFn`. `MailboxHost.isReady` returns the stored `unit -> bool` probe. [[src/Server/Core/CoreMailbox.fs]] `coreChanges` passes that function through. `CoreMailbox.isReady` invokes it. No extra wrapper. The processor stays private.

## 8. Logout removeCaller

[[src/Server/Core/CoreMailboxBackend.fs]] `removeCaller` sits beside `addCaller`. Logout uses it. Cookie-presence `IsAuthenticated` is the same hole as section 1.

## 9. Tests

Seams: `createAuthentication` / `IsAuthenticated`; logout then auto-issue without login vs auto-issue that logins.

1. `createAuthentication IsAuthenticated is false for a present unknown cookie`
2. `createAuthentication IsAuthenticated is false when a cookie is missing`
3. `after logout the development cookie is not admitted without login` (`GET /ambit/state` 401)
4. `after logout GET /ambit login auto-issue admits the development cookie`

Foreground:

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~BrowserCredentialTests|FullyQualifiedName~CoreCredentialsTests|FullyQualifiedName~TestActorHelloTests|FullyQualifiedName~GraphOnlyChangePostTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~HttpResponseLogTests|FullyQualifiedName~WorkspaceWebDavTests|FullyQualifiedName~StateEndpointTests|FullyQualifiedName~GitSaveEndpointTests|FullyQualifiedName~ChangeEndpointResilienceTests|FullyQualifiedName~ApiGetStateTests|FullyQualifiedName~ApiPostLoadTests"
```

Passed: 181. Failed: 1 flake (`CoreActorPool.startActor selects actor from command node text`); that fact passed on immediate re-run with `--no-build`. Then re-ran BrowserCredential, CoreMailboxDoor, CoreRuntime, CredentialedChangePosts after the RouteAuthentication helper split.

Client compile gate skipped: Client/Shared were not edited.

## 10. Leftover

None. Did not undo the door cut. Did not start the remaining doors (second CoreChanges handle; pool as second lifecycle door; two Histories). Did not edit [[.agents/skills/code-review/scripts/standards-scan.py]] or the code-review skill. Product Core files were not emptied.
