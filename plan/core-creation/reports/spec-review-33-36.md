# Spec review — [33 — Credentialed Browser Change posts](../issues/33-credentialed-browser-change-posts.md) and [36 — Mailbox is the only Core door](../issues/36-mailbox-is-the-only-core-door.md)

Independent Spec review. Not a Standards review.
**Pin:** software at `fce22cf7f5ead1db27d0b5635c610c8c54d432f0` (staging tip named for this review). Question: are the ticket What-to-build demands true in software at that tip?
**Spec:** [33 — Credentialed Browser Change posts](../issues/33-credentialed-browser-change-posts.md); [36 — Mailbox is the only Core door](../issues/36-mailbox-is-the-only-core-door.md). Aids: arch Story path **Browser Change posts**, shared segment **Credentialed `PostEvent` through CoreMsg**, seam **CoreMailbox door**, seam **Credentialed Change posts** on [Core creation architecture](../arch.md).
**Standards:** deferred (Alan: Spec only).
**Mechanical scan:** not run.
**Focused tests:** `dotnet test tests/Server.Tests --filter` CredentialedChangePosts / BrowserCredential / CoreMailboxDoor / CoreRuntimeTests / ActorCoreChangesDoor / GraphOnlyChangePost / CoreCredentialsTests / AuthTokenTests — 69 passed, 0 failed.

## Standards

Deferred. No Standards axis this run.

## 33 — Credentialed Browser Change posts

### Spec

Question: are the What-to-build demands of [33 — Credentialed Browser Change posts](../issues/33-credentialed-browser-change-posts.md) true at the pin? Extra later remakes (Event door name, mailbox-owned Caller set) are fulfillment, not 33 defects. Arch Story path **Browser Change posts** hops 1–4 are `[x]`.

#### Demands that hold

Credential identity (ticket lines 22–25). Cookie name is `gambol_auth` ([AuthToken.fs](src/Shared/dotnet/AuthToken.fs) `cookieName`). Boot seeds a Browser Caller whose secret is `AuthToken.deriveToken` of Auth config ([CoreRuntime.fs](src/Server/Core/CoreRuntime.fs) `bootCallers`). Login POST calls `loginThenSetCookie` then `SetCookie` of that same derived token ([RouteAuthentication.fs](src/Server/RouteAuthentication.fs) 59–73; [RouteRegistration.fs](src/Server/RouteRegistration.fs) 105–119). GET `/ambit` auto-issues that cookie only when Auth user and password are empty ([RouteAppShell.fs](src/Server/RouteAppShell.fs) 139–153). Factory `IsAuthenticated` is `fun _ -> false` ([RouteAuthentication.fs](src/Server/RouteAuthentication.fs) 52). Empty Auth without a cookie is HTTP 401 on Browser APIs ([BrowserCredentialTests.fs](tests/Server.Tests/BrowserCredentialTests.fs) `Empty Auth Browser APIs without cookie are refused`). There is no `auth.Disabled` skip in Server source.

Missing cookie refuses at the Adapter and does not call CoreMailbox (ticket lines 25, 68). [RouteRegistration.fs](src/Server/RouteRegistration.fs) `withBrowserChanges` 88–89: `None -> async.Return(Results.Unauthorized())` before `isAdmitted` / `boundChanges`. HTTP `/ambit/changes` and `/ambit/events` go through that wrapper (212–238). [BrowserCredentialTests.fs](tests/Server.Tests/BrowserCredentialTests.fs) posts without cookie get 401.

Request-carried secret; no closed-over fallback (ticket lines 24, 57, 65). [BrowserRequestCreds.fs](src/Server/BrowserRequestCreds.fs): cookie value is the secret; missing or blank cookie yields no Caller. Desktop attaches Cookie via `AuthToken.proxyCookieHeader` (AuthStore / captured Set-Cookie / development `deriveToken("", "")`) ([AuthToken.fs](src/Shared/dotnet/AuthToken.fs) 30–47; [LocalProxy.fs](src/Desktop/LocalProxy.fs) 115–127). `DeployEpochSec` / `window.__BUILD_TS__` is epoch only ([RouteAppShell.fs](src/Server/RouteAppShell.fs) 108–110; [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) 21–33). Client writes that epoch on poll/load ([App.fs](src/Client/App.fs) 428, 461).

