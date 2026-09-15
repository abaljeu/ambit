# Mailbox is the only Core door

Date: 2026-09-14

Ticket: [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]]. Status `coded`. Builds on [[corecredentials-caller-set.md]]; does not rewrite [[../issues/33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]].

## 1. What doors existed and what closed

### 1. Two copies of who may talk to Core

Before this cut there were three admissions: HTTP `IsAuthenticated` compared the cookie to `AuthToken.deriveToken`; [[src/Server/RouteRegistration.fs]] `withBrowserChanges` asked mailbox `isAdmitted`; [[src/Server/Core/CoreRuntime.fs]] closed over `browserCredential` and invented `Authority "Browser"` / `"Caller"` plus empty `name`. Logout only `ClearCookie`.

Closed:

1. **DeriveToken equality** — `createAuthentication` no longer compares cookie `== deriveToken`. Cookie issuance still uses `deriveToken` for `SetCookie`. That is issuance, not admission.
2. **Missing cookie** — Adapter HTTP 401 without calling Core (`tryCookieCaller` is `None`).
3. **Present cookie** — Built as a Browser Caller and admitted only via mailbox `isAdmitted` / `admitCaller` / `CoreCredentials.contains`. After Core starts, bool-gated routes (`IsAuthenticated`) use the same mailbox query (`Async.RunSynchronously` on `AdmitCaller`).
4. **Logout** — `CoreMailbox.logout` removes the request Caller from the mailbox set, then `ClearCookie`.

### 2. CoreRuntime as a second public Core door

Before: production `PersistenceContext.Core` was three handle factories (`changes` / `bindChanges` / `browserChanges`) plus File snapshot helpers. HTTP and tests talked to that wrapper; tests talked to [[src/Server/Core/CoreMailbox.fs]].

Closed:

1. **Handle factories** — Removed. CoreRuntime is `{ host; parseCaller }`. HTTP calls `CoreMailbox` on `host` and passes a request-carried Caller.
2. **Invented Authority / empty name at the wrapper** — Adapter builds the Browser Caller from the cookie (`BrowserRequestCreds.callerFromSecret`). Empty `name` stays the Caller-set remake tail so the boot-seeded cookie still matches.
3. **File snapshot helpers** — Stay persist plumbing: `CoreMailbox.flushSnapshot` / `getRevision`. Not a security door; not expanded.

### 3. Unauthenticated write door

Before: `PostGraphOnlyChange` had no Caller and skipped `admitCaller`. `parseBound` stamped `Authority "Parse"` then posted Graph-only, so the stamp was unused. Process writers impersonated the seeded Browser secret.

Closed:

1. **Caller on Graph-only** — `PostGraphOnlyChange` carries Caller and goes through `admitCaller` before PersistHandlers.
2. **Process Parse Caller** — Boot seeds `{ authority = Parse; name = "process"; secret = "parse:" + deriveToken }` into the mailbox set. Distinct from the Browser cookie. Parse / git reconcile use that Caller. No no-Caller Post remains.

### 4. CoreMsg leaked past the door

Before: `MailboxHost.mailbox` was a public `MailboxProcessor<CoreMsg>`. `PersistFilling.bindMailbox` took the same type. Tests posted `StartActor` / `ActorStop` around CoreMailbox. DbAgent posted `SnapshotDone` on the raw processor.

Closed:

1. **Public processor** — `MailboxHost` is a private-field record. `CoreMsg` is `internal`. Tests and HTTP cannot Post CoreMsg.
2. **Private snapshot channel** — `PersistFilling.bindSnapshot: Graph option -> unit`. DbAgent posts snapshot completion through that channel; CoreMailbox binds it to `SnapshotDone` internally.
3. **Door-only tests** — `CoreMsgActorCasesTests` now uses `CoreMailbox.startActor` / `actorStop`.

## 2. Remaining doors not touched

These stay out of scope of the original door cut: the second CoreChanges handle is closed in [[actor-corechanges-mailbox-door.md]]; pool as second lifecycle door; two Histories. No mechanical change was required in the latter two to close 1–4.

1. **Second CoreChanges handle** — Closed. Actors receive `CoreMailbox.coreChanges`. Report: [[actor-corechanges-mailbox-door.md]].
2. **Pool as second lifecycle door** — Actor kind as CSS string; unread `StartActorRequest.revision`. ActorStop double admit is closed in [[actorstop-single-admit.md]].
3. **Two Histories** — persist undo vs mailboxHistory; ActorStarted authority string; graph restart not restoring Actor lifecycle.

## 3. Files changed and tests

### 1. Product

