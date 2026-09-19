# Code review — [49 — Mailbox History durability](plan/core-creation/issues/49-mailbox-history-durability.md)

Independent `/code-review`. Not approval. Ticket Status stays `coded`.

Range pinned and non-empty: three-dot `origin/staging...origin/cursor/49-mailbox-history-durability-9b3d`. `origin/staging` `77f47cf28ddc6e78db16965cb059e7df97456ba8`. Tip `afa909e5f039f9774fbe95d420c735f3091eadc4`.

Commits (`origin/staging..` tip):

- `afa909e5` Keep Graph checkpoint when EventLog is empty.
- `e1315d20` Implement 49 — Mailbox History durability.
- `437b3ab2` Widen 46 plan: EventLog authority, replay, one serial.
- `bf293df9` Explore 46 mailbox History durability.

Focused tests (green): [Issue49MailboxHistoryDurabilityTests](tests/Server.Tests/Issue49MailboxHistoryDurabilityTests.fs), [Issue42PersistHandlersTests](tests/Server.Tests/Issue42PersistHandlersTests.fs), [PersistHandlersRestoreTests](tests/Server.Tests/PersistHandlersRestoreTests.fs), [FileAgentFailureTests](tests/Server.Tests/FileAgentFailureTests.fs), [StateEndpointTests](tests/Server.Tests/StateEndpointTests.fs) — 83 passed. [EventTests](tests/Shared.Tests/EventTests.fs) — 19 passed.

## Standards

Standards axis only. Range as pinned. Ticket Status is unchanged.

### Mechanical scan

`src/Server/Core/DbAgent.fs  .agents/rules/fsharp-source.md  FILE 561->565  already over 400 or new file over 400; change increased it`

measure-fs-size (parent scan; not re-run): [EventLog.fs](src/Shared/EventLog.fs) `tip` 5, `adoptNewestHead` 11, `applyRecover` 6, `recoverState` 17. All under 40.

### Hard violations

1. **File length** — [fsharp-source.md](.agents/rules/fsharp-source.md): 400 lines or less per file; if a file is already longer, do not increase it. Scan: [DbAgent.fs](src/Server/Core/DbAgent.fs) 561→565. The append `eventId` bump and `recoverState` call add four lines to a file already over the limit.

