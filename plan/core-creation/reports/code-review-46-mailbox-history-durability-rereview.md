# Code review re-review — [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md)

Independent `/code-review` after Alan overruled empty EventLog → `EventId.zero`. Not approval. Ticket Status stays `coded`.

Range pinned and non-empty: three-dot `origin/staging...origin/cursor/46-mailbox-history-durability-9b3d`. `origin/staging` `77f47cf28ddc6e78db16965cb059e7df97456ba8`. Tip `44a25eaf` Keep Graph checkpoint as tip when EventLog is empty.

Commits (`origin/staging..` tip):

- `44a25eaf` Keep Graph checkpoint as tip when EventLog is empty.
- `39a732e5` Set empty EventLog recover tip to EventId.zero.
- `afa909e5` Keep Graph checkpoint when EventLog is empty.
- `e1315d20` Implement 46 — Mailbox History durability.
- `437b3ab2` Widen 46 plan: EventLog authority, replay, one serial.
- `bf293df9` Explore 46 mailbox History durability.

Superseded: [code-review-46-mailbox-history-durability](code-review-46-mailbox-history-durability.md) Spec (c)1 empty EventLog → `EventId.zero`. Alan locks in the ticket and [46 mailbox History durability empty-log tip](46-mailbox-history-durability-empty-log-tip.md) are the door rule.

Focused tests (green): [Issue46MailboxHistoryDurabilityTests](tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) — 7 passed. [EventTests](tests/Shared.Tests/EventTests.fs) — 20 passed.

## Standards

Standards axis only. Range as pinned. Ticket Status is unchanged.

### Mechanical scan

`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`

