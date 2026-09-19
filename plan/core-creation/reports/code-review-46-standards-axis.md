# Standards axis — [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md)

Independent Standards review of tip `ea1d1af2` against `origin/staging`. Ticket Status is unchanged.

## Findings

1. **Hard — file length.** [fsharp-source.md](.agents/rules/fsharp-source.md): 400 lines or less per file; if a file is already longer, do not increase it. Scan: [DbAgent.fs](src/Server/Core/DbAgent.fs) 561→577. `loadReconciled` and the live `eventId` bump grow a file that is already over the limit.
2. **Hard — file length.** Same rule. Scan: [EventTests.fs](tests/Shared.Tests/EventTests.fs) 313→409. New `recover` facts push a file that was under 400 past the limit.
3. **Hard — refer by name.** [refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id. Scan BARE_ID: [code-review-46-mailbox-history-durability-rereview.md](code-review-46-mailbox-history-durability-rereview.md) lines 28–31 and 39 (`item 9`, `item 4`); [code-review-46-mailbox-history-durability.md](code-review-46-mailbox-history-durability.md) lines 53, 67, 73 (`item 9`, `item 4`). Added prose also uses bare `46` / `35b` / `34b` / `42` in [project.md](plan/core-creation/project.md) (“does not need 46”, “does not depend on 46”), [46 mailbox History durability](46-mailbox-history-durability.md) (“without 46”), and [46 mailbox History durability explore](46-mailbox-history-durability-explore.md) (“Old 34b names”, “Single-event 42 tests”, “without 46”).
4. **Hard — `change` bound to Ev.** [fsharp-source.md](.agents/rules/fsharp-source.md): rename the local to `event` / `events`. [Issue46MailboxHistoryDurabilityTests.fs](tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) `stop :: change :: start` and `let childId, change`. [EventTests.fs](tests/Shared.Tests/EventTests.fs) `let change, childId = recoverChange`.
5. **Judgement — Duplicated Code.** File and Db both bump `State.eventId` and both drop persist when recover empties a non-empty log:

```
loaded.state.Value <-
    { loaded.state.Value with eventId = event.id }
```

[FileAgent.fs](src/Server/Core/FileAgent.fs) append against [DbAgent.fs](src/Server/Core/DbAgent.fs) `recordPersistedEvent`. `tip`, `adoptNewestHead`, and `restorePersisted` repeat `List.map Ev.id |> List.reduce EventId.max`. File and Db recover facts share one shape.

6. **Judgement — Divergent Change / Feature Envy.** [EventLog.fs](src/Shared/EventLog.fs) `recover` applies Graph `State` through `Ev.apply`. EventLog gains a second reason to change.
7. **Judgement — Speculative Generality.** `applyRecover` keeps Graph on `ApplyResult.Invalid`. The three-way compare does not need that hook.

4 hard, 3 judgement.
