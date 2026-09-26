# Robust outliner

Stage: charting

Integrity and correctness of the outline: ownership, parse/load state, UI seams, Change/History spine. First use of a User Epic may already need a slice of this; the rest is a home for robustness work.

Current chapter: [[chapters/initial-core.md]]

## Chapters

- [[chapters/initial-core.md]]
- [[chapters/actors-supported.md]]
- [[chapters/acid-apply.md]]
- [[chapters/incremental-operations.md]]
- [[chapters/rowview-layout-vs-behavior.md]]

## Solid core

Near-term sequencing for the Solid core bar. Detailed Core design and implementation ownership are in [[plan/core-creation/project.md]].

Sequence: establish Core through [[chapters/initial-core.md]], support the first Actor definition through [[chapters/actors-supported.md]], then continue with ACID apply and incremental operations.

### Bar (not process crash isolation)

- **Core and managed Actor pool** — [[plan/core-creation/project.md]], sequenced by [[chapters/initial-core.md]].
- **First Actor definition** — Parse through [[chapters/actors-supported.md]].
- **ACID apply** — [[chapters/acid-apply.md]].
- **Incremental work** — [[chapters/incremental-operations.md]].
- **Not required for this bar:** process-level crash isolation (outer work crashing must not take down a separate core process). Full Workspace Upload/Load redesign and every connector stay parked.

## Incremental operations

Aim for incremental work. Send a modest amount, then send more. Chapter: [[chapters/incremental-operations.md]].

The Browser starts small and grows from want (hollow-circle Bullet, or Find of a not-yet-Resident hit then Fetch). The Server Parse Actor turns file-shaped disk into Graph and emits Changes. Post-Event and Poll carry Changes plus wanted Nodes. Workspace Download and Upload stay file operations; they do not Parse on the Browser or the App. Graph→Browser is visible-closure.

Conflict resolution is already implemented. Do not re-plan it.

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

- [x] [[plan/delete-ref/project.md]]
- [x] [[plan/fix-settext-system-css-resilience/project.md]]
- [x] [[plan/glossary-directory-file/project.md]]
- [x] [[plan/search-zoom-select/project.md]]
- [x] [[plan/relaxed-concurrency/project.md]]
