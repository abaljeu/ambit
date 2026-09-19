# 36 — Mailbox is the only Core door

**Status:** done
**Blocked by:** None — can start immediately. Builds on the Caller-set remake in [[../reports/corecredentials-caller-set.md]]. Does not rewrite [[33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]].
Estimate: 2h
Actual: 9h45m

## 1. Context

[33 — Credentialed Browser Change posts](33-credentialed-browser-change-posts.md) made cookie-as-credential and CoreMsg admit for Browser Change posts. The Caller-set remake made mailbox-owned [[src/Server/Core/CoreCredentials.fs]] a Set of Caller. Those are not the remaining hole.

Security was copied across extra doors: HTTP `IsAuthenticated` re-derived the cookie token, [[src/Server/Core/CoreRuntime.fs]] invented Authority and empty `name`, Graph-only posts skipped Caller, and [[src/Server/Core/MailboxHost.fs]] exposed `MailboxProcessor<CoreMsg>` so callers could Post around [[src/Server/Core/CoreMailbox.fs]].

Architecture seam **CoreMailbox door**: [Core creation architecture](../arch.md). Report: [[../reports/mailbox-single-door.md]].

## 2. What to build

Collapse multiple entrances so the mailbox is the only who-may-talk decision. Adapter may refuse a missing cookie as HTTP 401 without calling Core. Adapter must not re-derive and compare the token as a second admission.

### 1. Two copies of who may talk to Core

1. [x] Remove `IsAuthenticated` cookie `== AuthToken.deriveToken` as admission.
2. [x] Missing cookie stays Adapter HTTP 401 without Core.
3. [x] Present cookie is admitted only via mailbox `CoreCredentials.contains` / `admitCaller`.
4. [x] Logout revokes the Caller at the mailbox, then ClearCookie.

### 2. CoreRuntime is not a second public Core door

1. [x] HTTP uses the mailbox door, not handle factories that invent Authority or empty `name`.
2. [x] Thin CoreRuntime to composition (host plus in-process Parse Caller). Do not add factories.
3. [x] File snapshot helpers remain persist plumbing on CoreMailbox.

### 3. Unauthenticated write door

1. [x] `PostGraphOnlyChange` carries a real Caller and goes through `admitCaller`.
2. [x] Seed a process/Parse Caller in the mailbox set. Do not impersonate the Browser secret.
3. [x] Do not leave a no-Caller Post.

### 4. CoreMsg leaked past the door

1. [x] Hide the public `MailboxProcessor<CoreMsg>` field.
2. [x] Persist agents post SnapshotDone through a private snapshot channel, not a public mailbox field.
3. [x] Tests and HTTP must not Post CoreMsg around CoreMailbox.

### 5. Second CoreChanges handle

1. [x] Actors receive [[src/Server/Core/CoreMailbox.fs]] `coreChanges`. No Actor `makeCoreChanges` that swallows getRevision / getChangesSince.

## 3. Out of scope

1. [x] Second CoreChanges handle (`coreChanges` vs Actor `makeCoreChanges` that swallows errors). Done as section 5. Report: [[../reports/actor-corechanges-mailbox-door.md]].
2. Pool as second lifecycle door; Actor kind as CSS string; unread StartActorRequest.revision. ActorStop double admit closed: [[../reports/actorstop-single-admit.md]].
3. Two Histories (persist undo vs mailboxHistory); ActorStarted authority string; graph restart not restoring Actor lifecycle.

## 4. See also

[Core creation architecture](../arch.md), [33 — Credentialed Browser Change posts](33-credentialed-browser-change-posts.md), [[../reports/corecredentials-caller-set.md]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]

## 5. Comments

- 2026-09-14 — Filed for the multiple-entrances cut. Keep [33 — Credentialed Browser Change posts](33-credentialed-browser-change-posts.md) as the coded Browser-path ticket.
- 2026-09-14 — Implemented mailbox-only admission, thinned CoreRuntime, credentialed Graph-only, hidden CoreMsg processor. Status `coded`. Report: [[../reports/mailbox-single-door.md]].
- 2026-09-15 — Two-axis review corrections. Status stays `coded`. Report: [[../reports/36-review-corrections.md]].
- 2026-09-15 — Six-arg route clump is [AmbitApp](../reports/ambitapp-record.md). Status stays `coded`.
- 2026-09-15 — GitGateway, WebDAV, diagnostics, HttpResponseLog take `this` (AmbitApp), not unpacked App/Auth/DataDir. Status stays `coded`.
- 2026-09-15 — `CreateRuntime` collapses Auth/DataDir unpack into CoreRuntime. Persistence is not an AmbitApp member. Status stays `coded`.
- 2026-09-15 — Shell registration clump is `AppShellContext`. Status stays `coded`.
- 2026-09-15 — `AppShellContext` built once and passed to auth/state/save/shell register helpers. Status stays `coded`.
- 2026-09-15 — `serveUserCss` and `renderGambolHtml` take `AppShellContext`, not unpacked fields. Status stays `coded`.
- 2026-09-15 — GitGateway handlers take `AppShellContext` for git auth and DataDir; flush/reconcile stay args. Status stays `coded`.
- 2026-09-15 — `CoreBoot` is the Core-owned create input. Status stays `coded`.
- 2026-09-15 — `CoreBoot` now reaches `startHost` / `bootCallers`; Adapter `CreateBoot` so RouteRegistration does not rebuild the persist list. Status stays `coded`.
- 2026-09-15 — `GitGateway.Routes` holds shell/flush/reconcile; built once at `registerPersistenceAndRoutes`. Status stays `coded`. Report: [[../reports/gitgateway-routes-type.md]].
- 2026-09-15 — Actors use mailbox `coreChanges`; removed Actor `makeCoreChanges` swallow. Status stays `coded`. Report: [[../reports/actor-corechanges-mailbox-door.md]].
- 2026-09-15 — ActorStop admits once on the mailbox path; `pool.finish` drops without re-admit. Status stays `coded`. Report: [[../reports/actorstop-single-admit.md]].
- 2026-09-19 — Independent Spec review approve → Status `done`. Report: [spec-review-33-36](../reports/spec-review-33-36.md).

## Time

- 2026-09-14 2h — Collapse extra Core doors onto CoreMailbox (from chat)
- 2026-09-15 1h30m — Two-axis review corrections (from chat)
- 2026-09-15 1h — AmbitApp record for the route parameter clump (from chat)
- 2026-09-15 30m — Helpers take AmbitApp `this` instead of unpacked fields (from chat)
- 2026-09-15 15m — CreateRuntime; no Persistence member (from chat)
- 2026-09-15 15m — AppShellContext for CSS and /ambit shell routes (from chat)
- 2026-09-15 15m — Reuse AppShellContext for auth/state/save register helpers (from chat)
- 2026-09-15 15m — serveUserCss and renderGambolHtml take AppShellContext (from chat)
- 2026-09-15 15m — GitGateway handleInfoRefs/handlePackPost take AppShellContext (from chat)
- 2026-09-15 15m — CoreBoot for CoreRuntime.create (from chat)
- 2026-09-15 15m — CoreBoot through startHost and CreateBoot (from chat)
- 2026-09-15 15m — GitGateway.Routes for registerRoutes/handleInfoRefs/handlePackPost (from chat)
- 2026-09-15 1h15m — Actor CoreChanges through mailbox coreChanges; no makeCoreChanges swallow (from chat)
- 2026-09-15 45m — ActorStop admits once; pool.finish drops without re-admit (from chat)
- 2026-09-19 45m — Independent Spec review vs `fce22cf7` (from chat)
