# 36 — Mailbox is the only Core door

**Status:** coded
**Blocked by:** None — can start immediately. Builds on the Caller-set remake in [[../reports/corecredentials-caller-set.md]]. Does not rewrite [[33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]].
Estimate: 2h
Actual: 2h

## 1. Context

[[33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]] made cookie-as-credential and CoreMsg admit for Browser Change posts. The Caller-set remake made mailbox-owned [[src/Server/Core/CoreCredentials.fs]] a Set of Caller. Those are not the remaining hole.

Security was copied across extra doors: HTTP `IsAuthenticated` re-derived the cookie token, [[src/Server/Core/CoreRuntime.fs]] invented Authority and empty `name`, Graph-only posts skipped Caller, and [[src/Server/Core/MailboxHost.fs]] exposed `MailboxProcessor<CoreMsg>` so callers could Post around [[src/Server/Core/CoreMailbox.fs]].

Architecture seam **CoreMailbox door**: [[../arch.md|Core creation architecture]]. Report: [[../reports/mailbox-single-door.md]].

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

## 3. Out of scope

1. Second CoreChanges handle (`coreChanges` vs Actor `makeCoreChanges` that swallows errors).
2. Pool as second lifecycle door; Actor kind as CSS string; ActorStop double admit; unread StartActorRequest.revision.
3. Two Histories (persist undo vs mailboxHistory); ActorStarted authority string; graph restart not restoring Actor lifecycle.

## 4. See also

[[../arch.md|Core creation architecture]], [[33-credentialed-browser-change-posts.md|33 — Credentialed Browser Change posts]], [[../reports/corecredentials-caller-set.md]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]

## 5. Comments

- 2026-09-14 — Filed for the multiple-entrances cut. Keep 33 as the coded Browser-path ticket.
- 2026-09-14 — Implemented mailbox-only admission, thinned CoreRuntime, credentialed Graph-only, hidden CoreMsg processor. Status `coded`. Report: [[../reports/mailbox-single-door.md]].

## Time

- 2026-09-14 2h — Collapse extra Core doors onto CoreMailbox (from chat)
