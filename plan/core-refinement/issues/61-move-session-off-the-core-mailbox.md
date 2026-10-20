# 61 — Move Session off the core mailbox

**Status:** `defined`
**Type:** coding
**Blocked by:** 63 — Move queries to Ask; 62 — Admit at the entry
**Trackers:** architecture

## Context

`Login`, `Logout`, and `AdmitCaller` are `CoreMsg` cases. `CoreMailboxLoad.runMsg` handles them on the core queue. `RouteRegistration` calls `CoreMailboxApi.isAdmitted` before a route body, and `GET /ambit/logout` calls `CoreMailboxApi.logout`, which removes the caller immediately. Decision 8 **Session leaves** in [Message shape](../cqrs-messages.md): these three leave the core mailbox. Logout stops admitting the caller, queues `CoreQueue.DropAuthority` behind orders already on the queue, and returns. The core removes the authority when it processes that case. At Receive, `CoreMailboxLoad` rejects an order whose current authority generation `DropAuthority` closed. A caller with an open generation still runs. `PlaceOrder` replies `Error` with a reason. Nothing is applied or logged. Login returns `Ok` and, when admission is closed, opens a new generation. `DropAuthority` closes only the generation logout queued. `Login` and `Admit` have no barrier. This ticket is blocked by that ticket and by [62 — Admit at the entry](62-admit-at-the-entry.md). Upload, import, parse, Load, undo, and persist stay working.

## What to build

Move login, logout, and admit onto a `Session` component outside the credential set. `Session` owns whether an authority may start a route and whether that authority still exists. Logout stops admitting, queues `CoreQueue.DropAuthority`, and returns. `CoreMailboxLoad` removes the authority when it processes that case. At Receive, that same component rejects an order whose current generation `DropAuthority` closed. Login during that logout opens a new generation, and the queued drop leaves it open.

### 1. Session API

1. [ ] **Login** — `Login caller` registers the caller's authority and opens admission for it. Login does not await an `Ask`.
2. [ ] **Admit** — `Admit caller` checks whether the caller and authority are currently allowed to start a route. It does not add authority, revoke authority, remove authority, or await an `Ask`.
3. [ ] **Stop admission** — The first logout phase closes admission for that caller. After this phase takes effect, no later admission check for the caller succeeds. Closing admission keeps the authority available to orders that already passed admission.
4. [ ] **Remove authority** — The final logout phase removes the caller's authority when `CoreMailboxLoad` processes `CoreQueue.DropAuthority`. Removal is distinct from closing admission and does not reopen admission.
5. [ ] **Representation** — Login, Admit, and stop-admitting stay off `CoreQueue`. The drop request is `CoreQueue.DropAuthority`. The implementation may use messages, functions, or an equivalent private representation for the Session effects. The credential set stays off `CoreQueue`.

### 2. Change

1. [ ] **Doors** — `CoreMailboxApi.login` and `isAdmitted` use `Session` and do not post. `CoreMailboxApi.logout` stops admitting and queues `CoreQueue.DropAuthority`. It does not post `CoreMsg`. `RouteAuthentication.loginThenSetCookie` still calls login. Remove `Login`, `Logout`, and `AdmitCaller` from `CoreMsg` and from `CoreMailboxLoad.runMsg`.
2. [ ] **Logout sequence** — `RouteRegistration` on `GET /ambit/logout` closes admission for the caller, queues `CoreQueue.DropAuthority`, and returns. `CoreMailboxLoad` removes the authority when it processes that case.
3. [ ] **Queue ordering** — `DropAuthority` joins the core queue behind orders already there. Those orders run before it. The startup prelude serves `Ask` and leaves `DropAuthority` behind those orders. The logout reply has returned. The authority remains until the core processes `DropAuthority`.
4. [ ] **Synchronization seam** — The successful admission check and the handoff to the order door must be synchronized with closing admission. A scoped admission grant is one valid realization, but this ticket does not prescribe a lock, grant type, processor, or file layout.

