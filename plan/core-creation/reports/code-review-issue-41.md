# Code review — [41 — Migrate Core mailbox, CoreMsg, and Pool onto Event](../issues/41-migrate-core-mailbox-coremsg-and-pool-onto-event.md)

Range: uncommitted working tree vs `HEAD`.

## Standards

### Hard violations

**[[plan/core-creation/reports/issue-41-seam-audit.md]]** — [[.agents/rules/refer-by-name.md]] bare ids — **fixed** (arch / Event-abstraction checklist points named; out-of-scope and blocked-by links use full ticket titles).

## Spec

### (a) Missing / partial

1. **Core `PostChange` path not on `postEvent`.** — **addressed** ([[issue-41-postchange-postevent-fix.md]]). `CoreMailbox.postChange` / `coreChanges.postChange` build Events at the door and loop `postEvent`. Graph Changes via that door appear on EventLog / `eventHistory`. `postGraphOnlyChange` also goes through Event flow and EventLog, skipping only file persistence.

2. **EventLog incomplete for remaining Change posts.** — **addressed** with (a)1. Empty `postChange []` still hits persist rejection via CoreMsg `PostChange` with `[]` (unchanged-submission path).

### (b) Scope creep

1. **New `PostEvents` CoreMsg case** — **removed** as scope creep ([[issue-41-postchange-postevent-fix.md]]). Change lists use single `postEvent` calls.

### (c) Looks done but wrong

No hard Spec-wrong hits on the checklisted behaviours that *were* implemented: name-only Undo/Redo fill via `tryFind` + `inverseOps` (same `submissionId`), ActorStart/Stop mailbox append (callers rejected from `postEvent`), start payload as `ActorStart`, authority overwrite from admitted Caller. ActorStart/Stop persist deferred to 42 is correctly out of scope.

**Covered:** §1 name-only Undo/Redo (via `postEvent`); §2 EventLog reply; §3 ActorStart/Stop + `ActorStart` request; §4 authority stamp; Core Changes → Event at door via `postEvent`.

---

**Summary:** Standards bare-id findings fixed earlier. Spec (a)1–2 addressed by routing Core `postChange` through `postEvent`; (b)1 addressed by removing `PostEvents`.
