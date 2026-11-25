# 06 — Investigation: children missing on the first connect after idle

Status: candidate found, pending confirmation against a captured occurrence
Scope: [06 — Children missing on the first connect after idle](../06-children-missing-on-connect.md)

This note walks the whole path from server start to a client's first render, ranks the candidate causes the ticket named, and adds one mechanism the ticket's own evidence points to but did not spell out. It does not change any code path. A reproducing test for the top candidate lives in `tests/Shared.Tests/EventTests.fs`, skipped so the suite stays green.

## How a restart reaches a client's first render

- `DbAgent.createWithLiveSave` (`src/Server/Core/DbAgent.fs`) calls `Database.loadPersistedState`, which reads the normalized projection tables through `tryLoadGraphFromProjection`. That read runs `GraphProjection.graphFromPersistence` (`src/Shared/GraphProjection.fs`), an all-or-nothing check over the whole projection — every parent's child ordinals must be dense `0..n-1`, or the whole read returns `Error`. On `Error`, `loadPersistedState` falls back silently to `Graph.create (), EventId.zero`.
- `DbAgent.createLoaded` then calls the private `loadReconciled`, which fetches every row ever stored (`Database.getEvents` has no `WHERE` clause on event id) and decodes each one with `EventLogFile.decodeEvent`. `List.choose` keeps only the rows that decode; a row that fails to decode is dropped from the replay log before `EventLog.recover` ever sees it.
- `EventLog.recover` (`src/Shared/EventLog.fs`) then applies every event still ahead of the starting checkpoint, oldest first, through `Ev.apply`. An event whose `Op.Replace` span no longer matches the state it is applied against comes back `ApplyResult.Invalid`; `applyRecover` keeps the state unchanged but still advances `eventId` past that event — the same silent-drop shape as a decode failure, one step later.
- This whole sequence runs synchronously, before the mailbox exists. `loaded.state.Value` holds its result from the first moment any query can reach the mailbox.
- The mailbox then starts with `CoreMailboxLoop.startWithPrelude`, running `DbAgent.liveStartupSweep` (`DatabaseProjection.startupSweepPatch` through `executeMaintenance`) as its `until` prelude. While that prelude is not complete, `startWithPrelude`'s `startupLoop` answers only the messages `CoreMailboxLoop.isStartupRead` accepts — `Query.GraphAtEvent`, `Query.LastEventId`, `Query.EventsSince` — straight off the queue, ahead of every other message.
- Once the sweep reports its result, `DbAgent.applyMaintenance` reads the projection tables fresh a second time (`readProjection`) and replaces `loaded.state.Value`, either directly (`result.requiresReload`, when the sweep changed anything) or through a second `EventLog.recover` pass (`reloadRecovered`, when it did not). Only after this step does the prelude finish and the mailbox call `loaded.ready.TrySetResult()`.
- `Api.getState` (`src/Server/Api.fs`) answers a connect's `/state` request with `CoreDoor.graphAtEvent handle`, trimmed for the default `BootstrapScope.RootClosure` scope through `ResidentProjection.visibleClosureGraph`.
- `Program.fs`'s client boot path (`loadFromState`) always fetches `/state` fresh and renders it directly through `finishPaint`.

## Ranked candidates

### 1. A cold restart's fallback replay leaves a Node wrongly empty, and a connect reads that memory before the startup sweep heals it

This candidate joins two paths the ticket named separately — `DbAgent.loadReconciled`'s silent drop and `CoreMailboxLoop.isStartupRead`'s race — into one chain that accounts for every observed detail: only some Nodes affected, no event in the log that ever emptied a list, and a fix with no code change and no event between the bad connect and the good one.

Mechanism:

