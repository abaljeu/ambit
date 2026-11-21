# 98 — Core marks carry the Edit's id

Ticket: [[../issues/98-core-marks-carry-the-edits-id.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Finish-report bare id** — Hard. `.agents/rules/refer-by-name.md`: never identify anything only by number; always include a name. `python scripts/standards-scan.py` exited 1. The printed `BARE_ID` lines are `plan/core-refinement/reports/98-core-marks-carry-the-edits-id-finish.md` line 16 and line 73, quoting the id without the name `98 — Core marks carry the Edit's id`. The same scan printed `needsUnpersistedMark` at 5 lines and `writeUnpersisted` at 9 lines. `markForPersist` is 18 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.
- 2. **Duplicated apply bind** — Judgement. Smell Duplicated Code. `FileAgent.applyOne` and `DbAgent.applyOneEvent` both bind `applyForGraphOnly` / `applyForCommand` and pass `event.id`.

```fsharp
let apply =
    if graphOnly then ChangeAmendment.applyForGraphOnly
    else ChangeAmendment.applyForCommand
let result, amended, appliedOps =
    apply event.id event.commandName ops s
```

## 2. Spec

- 1. **Undo and Redo version** — Extra. `Op.persistMarkVersion` returns `Some eventId` for every command except `"Parse"` (`src/Shared/History.fs` 263-269). `FileAgent.applyOne` and `DbAgent.applyOneEvent` pass `event.id`. An Undo or Redo event carries the inverse ops and the original `commandName`, so that apply writes `changedVersion` at the Undo or Redo event id. Spec §2.6 **Undo, Redo, and stored replay** and [47 — Undo and redo set Unpersisted](../issues/47-undo-and-redo-set-unpersisted.md) say Undo and Redo do not write a version. The ticket Changes section tells the implementer to pass `event.id` through `Op.marksOwningPersist`.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-215539.log`. `scripts/check-test.sh 45263` printed `DONE` and that log path. The log printed `full suite finished`. Server.Tests passed 750, failed 0, skipped 0, total 750, duration 3 m 26 s. Shared.Tests passed 1932, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`), total 1933, duration 6 s.
- 2. **Named door test** — `An Edit marks its owning special node at the Edit's id` is in `tests/Server.Tests/CoreMailboxDoorTests.fs` 814-846. It passed inside the server run. The host is `hostRecordingGraph`. `acceptPosted` returns `CoreChanges.accepted`. `replaceGraph` stores the graph `markForPersist` writes. Two posts of the same File Node's text leave that node `Unpersisted` with `changedVersion` at the second accepted id, through `completed.id`. `Node.Create` starts `Persisted`, so the first post marks, and the second post marks again because `changedVersion` is older.
- 3. **Apply writer** — `FileAgent applyEvent marks a File Node at the later Edit id` in `tests/Server.Tests/PersistApplyTests.fs` 58-82 passed inside the server run. Two Edits of the same File Node's text go through `handlers.applyEvent`, which is `FileAgent.applyOne`. The node is `Unpersisted`, `changedVersion` is stored id 5, and the text is `second`. When apply has already set `Unpersisted` at that same id, `needsUnpersistedMark` is false and `markForPersist` returns `Ok ()`.
- 4. **Push test** — `An Edit pushes its owning special node` records that File Node. It passed inside the server run.
- 5. **Parse callers** — `LazyLoadReconciliationTests`, `DirectoryReconcileTests`, and `SpecialNodeStateAxesTests` pass `EventId.zero` into `applyForCommand` / `applyForGraphOnly` with `"Parse"`. `Op.persistMarkVersion "Parse"` is `None`. They passed inside Shared.Tests.
- 6. **Notice handlers** — `NodePersisting leaves the Edit's changedVersion` and `NodeUploading leaves the Edit's changedVersion` in `tests/Server.Tests/CoreLoopNoticeTests.fs` start with `changedVersion` at Edit id 5 and `state.eventId` at 12. After `CoreMailboxLoad.applyNotice`, `changedVersion` is still 5 and persist state is `Unpersisted`. They passed inside the server run. `writeUnpersisted` calls `GraphMutate.setPersistState` with `PersistState.Unpersisted`. `Node.withPersistState` leaves `changedVersion` as it is.
- 7. **loadNode log** — `loadNode of a plain Node replies Error` in `tests/Server.Tests/MailboxExpandTests.fs` asserts the rejected `loadNode` left no `EventBody.Load`. It passed inside the server run.

## 5. Miss

- 1. **Finish-report bare id** — Hard. In `plan/core-refinement/reports/98-core-marks-carry-the-edits-id-finish.md`, write the name with the id on line 16 and line 73: `98 — Core marks carry the Edit's id`.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- 1. **Notice stamp in the ticket Changes section** — Accept. The document step writes the Fix into [98 — Core marks carry the Edit's id](../issues/98-core-marks-carry-the-edits-id.md).
  - **Problem.** The Changes section says `CoreMailboxLoad.writePullingFileUnpersisted` and the `Notice.NodePersisting` arm write through `GraphMutate.setUnpersistedAt` at the `eventId` from `persist.getState ()`. `CoreMailboxLoad.writeUnpersisted` writes `PersistState.Unpersisted` through `GraphMutate.setPersistState`. The Look at bullet already names `GraphMutate.setPersistState`.
  - **Fix.** Replace that Changes bullet with:
    - `CoreMailboxLoad.writeUnpersisted` writes `PersistState.Unpersisted` through `GraphMutate.setPersistState`.
    - `writePullingFileUnpersisted` (`Notice.NodeUploading`) and the `Notice.NodePersisting` arm call it.
    - `changedVersion` stays the Edit id already on the node.
- 2. **Undo and Redo stamp** — Reject. `plan/core-refinement/spec.md` and `plan/core-refinement/arch.md` stay as they are.
  - **Problem.** Undo and Redo stamp `event.id` through `Op.persistMarkVersion`. The finish report lists that stamp as a leftover risk beside the spec and the arch.
  - **Fix.** Rewrite spec §2.6 **Undo, Redo, and stored replay** and arch **Undo** (Graph change undo) so they record that stamp.
  - **Reason.** Spec §2.6 **Undo, Redo, and stored replay** says Undo and Redo do not write a version. Arch **Undo** says neither writes a version. The stamp stays Spec Extra **Undo and Redo version** in this review. `Op.persistMarkVersion` in `src/Shared/History.fs` returns `Some eventId` for every command except `"Parse"`.
- 3. **Context markdown** — Reject. These files stay as they are.
  - **Problem.** The working tree carries edits in these files beside [98 — Core marks carry the Edit's id](../issues/98-core-marks-carry-the-edits-id.md):
    - `context/build-context.md`
    - `context/document-context.md`
    - `context/review-context.md`
  - **Fix.** Fold those edits into the ticket.
  - **Reason.** The three files sit outside [98 — Core marks carry the Edit's id](../issues/98-core-marks-carry-the-edits-id.md).
- 4. **Uncommitted files** — Reject. These files stay uncommitted.
  - **Problem.** These files sit uncommitted with the code:
    - [98 — Core marks carry the Edit's id](../issues/98-core-marks-carry-the-edits-id.md)
    - `plan/core-refinement/review/98-core-marks-carry-the-edits-id.md`
    - `plan/core-refinement/reports/98-core-marks-carry-the-edits-id-finish.md`
    - `tests/Server.Tests/MailboxExpandTests.fs`
  - **Fix.** Commit them with the ticket and push.
  - **Reason.** Desktop leaves the commit to the user.
