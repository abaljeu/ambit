# Spec axis: 21 — SES smell-cleanup

Range: `18b6880...HEAD` (30490c7). Spec: [21 — SES smell-cleanup](../issues/21-ses-smell-cleanup.md) and [SES smell-cleanup quality criteria](ses-smell-cleanup-quality-criteria.md). KEEP [1.2 KEEP names that mean EventBody.Change or HTTP](ses-smell-cleanup-quality-criteria.md#12-keep-names-that-mean-eventbodychange-or-http) held: `mintChange`, HTTP `/changes`, and names that mean `EventBody.Change` of an Op list were not renamed.

## (a) Missing or partial

1. **1.4 leftover Revision in logs and comments.** Ticket: “spoken and API text say event id, not Revision, for an EventId value.” Criteria 1.4 Look for: “A public API comment, error, or log that calls an EventId value Revision.” [App.fs](src/Client/App.fs) `baseRev=` was renamed. [Update.fs](src/Client/Update.fs) still logs `newRev=` next to `newState.eventId` on LoadDone (this ticket edited that log) and both PollDone paths; the Loading comment still says “advance Revision.”
2. **1.3 leftover Revision names on EventId.** Ticket: “EventId APIs say event id (`readEventId`, `writeEventId`, `getFileEventId`); the value stays `EventId`.” Criteria 1.3 Look for: a parameter or field typed `EventId` still named `revision` / `rev`. [Update.fs](src/Client/Update.fs) binds `responseRevision` for `EventId` even though [ViewModel.fs](src/Shared/ViewModel.fs) already names the case field `responseEventId`. [Api.fs](src/Server/Api.fs) still has `let revValue = EventId.value eventId` (Look for remaining; not in this ticket’s hunks). Edited tests still keep `decodeSuccessRevision` and `let! revision = CoreMailbox.getEventId`.

Named bars that look complete for sites this ticket edited: 1.1 `ackEvents` and drop `.changes` aliases; 2.1 named peels only; 2.2 drafts `EventId.zero`; 2.4 `next` only in [EventLog.fs](src/Shared/EventLog.fs); 3.1 `shouldTruncate` / `plan` / projection load take `EventId`; 3.2 int at SQL/JSON peel; 4.1 TestActor hello `Authority "Actor"`.

## (b) Scope creep

[.agents/rules/environment.md](.agents/rules/environment.md) adds Cloud/Linux shell notes. Not a named 21 criterion.

`Database.getEvents` (full-log SELECT, no cursor) is the stated 2.3 follow-up so DbAgent restore does not peel `EventId.beforeAll`. Not creep.

## (c) Implemented but wrong

None. 2.3 Core no longer passes bare `-1`; restore reads the full log. Do not reopen Spec for [11 — One serial event id](../issues/11-one-serial-event-id.md) or [12 — Contract leftover Change and Revision](../issues/12-contract-leftover-change-and-revision.md).
