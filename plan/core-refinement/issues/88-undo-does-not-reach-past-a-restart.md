# 88 — Undo does not reach past a restart

**Status:** `defined`

## 1. Type
coding

## 2. Blockers
- [82 — The mailbox pushes an Edit's special nodes](82-the-mailbox-pushes-an-edits-special-nodes.md). Both change `CoreEventDispatch.fs`.

## 3. Look at
- Files:
  - `src/Server/Core/CoreEventDispatch.fs`
  - `src/Server/Core/CoreMailboxEvents.fs`
  - `src/Server/Core/CoreMailboxLoad.fs`
  - `src/Shared/ClientHistory.fs` (how the browser picks an Undo target)
  - `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
  - `tests/Server.Tests/Issue41CoreMailboxTests.fs`
- Types:
  - `CoreEventDispatch.Context`
  - `CoreMailboxEvents.MailboxContext`
  - `EventBody.Undo`, `EventBody.Redo`
- Functions:
  - `CoreEventDispatch.completeAction`, `CoreEventDispatch.prepare`
  - `CoreMailboxEvents.eventDispatchContext`
  - `CoreMailboxLoad.makeMailBox`, `CoreMailboxLoad.seedEventLog`
  - `EventLog.tip`, `EventLog.tryFind`
  - `ClientHistory.resolveTarget`, `ClientHistory.stampBody`

## 4. Context
Every Undo and Redo reaches the mailbox as an `EventBody.Undo(target, ops)` or `EventBody.Redo(target, ops)` and passes `CoreEventDispatch.prepare`, which calls `completeAction`.

- A name-only Undo or Redo (`ops = []`) looks up `target` in the mailbox event log with `EventLog.tryFind` and fills the inverse ops.
- An Undo or Redo with ops passes unchanged. The browser sends this form. `ClientHistory.resolveTarget` sets `target` from the events this browser has seen confirmed, or `EventId.zero` when none matches.
- At startup `CoreMailboxLoad.makeMailBox` seeds the mailbox event log from `persist.getEventLog`. `DbAgent` and `FileAgent` both keep their log across a restart. So after a restart an Undo can target an event from before it.
- In production only `DbAgent` takes an Undo. A File-mode server makes its browser door read-only (`RouteRegistration.isWritable`), and no Actor posts an Undo.

## 5. Changes
The rule: after a restart, Undo history begins at the startup tip. An Undo or Redo whose target is an event from before the restart is refused.

- `MailboxContext` gains `restartTip: EventId`. `makeMailBox` sets it to `EventLog.tip` of the seeded log, or `EventId.zero` when the seed fails.
- `CoreEventDispatch.Context` gains `restartTip`. `CoreMailboxEvents.eventDispatchContext` copies it.
- `completeAction` takes the `Context`. For `EventBody.Undo(target, _)` and `EventBody.Redo(target, _)`, with or without ops, when `target` is not `EventId.zero` and `EventId.value target <= EventId.value restartTip`, it returns `Error "target Event is from before the restart"`. `prepare` is its only caller.
- The rule is in the shared mailbox, so it holds for `DbAgent` and `FileAgent` alike.

## 6. Tests
- Change `Undo after File restart fills Change and rejects ActorStart` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` and rename it `Undo after restart refuses a target from before the restart`. It runs the mixed hello on `hostDb`, disposes it, and starts a second `hostDb` on the same database. A name-only Undo of the hello Change returns `Error` with `before the restart`. A Redo with ops naming that Change also returns that `Error`. It does not read or write the meta file.
- Add `Undo after restart accepts a target after the restart` in the same file. After the restart a new Change is posted, and a name-only Undo of it returns `Ok` with ops.
- Keep `name-only Undo and Redo store completed Events with submission ids` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` passing unchanged.
- Filters:
  - `scripts/agent-test.sh --filter "Undo after restart"`
  - `scripts/agent-test.sh --filter "name-only Undo and Redo store completed Events with submission ids"`

## 7. Done
- The named tests pass.
- Upload, import, parse, Load, undo and persist complete.
