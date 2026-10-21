# 67 — Log Load as Order.Load

**Status:** `defined`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`CoreMsg.LoadNode` is the parse-stack Load. `CoreMailboxLoad.dispatchLoad` checks that the subject is a Workspace, Directory, or File, then pushes that node on the parse stack. It appends no `Ev`. `CoreDoor.loadNode`, `GithubTransportActor`, and `DeskLoadPush.runLoadSave` call it. `Order.Load of nodeId: NodeId` is in `src/Shared/MailboxLog.fs`. `MailboxLog.historyBody` has no stored body for it.

## What to build

Decision 2 **Load is logged** and decision 19 **One case each** in [Message shape](../cqrs-messages.md).

1. [ ] **Stored body** — Add `EventBody.Load of nodeId: NodeId` to `src/Shared/History.fs`. `EventJson` writes and reads it. `MailboxLog.ofEv` and `MailboxLog.toEv` map it to `Order.Load` both ways. Undo skips it.
2. [ ] **Place** — `CoreMailboxApi.mbLoadNode` places `Order.Load` of the subject through `Mailbox.PlaceOrder`.
3. [ ] **Handle** — The `PlaceOrder` arm for `Order.Load` runs `dispatchLoad`. On success it appends one `Ev` of `EventBody.Load` of the subject and replies `Ok` of that `EventId`. A subject that is not a Workspace, Directory, or File replies `Error`.
4. [ ] **Desk Load** — `DeskLoadPush.runLoadSave` keeps calling `dispatchLoad` in the same mailbox turn.
5. [ ] **Remove** — Remove `CoreMsg.LoadNode`.

### Acceptance Criteria

1. [ ] **Logged** — `loadNode logs Order.Load`. `loadNode` of a File replies `Ok` of a new `EventId`, and the log at that id is `Load` of that file.
2. [ ] **Push** — `loadNode pushes the subject`. That file is on the parse stack after the reply, and parsing it reaches `Notice.NodeParsed`.
3. [ ] **Round trip** — `Load keeps its node through EventJson and MailboxLog`.
4. [ ] **Rejected** — `loadNode of a plain Node replies Error`. The log has no new entry.
5. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

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
