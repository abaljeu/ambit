# 46 — Mailbox History durability live-append tip

Date: 2026-09-19. Ticket: [46 — Mailbox History durability](../issues/46-mailbox-history-durability.md). Status stays `coded`.

Superseded by [46 mailbox History durability reconcile](46-mailbox-history-durability-reconcile.md). Alan rejected `EventId.max` on live `appendEvent`. Graph-ahead means the prior log was invalid and recover must drop it. Mailbox seed of an empty log uses [EventLog.afterCheckpoint](../../../src/Shared/EventLog.fs) so a later mint cannot recreate Graph-ahead skew.
