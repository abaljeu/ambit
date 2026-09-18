# Standards — 21 SES smell-cleanup

Range: `git diff 18b6880...HEAD` (three-dot). Commit `30490c7`. Parent `18b6880`. Standards only.

Mechanical scan (each line cites [fsharp-source.md](.agents/rules/fsharp-source.md): 40 lines per function, 100 characters per line):

- [Database.fs](src/Server/Database.fs)::getEvents 171–183 (13) — under 40
- [Database.fs](src/Server/Database.fs)::decodeProjectionEventId 228–230 (3) — under 40
- [DatabaseProjection.fs](src/Server/DatabaseProjection.fs)::plan 130–136 (7) — under 40
- [BootCache.fs](src/Shared/BootCache.fs)::maxPollEventIdGap 196 (1); maxEventIdGap 198–199 (2) — under 40
- [Database.fs](src/Server/Database.fs):231 LONG (103) — over 100

## (a) Documented standards

### Hard

[fsharp-source.md](.agents/rules/fsharp-source.md): 100 characters or less per line. [Database.fs](src/Server/Database.fs) line 231 is 103 characters after `Graph * int` became `Graph * EventId`:

```
    let tryLoadGraphFromProjection (connectionString: string) : Task<Result<Graph * EventId, string>> =
```

### Pass

Changed bindings are under 40 lines. [Database.fs](src/Server/Database.fs) grew 383→395 (under 400). [DatabaseProjection.fs](src/Server/DatabaseProjection.fs) stays 569: already over 400 and this change did not grow it. Surgical under-100-line preference is not a fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

[core-api.md](.agents/rules/core-api.md) EventId serial: production `fromJson` stays in `decodeProjectionEventId` (SQL peel). [DbAgent.fs](src/Server/Core/DbAgent.fs) restore calls `Database.getEvents` and does not peel `EventId.beforeAll` into SQL. No new `EventId.next`. Hunks that mint drafts use `EventId.zero`.

[fsharp-source.md](.agents/rules/fsharp-source.md) Ev names: production remaps `ackEvents`, drops `.changes` aliases, reads `syncResponse.events`. `getEvents` SQL is a tiny SELECT and matches `getEventsAfter`.

## (b) Baseline smells (judgement)

Possible Duplicated Code — `getEvents` copies `getEventsAfter` without the WHERE:

```
SELECT event_id, payload FROM events
ORDER BY event_id ASC
```

Possible Mysterious Name — [Update.fs](src/Client/Update.fs) still logs `newRev=` beside `newState.eventId.Value` on the hunk that renamed `changes` to `events`.

Possible Middle Man — `decodeProjectionEventId` is `EventId.fromJson`. Suppress: [core-api.md](.agents/rules/core-api.md) wants a named SQL peel.

Shotgun Surgery across many files is leftover Change/Revision rename. Suppress.
