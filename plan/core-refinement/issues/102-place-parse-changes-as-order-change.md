# 102 — Place Parse changes as Order.Change

**Status:** `defined`
**Type:** coding
**Blocked by:** 64 — Place Edit changes as Order.Change
**Trackers:** architecture

## Context

[64 — Place Edit changes as Order.Change](64-merge-postevent-and-postgraphonly-into-change.md) adds `ChangeOrigin` and places origin `Edit` changes through `Mailbox.PlaceOrder`. The parse posters still use `CoreMsg.PostGraphOnly` through `CoreMailboxApi.postGraphOnly`:

- **Parse ops** — `ParseThread.postParseOps`
- **Parse file** — `Api.applyParseFile`
- **Load** — `LazyLoadReconciliationServer` through `GraphOnlyChangePost.postChunks`

Decision 4 **One Change** and decision 18 **ChangeOrigin** in [Message shape](../cqrs-messages.md): those callers place `Order.Change` with origin `Parse`. Origin `Parse` keeps the file skip `PostGraphOnly` uses today. Upload, import, parse, Load, undo, and persist stay working.

## What to build

1. [ ] **Parse door** — `CoreMailboxApi.postGraphOnly` places `Mailbox.PlaceOrder` of `Order.Change` with origin `Parse`. The three parse posters in Context keep calling it. The reply is `Result<EventId, string>` like 64's.
2. [ ] **File skip** — `CoreEventDispatch` skips document files and the persist push for origin `Parse`, the way `PostGraphOnly` does today. The log entry is the `Change`.
3. [ ] **Remove the old door** — Remove `CoreMsg.PostGraphOnly` and `CoreMailboxEvents.dispatchPostGraphOnly`.

### Acceptance Criteria

1. [ ] **Parse order** — `postGraphOnly places Order.Change with origin Parse`. The reply is `Ok` of the new `EventId`. `CoreMsg.PostGraphOnly` is gone.
2. [ ] **Parse** — `postParseOps skips the document file`. `ParseThread.postParseOps` places `Order.Change` with origin `Parse` and completes. No document file is written. The logged Change holds the op list.
3. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [64 — Place Edit changes as Order.Change](64-merge-postevent-and-postgraphonly-into-change.md)
- [65 — Place Undo and Redo as orders](65-undo-and-redo-resolve-their-ops-from-the-target.md)
- [GLOSSARY](../../../GLOSSARY.md)
