# 59 — Rename InMsg to Notice

**Status:** `done`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

`InMsg` in [CoreMsg.fs](../../../src/Server/Core/CoreMsg.fs) is the internal message on the one mailbox queue. The cases are `NodeParsed`, `SnapshotDone` of `nodeId`, `MarkUnparsed`, `NodeUploading`, and `NodeReceived`. `QueueSum` is `Core of CoreMsg | In of InMsg`. `CoreMailboxApi.addInMsg` posts the `In` case. `CoreMailbox.hostPublishing` passes that function to `PersistFilling.bindPersistFinished`. This ticket renames those names. It does not add `Mailbox`, `Order`, or `Query`. [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md) does that. Upload, import, parse, Load, undo, and persist stay working.

## What to build

Rename `InMsg` to `Notice` in place. The cases keep their names and payloads. `QueueSum` becomes the internal queue `CoreQueue`. `CoreMailboxApi.addInMsg` becomes the notice function `postNotice`.

### 1. Expand

1. [ ] **Notice** — Rename `InMsg` to `Notice` in `CoreMsg.fs`. It stays server-internal. The cases stay `NodeParsed` of `nodeId`, `SnapshotDone` of `nodeId`, `MarkUnparsed` of `nodeId`, `NodeUploading` of `nodeId`, and `NodeReceived` of `nodeId`. `SnapshotDone` carries the node id.
2. [ ] **CoreQueue** — Replace `QueueSum` with `type internal CoreQueue = Public of CoreMsg | Notice of Notice`. `Public` holds today's `CoreMsg`. [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md) changes that payload to `Mailbox`.
3. [ ] **Notice function** — Rename `CoreMailboxApi.addInMsg` to `CoreMailboxApi.postNotice`. `postNotice` posts `CoreQueue.Notice`. `addCoreMsg` posts `CoreQueue.Public`. `PersistFilling.bindPersistFinished` takes `Notice -> unit`. `CoreMailbox.hostPublishing` passes `postNotice`.
4. [ ] **Handler** — Rename `CoreMailboxLoad.applyInMsg` to `applyNotice`. `dispatchItem` matches `CoreQueue`. `SnapshotDone` still calls `noteDiffDone` and sets `PersistState.Persisted`. `NodeParsed` still sets Parsed when `pullingFile` is clear. Rename `MailboxHost.inMsgLog` to `noticeLog` and `loggedInMsgs` to `loggedNotices`.

### Acceptance Criteria

1. [x] **Names** — The server has `Notice`, `CoreQueue`, and `postNotice`. It has no `InMsg`, `QueueSum`, or `addInMsg`. `Notice` has the five cases in item 1. `SnapshotDone` is `nodeId`.
2. [x] **Same effects** — `applyNotice` on `Notice.NodeParsed` sets Parsed when `pullingFile` is clear. `applyNotice` on `Notice.SnapshotDone` calls `noteDiffDone` and sets that node `PersistState.Persisted`.
3. [x] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [58 — Remove dead snapshot code](58-remove-dead-snapshot-code.md)
- [GLOSSARY](../../../GLOSSARY.md)
