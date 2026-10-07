# Robust outliner

Stage: charting

Integrity and correctness of the outline: ownership, parse/load state, UI seams, Change/History spine. First use of a User Epic may already need a slice of this; the rest is a home for robustness work.

Current chapter: [[chapters/actors-supported.md]]

## Chapters

- [[chapters/initial-core.md]]
- [[chapters/actors-supported.md]]
- [[chapters/acid-apply.md]]
- [[chapters/incremental-operations.md]]
- [[chapters/rowview-layout-vs-behavior.md]]

## Solid core

Near-term sequencing for the Solid core bar. **v1 built** (Alan 2026-10-07): [[plan/core-creation/project.md]] — Core baseline and managed Actor pool (Chapter [[chapters/initial-core.md]] met; Project Stage `done`). **v2 live:** [[plan/core-refinement/project.md]] — Core-seam revision; sole authority [[plan/core-refinement/arch.md]]. Leftover v1 tickets continue as pointers [08](../../core-refinement/issues/08-pointer-core-actor-pool.md)–[17](../../core-refinement/issues/17-pointer-prove-testactor-hello.md).

Sequence: Initial Core ([[chapters/initial-core.md]], done) → Actors supported ([[chapters/actors-supported.md]], current) → ACID apply → incremental operations. Core v2 work continues on core-refinement while later Chapters run.

### Bar (not process crash isolation)

- **Core and managed Actor pool (v1)** — [[plan/core-creation/project.md]], sequenced by [[chapters/initial-core.md]] (met).
- **Core-seam revision (v2)** — [[plan/core-refinement/project.md]] (live).
- **ACID apply** — [[chapters/acid-apply.md]].
- **Incremental work** — [[chapters/incremental-operations.md]].
- **Not required for this bar:** process-level crash isolation (outer work crashing must not take down a separate core process). Full Workspace Upload/Load redesign and every connector stay parked.

## Incremental operations

Aim for incremental work. Send a modest amount, then send more. Chapter: [[chapters/incremental-operations.md]].

The Browser starts small (visible-closure) and grows by auto wants: visible Nodes that miss Children first, then those Children; a hollow-circle Bullet until fill. Auto wants need no click and no command. Homes: [[plan/browser-residency/project.md]] (Stage done 2026-10-07), [[plan/parse-thread/project.md]], [[plan/transport-layer/project.md]] (file transit). Conflict resolution is already implemented. Do not re-plan it.

## Required for done

The Epic is not done until each item is done (or the named part). Chapter checklists are not repeated here.

Live:

- [ ] [[plan/event-sourced-ops/project.md]] — remainder beyond the Parse definition in [[chapters/actors-supported.md]], including advisory soft-lock behavior
- [ ] [[plan/single-event-source/project.md]] — Event as the sole durable source of truth for Graph history and apply
- [ ] [[plan/architecture/map.md]] — remainder of the architecture wiki; portions about other Epics gate those Epics
- [ ] [[plan/debug-reload/project.md]] — architecture documentation: debug modules and esbuild hard-reload
- [ ] [[plan/end-user-wiki/map.md]] — portion for this Epic (not yet filed)
- [ ] [[plan/marketing-wiki/map.md]] — portion for this Epic (not yet filed)

Done:

- [x] [[plan/split-node-trace/project.md]] — Poll/sync recoverable mismatch consume ([01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](plan/split-node-trace/issues/01-poll-sync-cas-undo-all-pending-apply-merge.md))
- [x] [[plan/class-toggle-keys/project.md]] — keybinds toggle cssClasses `b`, `i`, and `check` on the current Selection ([01 — Toggle cssClasses b, i, and check from keys](plan/class-toggle-keys/issues/01-toggle-cssclasses-b-i-check-from-keys.md))
- [x] [[plan/delete-ref/project.md]]
- [x] [[plan/fix-settext-system-css-resilience/project.md]]
- [x] [[plan/glossary-directory-file/project.md]]
- [x] [[plan/search-zoom-select/project.md]]
- [x] [[plan/relaxed-concurrency/project.md]]