### Acceptance Criteria

1. [ ] **Cases** — `CoreMsg` has no `Login`, `Logout`, or `AdmitCaller`. A login test reaches `Session` and does not post those cases on `CoreQueue`.
2. [ ] **Barrier** — Post an order, then log out. `Session` refuses a new route for that caller before `CoreQueue.DropAuthority` runs. The order posted before that case is applied before `CoreMailboxLoad` removes the authority.
3. [ ] **Admission race** — Pause a route after it passes admission and before it queues its order, then log out. The logout reply has returned and `DropAuthority` is queued. A later admission check is refused. An order from that open route that queues after `DropAuthority` is rejected at Receive. `PlaceOrder` replies `Error` with a reason. Nothing is applied or logged. Authority removal happens when the core processes `DropAuthority`.
4. [ ] **Login** — `RouteAuthentication.loginThenSetCookie` sets the cookie only after Session login returns `Ok ()`. That path does not post an `Ask`.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **Status** — Status stays `defined`. Acceptance stays unchecked.
2. **Session API** — `Session.login` registers authority and opens admission. `Session.admissionIsOpen` checks a caller. `Session.beginAdmittedRoute` and `Session.finishAdmittedRoute` hold one route from the check until that route queues or abandons. `Session.stopAdmitting` closes admission and leaves the authority in place. `Session.waitForAdmittedRoutes` completes when those routes finish. `Session.removeAuthority` drops only the generation that logout closed.
3. **Logout order** — `GET /ambit/logout` in [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) calls `CoreMailboxApi.logout`. That door stops admitting, queues `CoreQueue.DropAuthority`, and returns. `CoreMailboxLoad` removes that generation when it processes the case. Login registers the authority on Session. Admit checks Session.
4. **Scoped routes** — Parse, save, reconciliation, and the other browser order routes hold one admission through the Core handoff.
5. **Actor start** — The core still refuses an Actor whose secret is not live. Browser authority stays on `Session`.
6. **Drop request** — Alan, 2026-10-20. Logout queues `CoreQueue.DropAuthority` behind orders already on the queue. The core removes the authority when it processes that case. The startup prelude serves `Ask` and leaves the drop behind queued orders.
7. **Reply** — Alan, 2026-10-20. The logout route returns when admission is stopped and `CoreQueue.DropAuthority` is queued. `Session.waitForAdmittedRoutes` stays available for a caller that wants to observe open routes. The logout reply does not call it, and it does not wait for an `Ask`. Orders already on the queue are ahead of the drop, so that wait is not the drain.
8. **Late order** — Alan, 2026-10-20. `CoreMailboxLoad`, at Receive, rejects a caller whose current authority generation `DropAuthority` closed. The reply is `Error` "authority dropped". `PlaceOrder` uses that reply. A `CoreMsg` order uses the same reason and skips apply and the log. An Actor order stays held by the live pool secret. A caller with an open generation still runs. An order already ahead of `DropAuthority` still runs.
9. **One generation map** — Alan, 2026-10-20. Session tracks each caller in the one authorities map. `DropAuthority` marks that generation closed on the entry. Login returns `Ok`. A login while that generation is closed to admission opens a new generation. The queued drop closes only the generation logout queued, and the new generation's orders run.

## Time

1. **Implementation** — 2026-10-20 — moved `Session` outside the core mailbox and covered the logout barrier order.
2. **Drop request** — 2026-10-20 — logout queues `CoreQueue.DropAuthority`, and the core removes the authority when it processes that case.
3. **Reply** — 2026-10-20 — the logout route returns when the drop is queued.
4. **Late order** — 2026-10-20 — the core rejects an order that arrives after `DropAuthority`.
5. **One generation map** — 2026-10-20 — login during a queued logout opens a new generation, and the drop closes only the generation logout queued.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [GLOSSARY](../../../GLOSSARY.md)
