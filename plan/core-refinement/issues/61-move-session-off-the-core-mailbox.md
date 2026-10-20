# 61 — Move Session off the core mailbox

**Status:** `defined`
**Type:** coding
**Blocked by:** 63 — Move queries to Ask; 62 — Admit at the entry
**Trackers:** architecture

## Context

`Login`, `Logout`, and `AdmitCaller` are `CoreMsg` cases. `CoreMailboxLoad.runMsg` handles them on the core queue. `RouteRegistration` calls `CoreMailboxApi.isAdmitted` before a route body, and `GET /ambit/logout` calls `CoreMailboxApi.logout`, which removes the caller immediately. Decision 8 **Session leaves** in [Message shape](../cqrs-messages.md): these three leave the core mailbox. Logout stops admitting the caller, awaits an `Ask` so earlier orders drain, then removes the authority. `Login` and `Admit` have no barrier. The barrier uses `Query.LastEventId` from [63 — Move queries to Ask](63-move-queries-to-ask.md). This ticket is blocked by that ticket and by [62 — Admit at the entry](62-admit-at-the-entry.md). Upload, import, parse, Load, undo, and persist stay working.

## What to build

Move login, logout, and admit onto a `Session` component outside `CoreQueue`. `Session` owns whether an authority may start a route and whether that authority still exists. The logout route coordinates the mailbox barrier; `Session` does not join the core queue.

### 1. Session API

1. [ ] **Login** — `Login caller` registers the caller's authority and opens admission for it. Login does not await an `Ask`.
2. [ ] **Admit** — `Admit caller` checks whether the caller and authority are currently allowed to start a route. It does not add authority, revoke authority, remove authority, or await an `Ask`.
3. [ ] **Stop admission** — The first logout phase closes admission for that caller. After this phase takes effect, no later admission check for the caller succeeds. Closing admission keeps the authority available to orders that already passed admission.
4. [ ] **Remove authority** — The final logout phase removes the caller's authority. It runs only after the mailbox barrier replies. Removal is distinct from closing admission and does not reopen admission.
5. [ ] **Representation** — Keep these four effects at the `Session` boundary. The implementation may use messages, functions, or an equivalent private representation. Do not put the credential set or any of these effects on `CoreQueue`.

### 2. Change

1. [ ] **Doors** — `CoreMailboxApi.login`, `logout`, and `isAdmitted` use `Session` and do not post `CoreMsg`. `RouteAuthentication.loginThenSetCookie` still calls login. Remove `Login`, `Logout`, and `AdmitCaller` from `CoreMsg` and from `CoreMailboxLoad.runMsg`.
2. [ ] **Logout sequence** — `RouteRegistration` on `GET /ambit/logout` closes admission for the caller, waits until every route that passed admission before the close has either queued its order or abandoned that post, awaits `Ask` of `Query.LastEventId`, and then removes the authority.
3. [ ] **Queue ordering** — The `Ask` is queued only after admission is closed and the admitted route handoffs have settled. Orders already queued, including an order from a route that won the race with the close, run before the `Ask`. No order from that caller can enter after the `Ask`. The authority remains available while those orders run and is removed only after the reply.
4. [ ] **Synchronization seam** — The successful admission check and the handoff to the order door must be synchronized with closing admission. A scoped admission grant is one valid realization, but this ticket does not prescribe a lock, grant type, processor, or file layout.

### Acceptance Criteria

1. [ ] **Cases** — `CoreMsg` has no `Login`, `Logout`, or `AdmitCaller`. A login test reaches `Session` and does not post those cases on `CoreQueue`.
2. [ ] **Barrier** — Post an order, then log out. `Session` refuses a new route for that caller before `Query.LastEventId` replies. The order posted before the `Ask` is applied before `Session` removes the authority. The `Ask` reply is the current event id.
3. [ ] **Admission race** — Pause a route after it passes admission and before it queues its order, then start logout. Logout closes admission and does not queue the `Ask` until that route queues or abandons its order. A route that checks admission after the close is refused. Authority removal happens after the barrier reply.
4. [ ] **Login** — `RouteAuthentication.loginThenSetCookie` sets the cookie only after Session login returns `Ok ()`. That path does not post an `Ask`.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [63 — Move queries to Ask](63-move-queries-to-ask.md)
- [62 — Admit at the entry](62-admit-at-the-entry.md)
- [GLOSSARY](../../../GLOSSARY.md)
