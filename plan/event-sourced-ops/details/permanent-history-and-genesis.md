# Permanent global history and genesis

**Status:** **proposed** — charting extension to address a critical as-implemented flaw. Not software yet.

## The flaw (fact)

A **new server process** can leave an already-open Browser page unable to submit work. The user sees a stale-client outcome — `DataOutdated`, `ServerRejected`, or the blocking sync-risk overlay — and must reload, losing unsaved pending work.

Grounding in today's code:

- **Startup loads current state from the DB projection**, not by replaying the Change log. [[src/Server/Database.fs]] `loadPersistedState` reads `graph` + `revision`; [[src/Server/DbAgent.fs]] holds that as `initialState`.
- **The Change log is not permanent today.** The only code path that truncates `changes` is file-mode bootstrap/migration: [[src/Server/Database.fs]] `rebuildFromDocumentFiles` (`TRUNCATE changes RESTART IDENTITY CASCADE`) when the DB is empty or disk and DB diverge ([[src/Server/DatabaseSetup.fs]]). That path re-seeds the projection from parsed document files — **not** the proposed recovery model. When the log is discarded, revision and graph can still match while the append-only tail is gone.
- **Poll detects drift.** [[src/Shared/SyncLogic.fs]] `getPollOutcome` sets `DataOutdated` when `poll.revision > clientRev` and there is no local pending work — the common case after the server moved on without the page.
- **Submit can still reject.** [[src/Client/Update.fs]] maps failed apply / reconcile to `ServerRejected` and wipes pending. [[src/Client/Overlays.fs]] describes "revision mismatch or invalid op". Pending Ops planned against a graph the server no longer shares fail compare-and-swap even when amendment would have helped.
- **Protocol change is separate.** `CodeOutdated` when build stamps differ ([[src/Shared/SyncLogic.fs]]) — a forced reload remains correct and is not solved here.

This is **behavior to beat**, not the standard. It conflicts with merge-not-refuse and with [[.scratch/selective-client-loading/spec.md]] story 46 (mark stale and refresh without login when server restarts).

## Proposed resolution

| Condition | Outcome |
| --- | --- |
| Server **post protocol** changes (wire shape, Client decode contract) | Forced reload — `CodeOutdated` or equivalent. No escape in this increment. |
| Server restarts or a **new instance** with **unchanged** protocol | **Do not** treat the Browser as stale merely because process-local state reset. |
| Otherwise | Make the **global Change log permanent** (persisted, not truncated on restart). |

## Goal outcome

A new server process or version **does not demand a Client restart** when protocol is unchanged. The Server presents **consistent state** with what was live before the reset — same graph, same global revision, same retained Change tail. Open Browsers catch up through poll / merge, not through a forced reload.

**Client acceptance policy (proposed):**

- **Default: accept.** An already-open Browser keeps working. Stale-client rejection on restart alone is behavior to beat.
- **Explicit fail points only.** We reject only where we deliberately code it — post-protocol mismatch (`CodeOutdated`), malformed requests, auth failure, and similar. No implicit "new process = stale Client."
- **Short-term transitions only.** The Server generates Browser assets. Deployments do not leave ancient Clients in the wild; we only maintain compatibility across the **current and immediately prior** build. Transition work is **keep state and wire shape consistent** so the previous Client does not break — not a long tail of version adapters.
- **Forced reload stays for protocol change.** When the post envelope or Client decode contract changes, reload remains correct and unavoidable in this increment.

## System model (proposed)

**Current state** — the authoritative Graph and global revision — **loads from the DB** on startup, as today. The permanent log does not replace the projection as the hot read path.

**Permanent log store (proposed)** — the existing PostgreSQL append-only `changes` table in [[src/Server/Database.fs]] (schema, `appendChange` / `appendChangeWithTx`, `getChangesAfterCheckpointRevision`). **Recommendation:** keep (or evolve) that table as the permanent global Change log — not a new file log, Redis stream, or separate store. It is already the as-implemented append-only log; the work is retention across restart / recovery, not inventing a second store.

**Permanent log** — every accepted Change is retained in arrival order with `server_revision_after`. Poll / Load tails read from this log (`getChangesAfterCheckpointRevision` already has the right shape). The log is **not** a disposable short tail cleared on server restart or recovery.

**Recovery / rebuild (proposed)** — load **DB projection + permanent Change log** (same PostgreSQL database: projection tables + `changes`). Do not re-parse document files to rebuild state. Document-file bootstrap/migration (`rebuildFromDocumentFiles`) is out of scope for this model.

**Genesis** — the Graph state at revision 0 of the permanent log (the moment the log was first instituted for this deployment). It is **not** replay-from-empty through historic parsers (that stays rejected — [[.scratch/relaxed-concurrency/map.md]]). Genesis is **derivable**: walk the global sequence backward, inverting each Change (the same inverse Ops Undo already uses — [[details/undo.md]]), until the first entry. Nobody plans to do this routinely; the capability exists for recovery, audit, and future tooling.

**Client catch-up** — unchanged in spirit: rewind to baseline, replay a **short tail** from the permanent log ([[client-consume.md]]). Permanent storage does not mean every Client replays from genesis on every poll.

**Load packages** — stay Graph / state transfer, not Ops replay ([[architecture.md]]). Permanent log does not turn Load into genesis replay.

## What this is not

- **Not** log-as-truth Event Sourcing: the DB projection remains the record for `getState` and residency.
- **Not** a new log store: file log, Redis, or a second append path — **proposed** store is the existing (or evolved) PG `changes` table.
- **Not** routine genesis replay or retained historic parsers.
- **Not** a guarantee that a Browser never reloads — only that **process restart alone** must not orphan submissions when protocol is unchanged.

## Relation to other pins

- **Merge and amendment** ([[merge-invariant.md]], [[messaging.md]]) still apply; permanent history makes the sequence durable across processes.
- **Unrestricted Undo** ([[undo.md]], issue 12) becomes more meaningful when the global sequence is complete and retained; desirability is still open.
- **Relaxed concurrency** build-upon layer: genesis *replay through parsers* stays rejected; genesis *derivable via invert* is the new nuance ([[relation-to-relaxed-concurrency.md]]).

## Open (this doc)

- Exact migration: institute permanent log on existing deployments without truncating; mark genesis revision; handle pre-permanent snapshots.
- When an explicit new-genesis boundary is allowed (migration only) vs forbidden (ordinary restart/recovery).
- Server-restart signal to Clients: passive poll vs explicit `isReady` / stamp without `CodeOutdated`.

## Implementation pointer

Tracked in [[../issues/15-permanent-global-change-log.md]].
