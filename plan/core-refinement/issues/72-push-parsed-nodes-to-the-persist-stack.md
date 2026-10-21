# 72 — Push parsed nodes to the persist stack

**Status:** `defined`
**Type:** coding
**Blocked by:** [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
**Trackers:** architecture

## Context

Part 3 of [51 — Stand the persist stack](51-stand-the-persist-stack.md). `Notice.NodeParsed` calls each agent's `noteParsed`. `FileAgent.scheduleCatchUp` and `DbAgent.catchUpParsed` check that the node is Parsed and Unpersisted, then push its id onto the persist request stack.

This ticket moves the Parsed and Unpersisted check into the mailbox. Each agent supplies only the push.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Push** — Add `pushPersist: NodeId -> unit` to `PersistHandlers`. In the `Notice.NodeParsed` arm of `CoreMailboxLoad.applyNotice`, after Parsed is written, read the node from `persist.getState ()` and call `persist.pushPersist nodeId` when it is Parsed and Unpersisted; the mailbox does not wait. `FileAgent` and `DbAgent` build `pushPersist` as `PersistRequestStack.push` of `{ nodeId = nodeId }` onto the stack their persist thread takes from, and `DbAgent` pushes whether or not live save has a data directory; the test handlers set it to `ignore` or a recording list. Files: `src/Server/Core/CoreMsg.fs` (where `PersistHandlers` is defined), `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `tests/Server.Tests/TestBackend.fs`, `tests/Server.Tests/CoreLoopNoticeTests.fs` (add `NodeParsed pushes a persist request`). Done when `NodeParsed pushes a persist request` passes.
2. [ ] **Remove** — Delete `FileAgent.scheduleCatchUp` and set the `FileAgent` `noteParsed` handler to `ignore`. Delete `DbAgent.catchUpParsed` and its call in `DbAgent.noteParsed`; `DbAgent.noteParsed` keeps its file-diff request. Add the second acceptance test, which runs a NodeParsed through the persist thread and checks the node is Persisted. Files: `src/Server/Core/FileAgent.fs`, `src/Server/Core/DbAgent.fs`, `tests/Server.Tests/PersistThreadTests.fs`. Done when the second acceptance test passes and no file under `src/` or `tests/` names `scheduleCatchUp` or `catchUpParsed`.

### Acceptance Criteria

1. [ ] **Test** — `NodeParsed pushes a persist request`. A Parsed and Unpersisted node is pushed. Any other node is not pushed.
2. [ ] **Test** — after the loop writes the node, it is Persisted.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.

## See also

- [51 — Stand the persist stack](51-stand-the-persist-stack.md)
- [71 — Persist thread takes from the stack](71-persist-thread-takes-from-the-stack.md)
