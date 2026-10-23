# 67 — Log Load as Order.Load

**Status:** `coded`
**Type:** coding
**Blocked by:** 60 — Add Mailbox beside CoreMsg
**Trackers:** architecture

## Context

`CoreMsg.LoadNode` is the parse-stack Load. `CoreMailboxLoad.dispatchLoad` checks that the subject is a Workspace, Directory, or File, then pushes that node on the parse stack. It appends no `Ev`. `CoreDoor.loadNode`, `GithubTransportActor`, and `DeskLoadPush.runLoadSave` call it. `Order.Load of nodeId: NodeId` is in `src/Shared/MailboxLog.fs`. `MailboxLog.historyBody` has no stored body for it.

This ticket is decision 2 **Load is logged** and decision 19 **One case each** in [Message shape](../cqrs-messages.md).

## What to build

Do these steps in order. Finish each step before starting the next.

1. [x] **Stored body** — Add `EventBody.Load of nodeId: NodeId`. `EventJson` writes and reads it, `MailboxLog.ofEv` and `MailboxLog.toEv` map it to `Order.Load` both ways, and Undo skips it beside `EventBody.ActorStart` and `EventBody.Cancel`. Add `Load keeps its node through EventJson and MailboxLog`. Files: `src/Shared/History.fs` (where `EventBody` is defined, and the Undo skip), `src/Shared/EventJson.fs`, `src/Shared/MailboxLog.fs` (where `Order.Load` is defined), `tests/Shared.Tests/MailboxLogTests.fs`. Done when `Load keeps its node through EventJson and MailboxLog` passes.
2. [x] **Place and handle** — `CoreMailboxApi.mbLoadNode` places `Order.Load` of the subject through `Mailbox.PlaceOrder`. The `PlaceOrder` arm of `CoreMailboxLoad.dispatchMailbox` for `Order.Load` runs `dispatchLoad`; on success it appends one `Ev` of `EventBody.Load` of the subject and replies `Ok` of that `EventId`, and a subject that is not a Workspace, Directory, or File replies `Error`. `DeskLoadPush.runLoadSave` keeps calling `dispatchLoad` in the same mailbox turn. Add acceptance tests 1, 2 and 4. Files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `tests/Server.Tests/MailboxExpandTests.fs`. Done when `loadNode logs Order.Load`, `loadNode pushes the subject` and `loadNode of a plain Node replies Error` pass.
3. [x] **Remove** — Remove `CoreMsg.LoadNode` and its arms. Files: `src/Server/Core/CoreMsg.fs` (where `CoreMsg` is defined), `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreMailboxEvents.fs`. Done when it compiles, `MailboxExpandTests` pass, and no file under `src/` or `tests/` names `LoadNode (`.

### Acceptance Criteria

1. [x] **Logged** — `loadNode logs Order.Load`. `loadNode` of a File replies `Ok` of a new `EventId`, and the log at that id is `Load` of that file.
2. [x] **Push** — `loadNode pushes the subject`. That file is on the parse stack after the reply, and parsing it reaches `Notice.NodeParsed`.
3. [x] **Round trip** — `Load keeps its node through EventJson and MailboxLog`.
4. [x] **Rejected** — `loadNode of a plain Node replies Error`. The log has no new entry.
5. [x] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

## Amendments

1. **Actor result** — `GithubTransportActor.finishLoadPull` maps the `EventId` reply to `unit`. git Load still returns `Ok ()`.
2. **Undo** — `Ev.ops` and `Ev.target` return `None` for `EventBody.Load`, beside `ActorStart` and `Cancel`. Undo skips that body. A client `PostEvent` of `EventBody.Load` is rejected as mailbox-generated.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [60 — Add Mailbox beside CoreMsg](60-add-mailbox-beside-coremsg.md)
- [52 — Log stack pulls](52-log-stack-pulls.md)
- [21 — Contract Load onto the Parse stack](21-contract-load-onto-the-parse-stack.md)
- [GLOSSARY](../../../GLOSSARY.md)
