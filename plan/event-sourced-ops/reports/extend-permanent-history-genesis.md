# Extend permanent history and genesis — report

**Date:** 2026-08-23  
**Branch:** `w/event-sourced-ops` (cut from `selective-client-sync`)  
**Scope:** Documentation only — no code changes.

## Problem grounded in code

A new server process can orphan open Browsers:

1. [[src/Server/Database.fs]] `loadPersistedState` loads graph + revision from the DB projection (not log replay).
2. Today the `changes` table can be truncated on file-mode bootstrap/migration ([[src/Server/DatabaseSetup.fs]] → `rebuildFromDocumentFiles`) — not the proposed DB+log recovery path.
3. [[src/Shared/SyncLogic.fs]] `getPollOutcome` → `DataOutdated` when server revision > client revision (no pending).
4. [[src/Client/Update.fs]] / [[src/Client/Overlays.fs]] → `ServerRejected` on failed apply/reconcile ("revision mismatch or invalid op").

`CodeOutdated` (build stamp drift) is a separate forced-reload path — unchanged by this proposal.

## Docs changed

| File | Change |
| --- | --- |
| [[overview.md]] | Nuanced "What this is not": parser/genesis replay still rejected; proposed permanent log + derivable genesis |
| [[details/permanent-history-and-genesis.md]] | **New** — flaw, proposed model, boundaries, open migration questions |
| [[details/relation-to-relaxed-concurrency.md]] | Permanent log nuance; log-as-truth still rejected |
| [[details/as-implemented-facts.md]] | File-mode bootstrap/migration truncates log — behavior to beat |
| [[details/open-questions.md]] | Permanent log under **Still proposed** |
| [[details/decision-log.md]] | Round 9 — permanent global history (proposed) |
| [[details/client-consume.md]] | Short-tail vs derivable genesis distinction |
| [[details/undo.md]] | Cross-link permanent log to unrestricted Undo question |
| [[project.md]] | Issue 15, new detail link, summary/date |
| [[issues/15-permanent-global-change-log.md]] | **New** implementation issue |
| [[../index.md]] | Event-sourced-ops summary row |

## Key design decisions captured

1. **Permanent global Change log** — append-only `changes` survives server restart; not truncated on ordinary restart/recovery.
2. **Store (proposed):** PostgreSQL `changes` table in [[src/Server/Database.fs]] — already the as-implemented append-only log (schema, append, `getChangesAfterCheckpointRevision`). Recommend keep/evolve; not a new file log, Redis, or second store. Status remains **proposed** (user said "I think"); grounded in code.
3. **Current state from DB** — projection remains hot path for `getState`; not log-as-truth Event Sourcing.
4. **Recovery from DB + log** — rebuild loads projection + permanent log; not re-parse from document files.
5. **Genesis derivable, not routine** — invert-walk to first log entry; not replay-from-empty through historic parsers (relaxed-concurrency rejection stands).
6. **Client catch-up unchanged** — short-tail rewind+replay; Load packages stay Graph transfer.
7. **Protocol change → reload** — `CodeOutdated` / post-protocol mismatch still forces refresh.
8. **Implementation** — issue 15; genesis boundary policy for migration still open.

## Follow-up — PG store pin (2026-08-23)

User: "I think the log is best stored as a PG table?" — **recommended yes**: it already is. Docs updated to state the permanent log store explicitly as existing/evolved PG `changes` (still **proposed**). Files: [[../details/permanent-history-and-genesis.md]], [[../issues/15-permanent-global-change-log.md]], [[../details/decision-log.md]] Round 9, this report.

## Correction (2026-08-23)

Earlier drafts tied the stale-client flaw and the permanent-log solution to `rebuildFromDocumentFiles`. That was misleading.

- **Proposed recovery/rebuild** loads from **DB projection + permanent Change log**. It does **not** re-parse document files. `rebuildFromDocumentFiles` is not in view for that model.
- **Today only:** `rebuildFromDocumentFiles` is file-mode bootstrap/migration (empty DB or disk/DB mismatch). It truncates `changes` and re-seeds the projection from files — a separate, as-implemented path documented in [[../details/as-implemented-facts.md]].

Docs updated: [[../details/permanent-history-and-genesis.md]], [[../issues/15-permanent-global-change-log.md]], [[../details/as-implemented-facts.md]].

## Stage

Project remains **active**; no stage transition.

## Follow-up (goal outcome)

User refinement after initial pass — incorporated into [[overview.md]], [[details/permanent-history-and-genesis.md]], [[details/decision-log.md]] Round 9, and [[issues/15-permanent-global-change-log.md]]:

- Server restart / new version → **no forced Client reload** when protocol unchanged; **consistent state** with pre-reset.
- **Old Clients accepted** by default; reject only at **explicit fail points**.
- Server generates Browser code → only **short-term transition** compatibility; coding = **keep state and protocols consistent** so the previous Client does not break.
