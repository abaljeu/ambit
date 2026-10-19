# 62 — Admit at the entry

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

`CoreEventDispatch.postEvent` calls `context.admit`, and that function is `CoreMailboxEvents.admitCaller`. The same check runs in `CoreMailboxActors.prepareActorStart`, `dispatchActorStop`, `runCancel`, `dispatchRecordSearchStart`, `dispatchRecordSearchStop`, and `CoreMailboxLoad.dispatchLoad`. `RouteRegistration.withBrowserChanges` already calls `CoreMailboxApi.isAdmitted` before the route body. Decision 9 **Admission at the entry** in [Message shape](../cqrs-messages.md): the route checks a logged-in user before `PlaceOrder`, an Actor is admitted when it starts, and the core reads authority. `admitCaller` inside `postEvent` is the call that leaves. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Check a logged-in user at the route, and admit an Actor in the start handler. `postEvent` reads `caller.authority` and does not admit.

### 1. Change

1. [ ] **Route** — `RouteRegistration.withBrowserChanges` checks that the cookie caller is logged in before the route body places an order. A missing cookie gets 401. A caller who is not logged in gets 401. The check stays in front of today's `PostEvent` and `PostGraphOnly`. When [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md) adds `PlaceOrder`, this same check stays in front of that post.
2. [ ] **Actor** — `CoreMailboxActors.prepareActorStart` admits `Authority "Actor"` when the start runs, using the pool live check `admitCaller` uses today. A later post from that Actor does not admit again.
3. [ ] **Core** — Remove `admitCaller` from `CoreEventDispatch.postEvent`. `prepare` still copies `caller.authority` onto the stored event. Remove `admitCaller` from `dispatchActorStop`, `runCancel`, `dispatchRecordSearchStart`, `dispatchRecordSearchStop`, and `dispatchLoad`. Those handlers read the caller on the message.

### Acceptance Criteria

1. [ ] **Route** — `withBrowserChanges` returns 401 for a missing cookie and for a caller who is not logged in. It does not call `postEvent` on that path.
2. [ ] **postEvent** — `CoreEventDispatch.postEvent` has no `admitCaller` call. A direct post stores the event and the stored `authority` is the caller's `authority`.
3. [ ] **Actor start** — `prepareActorStart` rejects an Actor whose secret is not live. After a live start, `postEvent` from that Actor stores the event and does not call `admitCaller`.
4. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [61 — Move Session off the core mailbox](61-move-session-off-the-core-mailbox.md)
- [64 — Merge PostEvent and PostGraphOnly into Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [GLOSSARY](../../../GLOSSARY.md)
