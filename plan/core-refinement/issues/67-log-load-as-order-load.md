# 67 — Log Load as Order.Load

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`CoreMsg.LoadNode` is the parse-stack Load. `CoreMailboxLoad.dispatchLoad` checks that the subject is a Workspace, Directory, or File, then pushes that node. It does not append an `Ev` and it does not post a node notice. `CoreDoor.loadNode` and `GithubTransportActor` call `CoreMailboxApi.mbLoadNode`. Decision 2 **Load is logged** in [Message shape](../cqrs-messages.md): `Order.Load` is logged, and its effects follow as ordinary entries. The Load order itself posts no node notice. `NodeParsing` stays a later entry from the parse pull in [52 — Log stack pulls](52-log-stack-pulls.md). Upload, import, parse, Load, undo, and persist stay working.

## What to build

Replace `LoadNode` with `Order.Load`. Log that order. Keep the parse push. Post no notice from this handler.

### 1. Change

1. [ ] **Order** — `CoreMailboxApi.mbLoadNode` and `CoreDoor.loadNode` place `Order.Load` of the subject `NodeId`. Remove `CoreMsg.LoadNode`.
2. [ ] **Log** — Accept appends one `Ev` whose order is `Order.Load` of that node. The `PlaceOrder` reply is `Ok` of that `EventId`.
3. [ ] **Push** — The handler still rejects a subject that is not a Workspace, Directory, or File. On success it pushes the subject on the parse stack. It posts no `Notice`.
4. [ ] **Follow-on** — This handler does not post `NodeParsing`, a `Change`, or `NodeParsed`. The parse thread still posts its Changes and `Notice.NodeParsed`. `NodeParsing` stays the pull post in [52 — Log stack pulls](52-log-stack-pulls.md).

### Acceptance Criteria

1. [ ] **Logged** — `loadNode` of a File replies `Ok` of a new `EventId`. The log at that id is `Order.Load` of that file.
2. [ ] **Push** — That file is on the parse stack after the reply.
3. [ ] **No notice** — The Load handler posts no `Notice`. A Load that parses still reaches `Notice.NodeParsed` from the parse thread.
4. [ ] **Case** — `CoreMsg` has no `LoadNode`.
5. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [52 — Log stack pulls](52-log-stack-pulls.md)
- [21 — Contract Load onto the Parse stack](21-contract-load-onto-the-parse-stack.md)
- [GLOSSARY](../../../GLOSSARY.md)
