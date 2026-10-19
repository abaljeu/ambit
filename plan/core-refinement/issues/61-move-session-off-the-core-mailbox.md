# 61 — Move Session off the core mailbox

**Status:** `defined`
**Type:** coding
**Blocked by:** 63 — Move queries to Ask
**Trackers:** architecture

## Context

`Login`, `Logout`, and `AdmitCaller` are `CoreMsg` cases. `CoreMailboxLoad.runMsg` handles them on the core queue. `RouteRegistration` calls `CoreMailboxApi.isAdmitted` before a route body, and `GET /ambit/logout` calls `CoreMailboxApi.logout`, which removes the caller immediately. Decision 8 **Session leaves** in [Message shape](../cqrs-messages.md): these three leave the core mailbox. Logout stops admitting the caller, awaits an `Ask` so earlier orders drain, then removes the authority. `Login` and `Admit` have no barrier. The barrier uses `Query.LastEventId` from [63 — Move queries to Ask](63-move-queries-to-ask.md). Upload, import, parse, Load, undo, and persist stay working.

## What to build

Move login, logout, and admit onto a `Session` component outside `CoreQueue`. `RouteRegistration` keeps the logout barrier in front of authority removal.

### 1. Change

1. [ ] **Session** — Add `src/Server/Core/Session.fs`. `Session` is `Login of caller * AsyncReplyChannel<Result<unit, string>>`, `Logout of caller * AsyncReplyChannel<Result<unit, string>>`, and `Admit of caller * AsyncReplyChannel<bool>`. The credential set moves off `MailboxContext` onto `Session`.
2. [ ] **Doors** — `CoreMailboxApi.login`, `logout`, and `isAdmitted` call `Session`. They do not post `CoreMsg`. `RouteAuthentication.loginThenSetCookie` still calls `login`. Remove `Login`, `Logout`, and `AdmitCaller` from `CoreMsg` and from `CoreMailboxLoad.runMsg`.
3. [ ] **Logout barrier** — `RouteRegistration` on `GET /ambit/logout` tells `Session` to stop admitting that caller, awaits `Ask` of `Query.LastEventId`, then tells `Session` to remove the authority. `Login` and `Admit` do not await an `Ask`.

### Acceptance Criteria

1. [ ] **Cases** — `CoreMsg` has no `Login`, `Logout`, or `AdmitCaller`. A login test reaches `Session` and does not post those cases on `CoreQueue`.
2. [ ] **Barrier** — Post an order, then log out. `Session` refuses a new route for that caller before `Query.LastEventId` replies. The order posted before the `Ask` is applied before `Session` removes the authority. The `Ask` reply is the current event id.
3. [ ] **Login** — `RouteAuthentication.loginThenSetCookie` sets the cookie only after `Session.Login` returns `Ok ()`. That path does not post an `Ask`.
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