1. [[src/Server/Core/MailboxHost.fs]] — private mailbox; `bindSnapshot` instead of `bindMailbox`.
2. [[src/Server/Core/CoreMailboxBackend.fs]] — internal `CoreMsg`; Graph-only + Logout carry Caller; Graph-only admits.
3. [[src/Server/Core/CoreMailbox.fs]] — only public CoreMsg door; `logout`; Graph-only takes Caller; snapshot bind.
4. [[src/Server/Core/CoreRuntime.fs]] — thinned to `host` + `parseCaller`; boot seeds Browser and Parse Callers.
5. [[src/Server/Core/DbAgent.fs]] — snapshot poster, not `MailboxProcessor<CoreMsg>`.
6. [[src/Server/Core/FileAgent.fs]] — `bindSnapshot = ignore`.
7. [[src/Server/BrowserRequestCreds.fs]] — `callerFromSecret` / `tryCookieCaller`.
8. [[src/Server/RouteRegistration.fs]] — mailbox admit; parse Caller; logout revoke; no deriveToken compare.

### 2. Tests

1. [[tests/Server.Tests/CoreMailboxDoorTests.fs]] — Graph-only admit/refuse; logout; no public mailbox field; CoreMsg not public.
2. [[tests/Server.Tests/CoreRuntimeTests.fs]] — mailbox door; Parse Caller; no credential factories; Graph-only refuses inactive.
3. [[tests/Server.Tests/CredentialedChangePostsTests.fs]] — cookie Caller via CoreMailbox.
4. [[tests/Server.Tests/CoreMsgActorCasesTests.fs]] — CoreMailbox instead of raw Post.
5. [[tests/Server.Tests/TestActorHelloTests.fs]] — Graph-only passes `testCaller`.
6. [[tests/Server.Tests/BrowserCredentialTests.fs]] — present cookie admitted only when mailbox contains.
7. [[tests/Server.Tests/HttpResponseLogTests.fs]] — development cookie so mailbox admit matches empty-Auth boot seed.
8. [[tests/Server.Tests/WorkspaceWebDavTests.fs]] — same cookie on the WorkspaceFileSync client.

### 3. Commands and results

Foreground (related only; not the full suite):

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~BrowserCredentialTests|FullyQualifiedName~CoreCredentialsTests|FullyQualifiedName~TestActorHelloTests|FullyQualifiedName~GraphOnlyChangePostTests|FullyQualifiedName~CoreActorPoolTests"
```

Passed: 67.

Then HTTP admission neighbors (after cookie fix on two tests):

```
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~HttpResponseLogTests|FullyQualifiedName~WorkspaceWebDavTests|FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~BrowserCredentialTests|FullyQualifiedName~CoreCredentialsTests|FullyQualifiedName~TestActorHelloTests|FullyQualifiedName~GraphOnlyChangePostTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~StateEndpointTests|FullyQualifiedName~GitSaveEndpointTests|FullyQualifiedName~ChangeEndpointResilienceTests|FullyQualifiedName~ApiGetStateTests|FullyQualifiedName~ApiPostLoadTests"
```

Passed: 178. Failed: 0.

Client compile gate skipped: Client/Shared were not edited.

## 4. Assumptions

1. **Logout removes the mailbox Caller.** Mailbox-as-security-place: revoke then ClearCookie. The removed Caller is `{ authority = Browser; name = ""; secret = cookie }`, matching boot seed and login.
2. **Empty `name` stays.** That is the Caller-set remake tail, not this hole. HTTP still passes `name = ""` so the development cookie matches.
3. **Parse secret is `"parse:" + deriveToken`.** In-process only; never issued as `gambol_auth`. Distinct from the Browser cookie so process writes do not impersonate the Browser secret.
4. **Bool `IsAuthenticated` after Core exists is mailbox `isAdmitted`.** Missing cookie is false without Core. Present cookie queries the mailbox. `Async.RunSynchronously` is used only for existing `HttpRequest -> bool` module gates (WebDAV, diagnostics, capabilities). Core-facing Browser APIs stay async `withBrowserChanges`.
5. **SetCookie still derives the token.** Issuing the cookie is not a second admission. Admission is mailbox contains of that value as a Caller.
6. **No commit.** User did not ask for a commit.

## 5. Next step for the human

Review [[../issues/36-mailbox-is-the-only-core-door.md|36 — Mailbox is the only Core door]] (Status `coded`, not `done`). Second CoreChanges handle is closed in [[actor-corechanges-mailbox-door.md]]. Remaining: pool as second lifecycle door; two Histories. No commit unless you ask for one.
