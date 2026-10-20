# 61 — Move Session off the core mailbox

**Status:** `defined`
**Type:** coding
**Blocked by:** 63 — Move queries to Ask; 62 — Admit at the entry
**Trackers:** architecture

## Context

`Login`, `Logout`, and `AdmitCaller` are `CoreMsg` cases. `CoreMailboxLoad.runMsg` handles them on the core queue. `RouteRegistration` calls `CoreMailboxApi.isAdmitted` before a route body. `GET /ambit/logout` calls `CoreMailboxApi.logout` and removes the caller on that call. Decision 8 **Session leaves** in [Message shape](../cqrs-messages.md) places login, admit, and logout on module `Session`, outside the core mailbox. `Session.logout` stops admitting the caller, awaits `Ask` of `Query.LastEventId` through the mailbox door, then removes the authority. `Session.login` records the caller and returns. `Session.admit` returns whether that caller is logged in. The barrier uses `Query.LastEventId` from [63 — Move queries to Ask](63-move-queries-to-ask.md). [62 — Admit at the entry](62-admit-at-the-entry.md) places admission on the route and on Actor start before this ticket. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Move login, admit, and logout onto module `Session`. `Session` sits outside `CoreQueue`. The credential set moves off `MailboxContext` onto `Session`.

### 1. Change

1. [ ] **Session** — Add `src/Server/Core/Session.fs` with module `Session`. `Session.login` is `(name: string) -> (secret: Credential) -> Async<Result<unit, string>>`. `Session.admit` is `(caller: Caller) -> Async<bool>`. `Session.logout` is `(host: MailboxHost) -> (caller: Caller) -> Async<Result<unit, string>>`.
2. [ ] **login** — `RouteAuthentication.loginThenSetCookie` calls `Session.login`. `Session.login` records `Authority "Browser"` for that name and secret and returns `Ok ()`. `CoreMailboxApi.login` calls `Session.login`.
3. [ ] **admit** — `RouteRegistration` calls `Session.admit` before a route body places an order. `Session.admit` returns true when that caller is logged in. `CoreMailboxApi.isAdmitted` calls `Session.admit`.
4. [ ] **logout** — `RouteRegistration` on `GET /ambit/logout` calls `Session.logout`. `Session` stops admitting that caller, so `Session.admit` returns false and `RouteRegistration` refuses a new order from that authority. `Session` awaits `Ask` of `Query.LastEventId` through the mailbox door `CoreMailboxApi.getEventId`. `Session` removes that authority. `CoreMailboxApi.logout` calls `Session.logout`.
5. [ ] **CoreMsg** — Remove `Login`, `Logout`, and `AdmitCaller` from `CoreMsg` and from `CoreMailboxLoad.runMsg`.

### Acceptance Criteria

1. [ ] **Cases** — A login test calls `Session.login`. `Login`, `Logout`, and `AdmitCaller` are `Session` calls.
2. [ ] **Barrier** — Post an order, then call `Session.logout`. `Session.admit` returns false for that caller before `Query.LastEventId` replies. The order posted before the `Ask` is applied before `Session` removes the authority. The `Ask` reply is the current event id.
3. [ ] **Login** — `RouteAuthentication.loginThenSetCookie` sets the cookie after `Session.login` returns `Ok ()`. `Session.admit` returns whether that caller is logged in.
4. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

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
