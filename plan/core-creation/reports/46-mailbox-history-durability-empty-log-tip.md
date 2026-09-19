# 46 — Mailbox History durability empty-log tip

Date: 2026-09-19. Ticket: [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md). Status stays `coded`. Spec: [code-review-46-mailbox-history-durability](code-review-46-mailbox-history-durability.md) Spec (c)1 and [46 mailbox History durability explore](46-mailbox-history-durability-explore.md) §3.

This is a correction note, not authority.

## 1. Miss

[EventLog.recoverState](../../../src/Shared/EventLog.fs) on `[]` kept Graph `State.eventId`. [CoreMailbox.getEventId](../../../src/Server/Core/CoreMailbox.fs) then returned a Graph checkpoint (`gambol.meta` / projection `revision`). That is a second serial. Ticket [§3.2 No second cursor type](../issues/46-mailbox-history-durability.md) says the checkpoint is recover-only. Explore §3: EventLog tip is `EventId.zero` when there are no events.

## 2. Fix

Empty EventLog recover sets `State.eventId` to `EventId.zero`. Graph bytes stay. A non-empty log still uses the checkpoint only to choose which Ops `Ev` ids to replay, then sets `eventId` to [EventLog.tip](../../../src/Shared/EventLog.fs).

## 3. createForTest and file import

[DbAgent.createForTest](../../../src/Server/Core/DbAgent.fs) is not a durability seam ([explore](46-mailbox-history-durability-explore.md) §5). It goes through the same `recoverState`. Injected Graph `eventId` is not a Poll cursor when the log is empty.

File import of documents with empty `SYSTEM/gambol.events` keeps the Graph. `getEventId` is `EventId.zero`. That is Spec-compatible. A seam that kept the meta/projection number would restore two serials. Next `EventLog.append` already mints id 1 from empty `nextId`. No extra seam.

## 4. Proof

1. [recoverState empty EventLog exposes EventId.zero](../../../tests/Shared.Tests/EventTests.fs) — Graph kept; `eventId` is `EventId.zero`.
2. [File empty EventLog recover exposes getEventId zero](../../../tests/Server.Tests/Issue46MailboxHistoryDurabilityTests.fs) — `gambol.meta` 4, empty EventLog, `getEventId` is `EventId.zero`.
3. File-mode document import and Db createForTest / empty-log projection facts now expect the EventLog tip, not the checkpoint.
