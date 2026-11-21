# 119 — Add Notice.NodeUnpersisted

Ticket: [[../issues/119-add-notice-nodeunpersisted.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Ticket links leave the project root** — Hard. `.agents/rules/writing-markdown.md` Links: a file outside the same directory uses a path relative to the project root. [119 — Add Notice.NodeUnpersisted](../issues/119-add-notice-nodeunpersisted.md) links `../cqrs-messages.md`, `../../../src/Shared/History.fs`, `../../../src/Server/Core/CoreEventDispatch.fs`, `../../../src/Server/Core/CoreMailboxLoop.fs`, `../../../src/Shared/EventJson.fs`, and `../../../src/Server/Database.fs`.
- 2. **Problem names three bodies in prose** — Hard. `.agents/rules/writing-markdown.md` Lists: a list of things is an unnumbered list. The Problem paragraph names Change, Undo, and Redo in one sentence.

## 2. Spec

- No findings.

## 3. Arch

- 1. **Change handler skips the notice door** — Blocker. Arch Graph change contract, and the note under Graph change arrival, say the server's Change handler posts `Notice.NodeUnpersisted` after an ordinary edit, and Decision 15 **Notice has its own door** names `CoreMailboxApi.postNotice` as that function. `CoreEventDispatch.markForPersist` calls `logNotice` and then `writeAxis`. `persistNew` runs that mark after `store`. No call to `CoreMailboxApi.postNotice` was added.

## 4. Tests

- 1. **Full suite** — `bash scripts/check-test.sh 16586` printed `DONE` and `/tmp/ambit-full-suite-20261027-172027.log`. The log printed `full suite finished`. Server.Tests passed 721, failed 0, skipped 1 (`DocumentPersistenceTests.upload structure persistence preserves existing file mtime`), total 722, duration 3 m 14 s. Shared.Tests passed 1904, failed 0, skipped 3 (`WorkspaceUploadStructureTests.reuses existing owned path without duplicating`, `WorkspaceUploadStructureTests.planServerFilePresentOps transitions only matching absent files`, `WorkspaceLocalInventoryTests.listForPush without git still returns walk`), total 1907, duration 28 s.
- 2. **applyNotice NodeUnpersisted marks Unpersisted** — `tests/Server.Tests/CoreLoopNoticeTests.fs` calls `CoreMailboxLoop.applyNotice` with `Notice.NodeUnpersisted` and expects `PersistState.Unpersisted`, the same document state as `Graph.setPersistState`, and an unchanged parse state. It passed inside the server run.
- 3. **an edit logs NodeUnpersisted for its owning special** — `tests/Server.Tests/CoreMailboxDoorTests.fs` posts `editFileText` through `CoreMailboxApi.postEvents`. It reverses `getEventsSince` and expects `EventBody.Notice (Notice.NodeUnpersisted fileId)` after the Change, and no `Notice.NodePersisting`. It passed inside the server run.
- 4. **a graph-only change logs no NodeUnpersisted** — `tests/Server.Tests/CoreMailboxDoorTests.fs` posts that same edit through `CoreMailboxApi.postGraphOnly` and expects the `replaceGraph` notice list to be empty. It passed inside the server run.
- 5. **Notice JSON round trip** — `tests/Shared.Tests/EventJsonTests.fs` includes `Notice.NodeUnpersisted` in the notice list and round-trips each notice. It passed inside the shared run.
- 6. **NodeUnpersisted writes persist state to the database** — `tests/Server.Tests/DbAgentNoticeTests.fs` applies `Notice.NodeUnpersisted` and expects the database row `persistState` to be Unpersisted, with parse state and update time unchanged. It passed inside the server run.

## 5. Miss

- 1. **Ticket links leave the project root** — Hard. In [119 — Add Notice.NodeUnpersisted](../issues/119-add-notice-nodeunpersisted.md), point each source link at a project-root path, and point the message-shape link at `plan/core-refinement/cqrs-messages.md`.
- 2. **Problem names three bodies in prose** — Hard. In that same Problem paragraph, list Change, Undo, and Redo as items.
- 3. **Change handler skips the notice door** — Blocker. Arch Graph change contract says the Change handler posts `Notice.NodeUnpersisted` through the private notice function. `CoreEventDispatch.markForPersist` calls `logNotice` instead.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- 1. **Notice table and 120 — Ordinary edit posts NodeUnpersisted** — Accept. A later document step writes the Fix into [cqrs-messages](../cqrs-messages.md) and [120 — Ordinary edit posts NodeUnpersisted](../issues/120-ordinary-edit-posts-nodeunpersisted.md).
  - **Problem.** [cqrs-messages](../cqrs-messages.md) still lists `Notice.NodeUnpersisted` as an open decision. [120 — Ordinary edit posts NodeUnpersisted](../issues/120-ordinary-edit-posts-nodeunpersisted.md) still says `markForPersist` logs `Notice.NodePersisting`, and that 120 posts `Notice.NodeUnpersisted` through `postNotice`.
  - **Fix.** Reconcile the Notice table and 120 — Ordinary edit posts NodeUnpersisted so they name one poster.
  - **Reason.** `markForPersist` now appends `Notice.NodeUnpersisted` through `logNotice`. Those two documents still describe a different poster.
