# Code review — 21 SES smell-cleanup

Range: `git diff 18b6880...HEAD` (three-dot). Commit `30490c7` go through old issues to clean up code. Spec: [21 — SES smell-cleanup](../issues/21-ses-smell-cleanup.md) and [SES smell-cleanup quality criteria](ses-smell-cleanup-quality-criteria.md). Scan: measure-fs-size; [Database.fs](src/Server/Database.fs):231 LONG (103).

Ticket **Status** stays `done`. This report is not approval.

## Standards

Range: `git diff 18b6880...HEAD` (three-dot). Commit `30490c7`. Parent `18b6880`. Standards only.

Mechanical scan (each line cites [fsharp-source.md](.agents/rules/fsharp-source.md): 40 lines per function, 100 characters per line):

- [Database.fs](src/Server/Database.fs)::getEvents 171–183 (13) — under 40
- [Database.fs](src/Server/Database.fs)::decodeProjectionEventId 228–230 (3) — under 40
- [DatabaseProjection.fs](src/Server/DatabaseProjection.fs)::plan 130–136 (7) — under 40
- [BootCache.fs](src/Shared/BootCache.fs)::maxPollEventIdGap 196 (1); maxEventIdGap 198–199 (2) — under 40
- [Database.fs](src/Server/Database.fs):231 LONG (103) — over 100

### (a) Documented standards

#### Hard

[fsharp-source.md](.agents/rules/fsharp-source.md): 100 characters or less per line. [Database.fs](src/Server/Database.fs) line 231 is 103 characters after `Graph * int` became `Graph * EventId`:

```
    let tryLoadGraphFromProjection (connectionString: string) : Task<Result<Graph * EventId, string>> =
```

#### Pass

Changed bindings are under 40 lines. [Database.fs](src/Server/Database.fs) grew 383→395 (under 400). [DatabaseProjection.fs](src/Server/DatabaseProjection.fs) stays 569: already over 400 and this change did not grow it. Surgical under-100-line preference is not a fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

[core-api.md](.agents/rules/core-api.md) EventId serial: production `fromJson` stays in `decodeProjectionEventId` (SQL peel). [DbAgent.fs](src/Server/Core/DbAgent.fs) restore calls `Database.getEvents` and does not peel `EventId.beforeAll` into SQL. No new `EventId.next`. Hunks that mint drafts use `EventId.zero`.

[fsharp-source.md](.agents/rules/fsharp-source.md) Ev names: production remaps `ackEvents`, drops `.changes` aliases, reads `syncResponse.events`. `getEvents` SQL is a tiny SELECT and matches `getEventsAfter`.

### (b) Baseline smells (judgement)

Possible Duplicated Code — `getEvents` copies `getEventsAfter` without the WHERE:

```
SELECT event_id, payload FROM events
ORDER BY event_id ASC
```

Possible Mysterious Name — [Update.fs](src/Client/Update.fs) still logs `newRev=` beside `newState.eventId.Value` on the hunk that renamed `changes` to `events`.

Possible Middle Man — `decodeProjectionEventId` is `EventId.fromJson`. Suppress: [core-api.md](.agents/rules/core-api.md) wants a named SQL peel.

Shotgun Surgery across many files is leftover Change/Revision rename. Suppress.

## Spec

Range: `18b6880...HEAD` (30490c7). Spec: [21 — SES smell-cleanup](../issues/21-ses-smell-cleanup.md) and [SES smell-cleanup quality criteria](ses-smell-cleanup-quality-criteria.md). KEEP [1.2 KEEP names that mean EventBody.Change or HTTP](ses-smell-cleanup-quality-criteria.md#12-keep-names-that-mean-eventbodychange-or-http) held: `mintChange`, HTTP `/changes`, and names that mean `EventBody.Change` of an Op list were not renamed.

### (a) Missing or partial

1. **1.4 leftover Revision in logs and comments.** Ticket: “spoken and API text say event id, not Revision, for an EventId value.” Criteria 1.4 Look for: “A public API comment, error, or log that calls an EventId value Revision.” [App.fs](src/Client/App.fs) `baseRev=` was renamed. [Update.fs](src/Client/Update.fs) still logs `newRev=` next to `newState.eventId` on LoadDone (this ticket edited that log) and both PollDone paths; the Loading comment still says “advance Revision.”
2. **1.3 leftover Revision names on EventId.** Ticket: “EventId APIs say event id (`readEventId`, `writeEventId`, `getFileEventId`); the value stays `EventId`.” Criteria 1.3 Look for: a parameter or field typed `EventId` still named `revision` / `rev`. [Update.fs](src/Client/Update.fs) binds `responseRevision` for `EventId` even though [ViewModel.fs](src/Shared/ViewModel.fs) already names the case field `responseEventId`. [Api.fs](src/Server/Api.fs) still has `let revValue = EventId.value eventId` (Look for remaining; not in this ticket’s hunks). Edited tests still keep `decodeSuccessRevision` and `let! revision = CoreMailbox.getEventId`.

Named bars that look complete for sites this ticket edited: 1.1 `ackEvents` and drop `.changes` aliases; 2.1 named peels only; 2.2 drafts `EventId.zero`; 2.4 `next` only in [EventLog.fs](src/Shared/EventLog.fs); 3.1 `shouldTruncate` / `plan` / projection load take `EventId`; 3.2 int at SQL/JSON peel; 4.1 TestActor hello `Authority "Actor"`.

### (b) Scope creep

[.agents/rules/environment.md](.agents/rules/environment.md) adds Cloud/Linux shell notes. Not a named 21 criterion.

`Database.getEvents` (full-log SELECT, no cursor) is the stated 2.3 follow-up so DbAgent restore does not peel `EventId.beforeAll`. Not creep.

### (c) Implemented but wrong

None. 2.3 Core no longer passes bare `-1`; restore reads the full log. Do not reopen Spec for [11 — One serial event id](../issues/11-one-serial-event-id.md) or [12 — Contract leftover Change and Revision](../issues/12-contract-leftover-change-and-revision.md).

## Summary

Standards: 1 hard (103-char line in [Database.fs](src/Server/Database.fs)); worst = that LONG line. Spec: 2 partial (1.3/1.4 leftover Revision names/logs); worst = `newRev=` / `responseRevision` still on EventId in [Update.fs](src/Client/Update.fs).
