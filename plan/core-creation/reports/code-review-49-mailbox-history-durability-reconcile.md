# code-review — [49 — Mailbox History durability](../issues/49-mailbox-history-durability.md) reconcile

Date: 2026-09-19. Independent `/code-review` after Alan's three-way reconcile. Reviewer is not the implementer. Ticket **Status** stays `coded` (this report is not the Status write).

Range: three-dot `origin/staging` (`77f47cf2`) ... tip `ea1d1af2` of [PR 41](https://github.com/abaljeu/ambit/pull/41) branch `cursor/46-live-append-eventid-max-676f`. Authority: Alan's load compare on the ticket What to build, plus the user Spec for this review. [46 mailbox History durability reconcile](49-mailbox-history-durability-reconcile.md) is the correction note. Empty→zero and EventId.max tip patches are not Spec.

Axes: [Standards](code-review-46-standards-axis.md), [Spec](code-review-46-spec-axis.md).

## Standards

1. **Hard — file length.** [fsharp-source.md](../../../.agents/rules/fsharp-source.md): 400 lines or less per file; if a file is already longer, do not increase it. Scan: [DbAgent.fs](../../../src/Server/Core/DbAgent.fs) 561→577. `loadReconciled` and the live `eventId` bump grow a file that is already over the limit.
2. **Hard — file length.** Same rule. Scan: [EventTests.fs](../../../tests/Shared.Tests/EventTests.fs) 313→409. New `recover` facts push a file that was under 400 past the limit.
3. **Hard — refer by name.** [refer-by-name.md](../../../.agents/rules/refer-by-name.md): never refer by only the id. Scan BARE_ID: [code-review-49-mailbox-history-durability-rereview.md](code-review-49-mailbox-history-durability-rereview.md) lines 28–31 and 39 (`item 9`, `item 4`); [code-review-49-mailbox-history-durability.md](code-review-49-mailbox-history-durability.md) lines 53, 67, 73 (`item 9`, `item 4`). Added prose also uses bare `46` / `35b` / `34b` / `42` in [project.md](../project.md) (“does not need 46”, “does not depend on 46”), [46 mailbox History durability](49-mailbox-history-durability.md) (“without 46”), and [46 mailbox History durability explore](49-mailbox-history-durability-explore.md) (“Old 34b names”, “Single-event 42 tests”, “without 46”).
4. **Hard — `change` bound to Ev.** [fsharp-source.md](../../../.agents/rules/fsharp-source.md): rename the local to `event` / `events`. [Issue49MailboxHistoryDurabilityTests.fs](../../../tests/Server.Tests/Issue49MailboxHistoryDurabilityTests.fs) `stop :: change :: start` and `let childId, change`. [EventTests.fs](../../../tests/Shared.Tests/EventTests.fs) `let change, childId = recoverChange`.
5. **Judgement — Duplicated Code.** File and Db both bump `State.eventId` and both drop persist when recover empties a non-empty log:

```
loaded.state.Value <-
    { loaded.state.Value with eventId = event.id }
```

[FileAgent.fs](../../../src/Server/Core/FileAgent.fs) append against [DbAgent.fs](../../../src/Server/Core/DbAgent.fs) `recordPersistedEvent`. `tip`, `adoptNewestHead`, and `restorePersisted` repeat `List.map Ev.id |> List.reduce EventId.max`. File and Db recover facts share one shape.

6. **Judgement — Divergent Change / Feature Envy.** [EventLog.fs](../../../src/Shared/EventLog.fs) `recover` applies Graph `State` through `Ev.apply`. EventLog gains a second reason to change.
7. **Judgement — Speculative Generality.** `applyRecover` keeps Graph on `ApplyResult.Invalid`. The three-way compare does not need that hook.

4 hard, 3 judgement.

## Spec

### (a) Missing or partial

No missing or partial requirement: persist/load, three-way compare, Ops `Ev.apply` catch-up, ActorStart / ActorStop log-only for Graph, Undo Change-only, one EventId, live minted `Ev.id`, empty-seed `nextId` past Graph, and same-transaction write still out of scope.

### (b) Scope creep

The extra EventLog helpers (`tip`, `adoptNewestHead`, `afterCheckpoint`) and persist drop (`EventLogFile.truncate`, `Database.clearEvents`) are the drop and empty-seed machinery the Spec names. Inverted restart tests in [FileAgentFailureTests](../../../tests/Server.Tests/FileAgentFailureTests.fs) and [StateEndpointTests](../../../tests/Server.Tests/StateEndpointTests.fs) follow EventLog authority for Ops replay when the log is ahead. [CoreMailboxBackend.seedEventLog](../../../src/Server/Core/CoreMailboxBackend.fs) maps `getEventId` Error to [EventLog.empty](../../../src/Shared/EventLog.fs). Spec: "seed of an empty log continues `nextId` past the Graph checkpoint so a later append cannot recreate that skew." That Error door starts `nextId` at 1. FileAgent and DbAgent `getEventId` do not return Error.

### (c) Implemented but wrong

The three-way paths in [EventLog.recover](../../../src/Shared/EventLog.fs) are correct: Graph id > Log id drops the log and keeps Graph EventId; equal is noop; Log id > Graph id applies `Ev` records until concurrent and moves EventId on that walk, not by a bulk tip assign.

The tip matches Alan's three-way Spec. No major correction.

## Summary

Standards: 7 findings (4 hard, 3 judgement); worst is file-length growth in [DbAgent.fs](../../../src/Server/Core/DbAgent.fs) and [EventTests.fs](../../../tests/Shared.Tests/EventTests.fs). Spec: 0 missing, 0 wrong; worst within Spec is the unused `getEventId` Error → empty-seed door, not a three-way miss. Focused tests: 6 Shared `recover` facts and 11 [Issue49MailboxHistoryDurabilityTests](../../../tests/Server.Tests/Issue49MailboxHistoryDurabilityTests.fs) File/Db facts passed.

approve → Status done