Undo File/Db/GUID plumbing (ticket lines 44–51). [FileAgent.fs](src/Server/Core/FileAgent.fs) `create` / `createWithDependencies` take dataDir and dependencies only. [DbAgent.fs](src/Server/Core/DbAgent.fs) `create*` take connection string / dataDir only. [DatabaseSetup](src/Server/DatabaseSetup.fs) has no credentials. CoreActor / CoreActorPool have no CoreCredentials. [CoreRuntime.fs](src/Server/Core/CoreRuntime.fs) has no `browserChanges` / `addBoundCredentials` / boot GUID ([CoreRuntimeTests.fs](tests/Server.Tests/CoreRuntimeTests.fs) `CoreRuntime is not a second credential factory`).

HTTP Adapter (ticket lines 65–68). [Api.fs](src/Server/Api.fs) `postEvents` decodes an Ev batch and calls `handle.postEvents`. The handle is `CoreMailbox.coreChanges` with the request Caller ([RouteRegistration.fs](src/Server/RouteRegistration.fs) 71–96). Present cookie not in the mailbox set is Adapter 401 via `isAdmitted` (92–95). Persist admission stays in CoreMsg.

CoreMailbox / CoreMsg (ticket lines 74–86; arch hops 3–4; seam **Credentialed Change posts**). Public door is `postEvents` / `postEvent` with Caller (Authority + secret) ([CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) 142–158). `PostEvent` carries Caller ([CoreMsg.fs](src/Server/Core/CoreMsg.fs) 34–41). [CoreEventDispatch.fs](src/Server/Core/CoreEventDispatch.fs) `postEvent` 168–173 admits before persist. PersistHandlers `applyEvent` takes Ev and graphOnly only ([FileAgent.fs](src/Server/Core/FileAgent.fs) 257–258). Live cookie admits; inactive secret is `CoreAuth.refuse` and EventId is unchanged ([CredentialedChangePostsTests.fs](tests/Server.Tests/CredentialedChangePostsTests.fs)). No Actor live-table admit on the Browser path (`admitCaller` uses `hasCaller` unless Authority is Actor).

Restart / seed (ticket lines 29–36). Same `deriveToken` after restart if the Cookie header is sent ([AuthTokenTests.fs](tests/Server.Tests/AuthTokenTests.fs) `proxyCookieHeader stored matches boot-seed deriveToken after restart`). `credentials.add` is boot (`ofCallers`) plus login (`Login` → `addCaller`). No persist of the credential set.

#### Notes that do not falsify 33

Ticket text still says `postChange` / `PostChange`. Software and arch hop 3 say `postEvents` / `PostEvent` (Event migrate on later tickets). That remap is current fulfillment of Story path **Browser Change posts**.
Ticket undo item 3 says remove the credentials param from `createFile` / `createDb`. Those constructors still take `CoreCredentials` as the mailbox seed ([CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) 264–291). File/Db still do not take credentials. Later Caller-set remake owns that seed shape.
Ticket §6.3 said `IsAuthenticated` compares the cookie to `deriveToken`. [36 — Mailbox is the only Core door](../issues/36-mailbox-is-the-only-core-door.md) removed that Adapter compare. The cookie value is still the boot-seeded `deriveToken`; mailbox `contains` is the admit. Empty Auth still requires the cookie.

### Summary

Spec pass. Status `done`. Worst in-axis: none. Residuals are later remaps (door name, mailbox-owned seed set, mailbox `contains` instead of Adapter `deriveToken` compare), not unmet 33 demands.

## 36 — Mailbox is the only Core door

### Spec

Question: are the What-to-build demands of [36 — Mailbox is the only Core door](../issues/36-mailbox-is-the-only-core-door.md) true at the pin? Seam aid: **CoreMailbox door**. Extra later clumps (AmbitApp, AppShellContext, CoreBoot, GitGateway.Routes, ActorStop single admit) stay on this ticket’s Comments; they do not undo the door cut.

#### Demands that hold