- An ordinal-density violation anywhere in the projection (`GraphProjection.graphFromPersistence`'s dense-ordinal check) sends `loadPersistedState` down its fallback to `Graph.create (), EventId.zero`. `loadReconciled` must then replay every stored event from the start, instead of a short catch-up tail.
- A full replay touches far more history than a normal restart does, and `DbAgent.liveStartupSweep`'s own log line (`DbAgent: projection repair deleted=… ownershipUpdates=… insertNodes=… insertChildren=… ordinalShifts=…`) shows that ordinal drift in ROOT's own children is a real, already-repaired, recurring condition in production — confirming the trigger exists, though not yet confirmed to reach a non-root parent such as LAPS or the ambit Workspace Node itself.
- Somewhere in that full replay, one stored event that populated a parent's children — LAPS's or ambit's — either fails `EventLogFile.decodeEvent` or comes back `ApplyResult.Invalid` against the replay's own state. Either way `EventLog.recover` drops it silently and moves on. That parent keeps the empty `childMap` entry every freshly-created Node gets from `Graph.addDetachedNode`: Parsed, with an empty child list, the solid-circle bullet (`ViewModelChildrenIndicator.rowChildrenIndicator`).
- This wrong state lives only in `loaded.state.Value`, in memory. Nothing in this path writes back to Postgres: `DatabaseProjection.plan`'s `nodeUpserts` and `childReplacements` are scoped to the Node ids a *live* batch's own `Op`s name, so a dropped historical event never touches LAPS's or ambit's stored `node_children` rows. project.md's "Findings" section is exactly this: the rows stayed correct in Postgres throughout.
- Within the same boot, `DbAgent.liveStartupSweep`'s repair runs against Postgres directly (reading `nodes`/`node_children` fresh, not the in-memory graph) and, if it clears the ordinal violation, `DbAgent.applyMaintenance` reads the projection a second time and replaces `loaded.state.Value` with the now-correct graph — before `loaded.ready.TrySetResult()` runs. A connect that reaches `Api.getState` after that point sees the healed graph.
- A connect that reaches `Query.GraphAtEvent` *before* that point — which `CoreMailboxLoop.isStartupRead` allows, because it answers reads straight off the queue while the prelude is still running — sees the wrong, pre-heal graph. That window only exists for the few seconds after a restart, which is why only the first connect after an idle-triggered restart can ever see this, and why a later connect to the same, still-running server does not.

Evidence:

- `DbAgent.liveStartupSweep`'s log line shows ordinal drift in production, already self-repairing, on every boot.
- `tests/Server.Tests/DbAgentTests.fs`'s `DbAgent missing ROOT fails closed while reads stay available` shows a *harder* sweep failure wedges the mailbox rather than healing — distinguishing a permanent failure from this candidate's transient one, and matching why issue 07's real data loss (a Save on an Unparsed Node) needed a user action to happen, while this bug did not.
- `src/Server/Core/CoreEventDispatch.fs`'s `persistNew` / `store` / `withConfirmedOps` show the event written to Postgres already carries the amended ops a live post settled on, not the client's raw submission — so a clean replay is normally deterministic, and the gap needs a genuine decode or precondition failure to open, not routine amendment.

A reproducing test for this mechanism follows the ranked candidates, in "Reproducing test".

Proposed fix direction: make `EventLog.recover` surface a dropped event instead of swallowing it — return the failures alongside the recovered state, and have `DbAgent.loadReconciled` refuse readiness (the way a hard sweep failure already does) rather than publish a Graph with a silently shortened history. Pair that with a loud failure from `Database.loadPersistedState`'s `Error` branch instead of the silent fallback to an empty Graph, so an ordinal-density violation is a startup incident, not a quiet full replay.

What would confirm or rule this out: the `DbAgent: projection repair …` log line's timestamp against the Nov 23 connect's timestamp (a repair logged a few seconds before the connect matches this candidate), and whether the browser console's `decodeStateResponse` line for that connect reports zero children for LAPS and ambit specifically — the ticket's own "Evidence to capture next time" section names both.

### 2. CoreMailboxLoop.isStartupRead lets a connect through before the startup sweep finishes, on its own

`Query.GraphAtEvent`, `Query.LastEventId`, and `Query.EventsSince` are genuinely answered from `loaded.state.Value` while `DbAgent.liveStartupSweep`'s prelude is still running — this is real and confirmed by reading `CoreMailboxLoop.startWithPrelude`'s `startupLoop`. On its own, without candidate 1's replay failure, this window should not produce an emptied child list: the state it serves is the result of `DbAgent.loadReconciled`'s normal incremental catch-up, which `src/Server/Core/CoreEventDispatch.fs` shows applies deterministically-amended ops against a projection that was itself consistent at commit time. Ranked second because the race is confirmed, but needs candidate 1's replay failure, or some other mutation this investigation did not find, to have anything wrong to serve.

Proposed fix direction: covered by candidate 1's fix — once a replay failure cannot silently publish a wrong Graph, this race has nothing left to expose.

What would confirm or rule this out: the same timing evidence as candidate 1; this candidate adds nothing beyond it.

### 3. Api.getState's RootClosure scope trims through visibleClosureGraph instead of the complete-subtree bootstrapGraph

`Api.getState`'s default scope (`BootstrapScope.RootClosure`) calls `ResidentProjection.visibleClosureGraph`, not the complete-subtree `ResidentProjection.bootstrapGraph` / `rootBootstrapGraph` that `src/Client/BootCache.fs` and `tests/Shared.Tests/BootstrapScopeTests.fs` treat as correct. `ResidentProjection.wantAnswer`'s `package` step folds a missing `childMap` entry into an empty list (`GraphChildren.tryGet graph id |> Option.defaultValue []`) for anything in the requested `want` set, which would turn a genuinely Unloaded Node into the same solid-circle bullet this bug shows.

This is a real code gap, but it needs the server's own Graph to already carry a true Unloaded entry for LAPS or ambit, and normal server operation never produces one: `GraphProjection.graphFromPersistence` and `Graph.addDetachedNode` both always populate a `childMap` entry, even `[]`, so `GraphChildren.tryGet` has nothing to default on a healthy Graph. `tests/Server.Tests/ApiGetStateTests.fs`'s `getState without zoom keeps nested Workspace Unloaded` also shows the default, no-zoom connect cannot even reach a nested Workspace Node — the client's saved `?zoom=` session hint has to point at or inside LAPS or ambit for either Node to appear in the response at all, which narrows which connects could show this bug regardless of cause.

Ranked third: real, but inert without an upstream source that is already wrong, and candidate 1 already supplies a Node whose source Graph entry is `[]` without needing this fold at all.

Proposed fix direction: route `Api.getState`'s `RootClosure` scope through the same complete-subtree function `BootCache.fs` already trusts, so a genuinely Unloaded Node keeps its hollow-circle bullet across the wire instead of becoming indistinguishable from a Loaded-empty one.

What would confirm or rule this out: whether the saved `zoom` query parameter on the Nov 23 connect pointed at or inside LAPS or ambit; if it did not, this candidate cannot have played any part.

### 4. Client BootCacheStore paints a stale snapshot before the fresh /state response — ruled out

`src/Client/Program.fs`'s boot sequence (`loadFromState`) always fetches `/state` fresh and renders it directly through `finishPaint`; `BootCacheStore.persistAfterState` writes the cache only after that render. A repository-wide search for `BootCacheStore.readCache`, `BootCache.decideBootRead`, and `BootCache.decideBootReadWait` finds no call site outside `src/Shared/BootCache.fs` itself and its tests (`tests/Shared.Tests/BootCacheTests.fs`) — nothing in the live boot path ever reads the cache back before painting. Ruled out, matching the ticket's own note that this needs confirming against the deployed build rather than assumed from the code alone.

## Reproducing test

`tests/Shared.Tests/EventTests.fs`'s `` `recover silently drops a stale Replace and freezes the parent's children empty` `` reproduces candidate 1's core mechanism at the `EventLog.recover` seam, skipped (`Fact(Skip = …)`) so the full suite stays green:

- It builds a Node standing in for LAPS, freshly created and so Loaded-empty, the same as every Node `Graph.addDetachedNode` creates.
- It gives that Node two stored events: the one that would have given it three real children, with a span that no longer matches a cold start, and a later one that only re-confirms those same three children.
- `EventLog.recover` advances past both — the recovered `eventId` reaches the log's tip, and neither event is an emptying Change, matching project.md's "Findings" section that no event in the production log ever emptied LAPS's or ambit's list.
- The recovered Graph still has that Node Parsed with an empty child list — the solid-circle bullet — instead of the three children the log faithfully describes.

Running it unskipped fails on exactly that last assertion:

```
Assert.Equal() Failure: Collections differ
Expected: [A, B, C]
Actual:   []
```

This confirms the mechanism — a stored event whose precondition no longer holds at replay time is silently dropped, and nothing downstream of that drop reports it. It does not confirm that this mechanism, rather than another one, produced the Nov 23 connect; only a captured occurrence (the sweep's log line and the connect's saved zoom, per the two candidates' own "What would confirm or rule this out" notes) can do that, per the ticket's own stop condition.

## See also

[[../../project.md]], [[../../evidence/investigation-report.md]]