[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):53 [refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 9
[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):53 [refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 4
[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):67 [refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 4
[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):73 [refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 4
[DbAgent.fs](src/Server/Core/DbAgent.fs) [fsharp-source.md](.agents/rules/fsharp-source.md) FILE 561->565 already over 400 or new file over 400; change increased it

measure-fs-size: [EventLog.fs](src/Shared/EventLog.fs) `tip` 5, `adoptNewestHead` 11, `applyRecover` 6, `recoverState` 17. All under 40.

### Hard violations

1. **File length** — [fsharp-source.md](.agents/rules/fsharp-source.md): do not grow a file already over 400 lines. Scan: [DbAgent.fs](src/Server/Core/DbAgent.fs) 561→565 (`eventId` bump plus `recoverState`).
2. **Refer by name** — [refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id. Scan BARE_ID on [code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md) lines 53 (`item 9`, `item 4`), 67 (`item 4`), 73 (`item 4`). Added prose still uses bare `35b` / `46` / `34b` / `42` in [project.md](plan/core-creation/project.md) (“Not required for 35b §7”, “does not depend on 46”), [46 mailbox History durability](46-mailbox-history-durability.md) (“without 46”), and [46 mailbox History durability explore](46-mailbox-history-durability-explore.md) (“Old 34b names”, “Single-event 42 tests”, “without 46”).
3. **`change` bound to Ev** — [fsharp-source.md](.agents/rules/fsharp-source.md): use `event` / `events`, not `change`. [Issue46MailboxHistoryDurabilityTests.fs](tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) `stop :: change :: start` and `let childId, change`. [EventTests.fs](tests/Shared.Tests/EventTests.fs) new recover facts use `let change =` (older tests use `changeEv`).

### Judgement calls ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

- **Duplicated Code** — File and Db both bump `State.eventId` and both call `recoverState`:

```
loaded.state.Value <-
    { loaded.state.Value with eventId = event.id }
```

[FileAgent.fs](src/Server/Core/FileAgent.fs) append vs [DbAgent.fs](src/Server/Core/DbAgent.fs) `recordPersistedEvent`. `tip`, `adoptNewestHead`, and `restorePersisted` repeat `List.map Ev.id |> List.reduce EventId.max`. File and Db recover facts in [Issue46MailboxHistoryDurabilityTests.fs](tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) share one shape.

- **Divergent Change** / **Feature Envy** — [EventLog.fs](src/Shared/EventLog.fs) `recoverState` applies Graph `State` through `Ev.apply`. Explore asked for the helper; EventLog still gains a second reason to change.
- **Speculative Generality** — `applyRecover` keeps Graph on `ApplyResult.Invalid`. Explore did not ask recover to ignore Invalid.

### Not violations

[EventLog.fs](src/Shared/EventLog.fs) `tip` needs `EventLog` context. `EventId.next` stays inside EventLog. Draft Undo uses `EventId.zero`. Added F# lines ≤100 chars. [FileAgent.fs](src/Server/Core/FileAgent.fs) `createWithDependencies` is 38 lines. No new `mutable` or Exceptions. Production code uses `event` / `events` for Ev. Empty-log Graph checkpoint as door tip is Spec, not Standards.

Standards axis: 3 hard, 3 judgement. Worst hard: [DbAgent.fs](src/Server/Core/DbAgent.fs) grown while already over 400 lines.

## Spec

Spec: ticket What to build + Out of scope; Alan locks (authoritative over prior empty→zero); [46 mailbox History durability empty-log tip](46-mailbox-history-durability-empty-log-tip.md); [46 mailbox History durability explore](46-mailbox-history-durability-explore.md); [Core creation architecture](plan/core-creation/arch.md) EventLog Interface 9 EventLog persist is authoritative and PersistHandlers Interface 4 create-time replay when they do not conflict. Superseded: [code-review-46-mailbox-history-durability](code-review-46-mailbox-history-durability.md) Spec (c)1 empty EventLog → `EventId.zero`. That empty→zero door is not required.

### (a) Missing or partial

None. Persist and load of Change / ActorStart / ActorStop `Ev` records, newest-head `seedEventLog` via `adoptNewestHead`, Ops `Ev` replay when the Graph checkpoint lags, Actor bodies log-only, Undo Change-only, and same-txn left out all match [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md) What to build and Out of scope.

### (b) Scope creep

None. [EventLog.tip](src/Shared/EventLog.fs), `adoptNewestHead`, and `recoverState` are the asked helpers. File/Db create-time `recoverState` and the live `appendEvent` serial bump are the recover/serial cut. [FileAgentFailureTests](tests/Server.Tests/FileAgentFailureTests.fs) and [StateEndpointTests](tests/Server.Tests/StateEndpointTests.fs) now expect replay because EventLog persist is the authority.

### (c) Looks implemented but wrong

1. Live append can drop a Graph-ahead serial. Alan lock: "Graph ahead of log by eventId → Graph keeps tip until log catches up." [46 mailbox History durability empty-log tip](46-mailbox-history-durability-empty-log-tip.md) §1.2: "Graph supplies the tip until the log catches up." Ticket [§3.1 One EventId](plan/core-creation/issues/46-mailbox-history-durability.md): "When EventLog is empty/missing or Graph is ahead by eventId, they equal the Graph checkpoint." [EventLog.recoverState](src/Shared/EventLog.fs) uses `EventId.max (tip log) state.eventId`. [FileAgent.appendEvent](src/Server/Core/FileAgent.fs) and [DbAgent.recordPersistedEvent](src/Server/Core/DbAgent.fs) set `State.eventId = event.id` with no max. After Graph-ahead recover (`eventId` 5, log tip 2), the next persist append (id 3, including ActorStart / ActorStop) sets `getEventId` to 3. The log has not caught up.

### Graph-tip verdict

Graph-tip holds on recover: empty or missing EventLog keeps the Graph checkpoint as `getEventId` / `State.eventId` (not `EventId.zero`); Graph-ahead keeps that checkpoint and does not rewrite EventLog from Graph; a non-empty log at or ahead of Graph uses the EventLog tip including ActorStop; recover replays Ops `Ev` only (`Ev.isAction`). Graph-tip does not hold after a later live append while Graph is still ahead.

Spec axis: (a) 0, (b) 0, (c) 1. Worst: live `appendEvent` drops the Graph-ahead serial.

## Summary

Standards: 3 hard, 3 judgement. Worst: [DbAgent.fs](src/Server/Core/DbAgent.fs) 561→565 against [fsharp-source.md](.agents/rules/fsharp-source.md) 400-line file rule.

Spec: (a) 0, (b) 0, (c) 1. Worst: live [FileAgent.appendEvent](src/Server/Core/FileAgent.fs) / [DbAgent.recordPersistedEvent](src/Server/Core/DbAgent.fs) overwrite `State.eventId` with the stored `Ev.id` while EventLog is still behind Graph. Recover Graph-tip in [EventLog.recoverState](src/Shared/EventLog.fs) (`[]` keeps Graph; non-empty uses `EventId.max`) matches Alan locks 2–4. Empty→zero is not a miss.

Locks that hold on recover and the hello/restart proofs: EventLog persist load; `seedEventLog` `adoptNewestHead`; Ops re-execute when the checkpoint lags; Actor bodies log-only; empty/missing EventLog door is the Graph checkpoint, not `EventId.zero`; Graph-ahead recover keeps the Graph serial and does not rewrite EventLog; non-empty log at or ahead uses the EventLog tip including ActorStop; Undo Change-only; same-txn write not added.

**major corrections required**

1. Live `appendEvent` must keep the Graph checkpoint when EventLog tip is still behind (`EventId.max` of stored `Ev.id` and current `State.eventId`), the same rule [EventLog.recoverState](src/Shared/EventLog.fs) already uses. Ticket [§3.1 One EventId](plan/core-creation/issues/46-mailbox-history-durability.md) and [46 mailbox History durability empty-log tip](46-mailbox-history-durability-empty-log-tip.md) §1.2: Graph keeps the tip until the log catches up.
