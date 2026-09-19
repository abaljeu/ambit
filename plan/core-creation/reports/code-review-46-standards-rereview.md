# Standards re-review — [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md)

Standards axis only. Range: three-dot `origin/staging...origin/cursor/46-mailbox-history-durability-9b3d`. `origin/staging` `77f47cf28ddc6e78db16965cb059e7df97456ba8`. Tip `44a25eaf` Keep Graph checkpoint as tip when EventLog is empty. Ticket Status is unchanged.

## Mechanical scan

`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`

[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):53 [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 9
[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):53 [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 4
[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):67 [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 4
[code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md):73 [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) BARE_ID item 4
[DbAgent.fs](src/Server/Core/DbAgent.fs) [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) FILE 561->565 already over 400 or new file over 400; change increased it

measure-fs-size: [EventLog.fs](src/Shared/EventLog.fs) `tip` 5, `adoptNewestHead` 11, `applyRecover` 6, `recoverState` 17. All under 40.

## Hard violations

1. **File length** — [fsharp-source.md](.agents/rules/fsharp-source.md): do not grow a file already over 400 lines. Scan: [DbAgent.fs](src/Server/Core/DbAgent.fs) 561→565 (`eventId` bump plus `recoverState`).

2. **Refer by name** — [refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id. Scan BARE_ID on [code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md) lines 53 (`item 9`, `item 4`), 67 (`item 4`), 73 (`item 4`). Added prose still uses bare `35b` / `46` / `34b` / `42` in [project.md](plan/core-creation/project.md) (“Not required for 35b §7”, “does not depend on 46”), [46 mailbox History durability](46-mailbox-history-durability.md) (“without 46”), and [46 mailbox History durability explore](46-mailbox-history-durability-explore.md) (“Old 34b names”, “Single-event 42 tests”, “without 46”).

3. **`change` bound to Ev** — [fsharp-source.md](.agents/rules/fsharp-source.md): use `event` / `events`, not `change`. [Issue46MailboxHistoryDurabilityTests.fs](tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) `stop :: change :: start` and `let childId, change`. [EventTests.fs](tests/Shared.Tests/EventTests.fs) new recover facts use `let change =` (older tests use `changeEv`).

## Judgement calls ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

- **Duplicated Code** — File and Db both bump `State.eventId` and both call `recoverState`:

```
loaded.state.Value <-
    { loaded.state.Value with eventId = event.id }
```

[FileAgent.fs](src/Server/Core/FileAgent.fs) append vs [DbAgent.fs](src/Server/Core/DbAgent.fs) `recordPersistedEvent`. `tip`, `adoptNewestHead`, and `restorePersisted` repeat `List.map Ev.id |> List.reduce EventId.max`. File and Db recover facts in [Issue46MailboxHistoryDurabilityTests.fs](tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) share one shape.

- **Divergent Change** / **Feature Envy** — [EventLog.fs](src/Shared/EventLog.fs) `recoverState` applies Graph `State` through `Ev.apply`. Explore asked for the helper; EventLog still gains a second reason to change.

- **Speculative Generality** — `applyRecover` keeps Graph on `ApplyResult.Invalid`. Explore did not ask recover to ignore Invalid.

## Not violations

[EventLog.fs](src/Shared/EventLog.fs) `tip` needs `EventLog` context. `EventId.next` stays inside EventLog. Draft Undo uses `EventId.zero`. Added F# lines ≤100 chars. [FileAgent.fs](src/Server/Core/FileAgent.fs) `createWithDependencies` is 38 lines. No new `mutable` or Exceptions. Production code uses `event` / `events` for Ev. Empty-log Graph checkpoint as door tip is Spec, not Standards.

Standards axis: 3 hard, 3 judgement. Worst hard: [DbAgent.fs](src/Server/Core/DbAgent.fs) grown while already over 400 lines.
