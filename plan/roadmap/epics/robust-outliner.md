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

Aim for incremental work. Send a modest amount, then send more. Do not communicate a large amount of data in one shot. Chapter: [[chapters/incremental-operations.md]].

Today these are not very incremental:

- Upload of a Workspace
- Load of a Workspace into the Browser

Incremental send also lets a hang abort mid-stream without a full redo.

## Required for done

The Epic is not done until each item is done (or the named part). Chapter checklists are not repeated here.

Live:

- [ ] [[plan/event-sourced-ops/project.md]] — remainder beyond the Parse definition in [[chapters/actors-supported.md]], including advisory soft-lock behavior
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
