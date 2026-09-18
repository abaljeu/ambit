# Code review — 14 EventId serial on Shared + Server

Range: uncommitted vs `HEAD`. Spec: [14 — EventId serial on Shared + Server](../issues/14-eventid-serial-shared-server.md). Scan: `src/Server/Bookkeeping.fs::readEventId` 13 lines.

After Spec, `parseClientRev` became `parseClientEventId`. Query key `rev` and SQL `graph.revision` stay.

## Standards

Mechanical: [Bookkeeping.fs](src/Server/Bookkeeping.fs)::`readEventId` is 13 lines. Under [[.agents/rules/fsharp-source.md]] “40 lines or less per function”. No fail. No long-line, TAB, mutable, file-growth, or bare-id hits.

### 1. Documented standards

**Hard violations:** none in this diff.

**Judgement / surgical**

- [[.agents/rules/core-agent-behavior.md]] Surgical Changes. [13 — Revision always 0 (diagnostic)](../issues/13-revision-always-zero.md) drops `:5215` `/ambit?debug=1` from Context, In, and Green bar. Adjacent ticket polish, not required by the EventId rename. Leftover sentence: `Interactive green bar.` (Alan’s status-done edit.)
- [[.agents/rules/fsharp-source.md]] “Don’t use Exceptions.” Touched `readEventId` still swallows `try/with _ -> EventId.zero`. Pre-existing I/O shape; `writeEventId` already maps `ex` to `Result`. Not introduced.

`EventId.fromJson` on `gambol.meta` is persist restore, so it fits [[.agents/rules/core-api.md]] EventId serial (“only serializing”). `EventId.next` is not in this diff.

Plan markdown in the hunks follows [[.agents/rules/markdown-writing.md]] (one blank between blocks; named links).

### 2. Baseline smells ([[SMELLS.md]] — always judgement)

**Primitive Obsession** — [BootCache.fs](src/Shared/BootCache.fs) rename keeps `int`:

```
        (snapshotEventId: int)
        (clientEventId: int)
        : bool =
        logLength > maxLogLength
        || (clientEventId - snapshotEventId) > maxRevGap
```

**Mysterious Name** — same hunk: leftover `maxRevGap` beside EventId locals.

**Primitive Obsession** — [SavePrep.fs](src/Server/SavePrep.fs) `getFileEventId` is `EventId`, then `return Ok (EventId.value …)` and the function still returns `int`. Tests bind that `int` as `eventId` and compare `state.eventId.Value`.

**Shotgun Surgery** — Revision→EventId peel across Server/Shared/tests. Requested rename; repo override, suppress.

**Parameter Explosion** — `syncDataDir` still six args. Pre-existing; surgical leave-alone.

## Spec

### 1. Missing or partial

**Database F# still talks Revision (column leave is fine).** Spec **In:** “Server Api/Core/Database peels that still say Revision.” Comments: “PostgreSQL `graph.revision` column stays; that is a schema name, not the EventId type.” Column stay is correct. Still in-process ints named revision: `GraphSingletonRow.revision`, `tryLoadGraphFromProjection`, `replaceGraphProjectionWithTx`, `DatabaseProjection.GraphPatch.revision`.

**BootCache truncate is a rename, not EventId.** What to build: “One serial id type everywhere wire/API/State already talks revision.” `shouldTruncate` params are `snapshotEventId` / `clientEventId` but still `int`; `maxRevGap` / `maxPollRevGap` and `FallbackState "revision"` unchanged.

History/EventLog/EventJson/`getEventId`/`EventId.next`/JSON `"eventId"`/core-api rule are already on HEAD from [11 — One serial event id](../issues/11-one-serial-event-id.md). Green bar tests touched are Shared/Server only.

### 2. Scope creep

No ClientHistory / SyncPlanner / SyncLogic / Browser `record`/approve (Out of scope 1). No `type Revision` / `ofRevision` deletes (Out of scope 2).

Plan-only extras not asked by 14: [13 — Revision always 0 (diagnostic)](../issues/13-revision-always-zero.md) `defined` → `done` (Alan).

### 3. Looks done, looks wrong

**`shouldTruncate` name vs type.** Same “One serial id type…” line. `decideBootPoll` already takes `EventId`; truncate still subtracts `int`.

**Meta file write peels with `value`.** **In** includes “core-api EventId serial rule.” `readEventId` uses `EventId.fromJson`; `writeEventId` writes `string (EventId.value eventId)`, not `toJson`.

## Summary

Standards: 0 hard; worst judgement Primitive Obsession on BootCache/SavePrep ints. Spec: 0 wrong lands; worst leftover Database F# `revision` fields bound to SQL.
