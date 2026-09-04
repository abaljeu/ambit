# Robust outliner

Stage: charting

Integrity and correctness of the outline: ownership, parse/load state, UI seams, Change/History spine. First use of a User Epic may already need a slice of this; the rest is a home for robustness work.

This Epic homes Projects. There are no Chapters to chart.

## Inner core

Lately bugs can stop all use until they are fixed. This Epic also aims at runtime isolation on the Server:

- A tight inner core never crashes.
- That core is modular. Each piece is clearly understood.
- Everything else asks the core to apply Changes. See [[plan/event-sourced-ops/overview.md]].
- If everything else hangs, abort it. The Graph is unharmed.
- If everything else crashes, the core does not crash.

## Incremental operations

Aim for incremental work. Send a modest amount, then send more. Do not communicate a large amount of data in one shot.

Today these are not very incremental:

- Upload of a Workspace
- Load of a Workspace into the Browser

Incremental send also lets a hang abort mid-stream without a full redo.

## Required for done

The Epic is not done until each item is done (or the named part).

Live:

- [ ] [[plan/owner-edge-db-repair/project.md]]
- [ ] [[plan/parse-load-demote/project.md]]
- [ ] [[plan/rowview-layout-behavior/project.md]]
- [ ] [[plan/event-sourced-ops/project.md]] — remainder beyond Agent-chat Actor issues
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