2. **Refer by name** — [refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id or number. New prose uses bare `35b` / `46` in [project.md](plan/core-creation/project.md) (“Not required for 35b §7”, “does not depend on 46”), the [implement report](49-mailbox-history-durability.md) (“without 46”), and the [explore report](49-mailbox-history-durability-explore.md) (“without 46”).

### Judgement calls ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

- **Duplicated Code** — File and Db both bump `State.eventId` on append and both call `recoverState` on create:

```
loaded.state.Value <-
    { loaded.state.Value with eventId = event.id }
```

[FileAgent.fs](src/Server/Core/FileAgent.fs) append vs [DbAgent.fs](src/Server/Core/DbAgent.fs) `recordPersistedEvent`. `tip` and `adoptNewestHead` repeat `List.map Ev.id |> List.reduce EventId.max`. File and Db recover facts in [Issue49MailboxHistoryDurabilityTests.fs](tests/Server.Tests/Issue49MailboxHistoryDurabilityTests.fs) share one shape.

- **Divergent Change** / **Feature Envy** — [EventLog.fs](src/Shared/EventLog.fs) `recoverState` applies Graph `State` through `Ev.apply`, not only EventLog structure. Explore asked for this helper; that module still gains a second reason to change.

### Not violations

[EventLog.fs](src/Shared/EventLog.fs) `tip` needs `EventLog` context; explore allowed it. `EventId.next` stays inside EventLog. Draft Undo uses `EventId.zero`. Added lines ≤100 chars. [FileAgent.fs](src/Server/Core/FileAgent.fs) `createWithDependencies` is 38 lines. No new `mutable` or Exceptions. Bindings use `event` / `events` for `Ev`.

Standards axis: 2 hard, 2 judgement. Worst hard: [DbAgent.fs](src/Server/Core/DbAgent.fs) grown while already over 400 lines.

## Spec

Spec: ticket What to build + Out of scope; [46 mailbox History durability explore](49-mailbox-history-durability-explore.md); [Core creation architecture](plan/core-creation/arch.md) EventLog item 9 and PersistHandlers item 4.

### (a) Missing or partial

1. File mixed hello does not prove Graph child. Explore §4: "Graph showing the hello child even when documents or projection were behind that Change." [Issue49MailboxHistoryDurabilityTests](tests/Server.Tests/Issue49MailboxHistoryDurabilityTests.fs) `File mixed hello survives dispose create` calls `assertRestartLog` only. Db mixed hello asserts Graph. File recover uses `appendEvent` with lag, not mixed hello.

### (b) Scope creep

1. Recover swallows Invalid. [EventLog.recoverState](src/Shared/EventLog.fs) `applyRecover` keeps Graph when `Ev.apply` is `ApplyResult.Invalid`, then still sets `eventId` to the EventLog tip. Ticket §2.2: "apply Change/Undo/Redo `Ev` records that are in the log and not yet on Graph." Explore §5 puts corrupt JSON out of scope and does not ask recover to ignore Invalid.

### (c) Looks implemented but wrong

1. Empty EventLog keeps the Graph checkpoint as `State.eventId`. Ticket §3.1: "`getEventId` / `State.eventId` / HTTP `latestId` / Poll equal the EventLog tip." Explore §3: "tip is `EventId.zero` when there are no events"; checkpoint "stays recover-only for which Ops `Ev` ids to replay." `recoverState` on `[]` returns `state`. Explore §5 forbids a repair that rewrites EventLog from Graph. Setting `eventId` to `EventId.zero` is not that repair. Leaving the checkpoint makes `getEventId` a Graph serial. One serial and EventLog authority fail. `createForTest` is not a durability seam (explore §5).

2. File mixed hello restart skips a Change that is in EventLog and not on Graph. Ticket §2.1: "File `SYSTEM/gambol.events` and Db `events` win when Graph or projection disagree." Ticket §2.2: apply Ops `Ev` "that are in the log and not yet on Graph." `recoverState` replays only Action `Ev` with `id` greater than the checkpoint. File live Change writes `gambol.meta` to that Change id even when documents omit a non-artifact root child. Recover then skips. Arch PersistHandlers item 4 matches the checkpoint filter; ticket §2.1 does not.

## Summary

Standards: 2 hard, 2 judgement. Worst: [DbAgent.fs](src/Server/Core/DbAgent.fs) 561→565 against [fsharp-source.md](.agents/rules/fsharp-source.md) 400-line file rule.

Spec: 1 missing/partial, 1 creep, 2 wrong (axis). Worst: empty EventLog leaves Graph checkpoint as `getEventId`. Independent check: axis (c)2 follows [46 mailbox History durability explore](49-mailbox-history-durability-explore.md) §3 and [Core creation architecture](plan/core-creation/arch.md) PersistHandlers item 4 (checkpoint chooses which Ops `Ev` ids to replay; File soft-fail keeps `gambol.meta` behind so recover can run). That is not a product defect. File mixed-hello Graph assert is a proof gap; [File recover applies Ops Ev ahead of Graph checkpoint](tests/Server.Tests/Issue49MailboxHistoryDurabilityTests.fs) already covers File catch-up when the checkpoint lags.

Locks that hold on the hello/restart path: EventLog persist load; `seedEventLog` `adoptNewestHead`; Ops re-execute when the checkpoint lags; Actor bodies log-only; live and restart `getEventId` = ActorStop tip; Undo Change-only; same-txn write not added.

**major corrections required**

1. Empty EventLog must set `State.eventId` / `getEventId` to the EventLog tip (`EventId.zero`), not keep the Graph checkpoint as a serial. Ticket [§3.2 No second cursor type](plan/core-creation/issues/49-mailbox-history-durability.md): checkpoint stays recover-only. Explore §3: tip is `EventId.zero` when there are no events. `recoverState` on `[]` plus [recoverState empty log keeps Graph checkpoint EventId](tests/Shared.Tests/EventTests.fs) lock the miss.