§1 Two copies of who may talk (ticket lines 22–25). Factory `IsAuthenticated` is `fun _ -> false` ([RouteAuthentication.fs](src/Server/RouteAuthentication.fs) 39–52). After Core exists, `withMailboxAdmit` sets `IsAuthenticated` to `mailboxIsAuthenticated`: cookie Caller then `CoreMailbox.isAdmitted` (`contains`) ([RouteRegistration.fs](src/Server/RouteRegistration.fs) 327–340). Missing cookie is Adapter 401 without Core (`withBrowserChanges` 88–89). Present foreign cookie is 401 ([BrowserCredentialTests.fs](tests/Server.Tests/BrowserCredentialTests.fs) `present cookie is admitted only when mailbox contains the Caller`). Logout calls `CoreMailbox.logout` then `ClearCookie` ([RouteRegistration.fs](src/Server/RouteRegistration.fs) 123–131). After logout, development APIs are 401 until GET `/ambit` re-issues ([BrowserCredentialTests.fs](tests/Server.Tests/BrowserCredentialTests.fs) 163–184). `SetCookie` still derives the token to issue it; that is not a second admit compare.

§2 CoreRuntime is not a second public Core door (ticket lines 29–31). [CoreRuntime.fs](src/Server/Core/CoreRuntime.fs) is `{ host; parseCaller }`. HTTP uses `CoreMailbox.coreChanges` / `startActor` / `login` / `logout` / `isAdmitted` / `flushSnapshot` / `getEventId`. No `changes` / `bindChanges` / `browserChanges` / `browserCredential` members. File snapshot helpers stay on CoreMailbox (`flushSnapshot`).

§3 Unauthenticated write door (ticket lines 35–37). `PostGraphOnly` carries Caller ([CoreMsg.fs](src/Server/Core/CoreMsg.fs) 14–17). Dispatch admits through `CoreEventDispatch.postEvent` ([CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) 211–218). Boot seeds a Parse process Caller whose secret is not the Browser cookie ([CoreRuntime.fs](src/Server/Core/CoreRuntime.fs) 71–76; [CoreRuntimeTests.fs](tests/Server.Tests/CoreRuntimeTests.fs) `CoreRuntime seeds a Parse process Caller distinct from Browser cookie`). Inactive Graph-only is refused. There is no no-Caller Post case on write messages.

§4 CoreMsg leaked past the door (ticket lines 41–43). [MailboxHost.fs](src/Server/Core/MailboxHost.fs) `mailbox` field is `private`. `CoreMsg` is `internal`. Persist `bindSnapshot` posts `SnapshotDone` on the host’s private processor inside `CoreMailbox.host` ([CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) 253–254), not a public field. Tests assert no public mailbox member and no exported `CoreMsg` ([CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 530–544). Production HTTP and tests Post through CoreMailbox doors, not `MailboxProcessor<CoreMsg>`.

§5 Second CoreChanges handle (ticket lines 47–48). Actors receive `CoreMailbox.coreChanges` via `bindCoreChanges` ([CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) 261; [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) 179). No Actor `makeCoreChanges`. Actor persist errors surface ([ActorCoreChangesDoorTests.fs](tests/Server.Tests/ActorCoreChangesDoorTests.fs)).

#### Notes that do not falsify 36

`CoreAuth.bindHandle` remains as `handle.asCaller` ([CoreCredentials.fs](src/Server/Core/CoreCredentials.fs) 63–64). Tests still use it. Production HTTP binds through `CoreMailbox.coreChanges`. That helper is not a second inbox.
Read doors (`GetState`, `getEventId`) do not carry Caller on CoreMsg. HTTP still refuses a missing cookie before those calls. 36’s hole was write/admit copies and a leaked processor, not every read message.

### Summary

Spec pass. Status `done`. Worst in-axis: none. Residuals are a test-only `bindHandle` alias and Caller-less read messages, not unmet 36 demands.

## Summary

Standards: deferred. Spec: 0 unmet demands on [33 — Credentialed Browser Change posts](../issues/33-credentialed-browser-change-posts.md); 0 unmet demands on [36 — Mailbox is the only Core door](../issues/36-mailbox-is-the-only-core-door.md). Both tickets → `done`.
