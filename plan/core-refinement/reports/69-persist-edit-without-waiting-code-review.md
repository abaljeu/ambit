# 69 — Persist an Edit without waiting

Review of the working diff against [69 — Persist an Edit without waiting](../issues/69-persist-an-edit-without-waiting.md).

## Standards

The standards scan listed the new functions. Each is under 40 lines. `History.fs` is 755 lines after `PersistStamp` moved out. `Notice` moved into `History.fs` so `NodeStamped` can carry `Op list`. `Op` and its module stay in one file.

No string match on `commandName` or `Authority`. The browser keys off `EventBody.Notice`.

## Spec

The Edit path pushes a `PersistKind.Ops` submit and returns. `PersistSubmit` has no `wait`. `PersistOutcome`, `waitReply`, and `PersistWork.reply` are gone.

`PersistThread.afterWrite` posts `Notice.NodeStamped` of the stamp ops and the persist message when that write returns a stamped graph. `CoreMailboxLoad.applyNotice` applies those ops. The queue puller logs the Notice first, as it does for the other Notices.

The ack events are the Edit's own events. `CoreChangesAccepted.message` is `None`. A polled `Notice.PersistFailed` sets `lastCmdResult` to `CmdLastResult.Error (None, message)` and does not change the graph or the persist field.

## Arch

[core-refinement architecture](../arch.md) **NodeStamped** matches this shape. A Notice still does not start a browser catch-up. `SyncLogic.foldProjectedEvents` applies the stamp ops forward and does not rewind.

## Care

`FileAgent.noteStamped` checkpoints the Edit event id after a clean write. A restart that loads documents from that id does not replay the Edit. The undo-after-restart test clears that checkpoint so the log replays. A soft-fail still leaves the checkpoint behind.

`DbAgent` remembers workspaces on the Edit and starts the file diff from `NodeStamped`, after the ops write. It does not write the stamp ops into the database projection on the mailbox. Those times are in memory until a later document load.

The full Shared and Server suite was started and not waited on.
