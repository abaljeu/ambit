# Robust outliner

Stage: charting

Integrity and correctness of the outline: ownership, parse/load state, UI seams, Change/History spine. First use of a User Epic may already need a slice of this; the rest is a home for robustness work.

This Epic homes Projects. There are no Chapters to chart.

## Solid core

What “solid product core” means for this Epic (locked 2026-09-04). Near-term strategy on [[../map.md]] points here; this section is the shape.

### Bar (not process crash isolation)

- **ACID apply** — an accepted Change is durable iff apply succeeds: one commit for amend + log + projection. No timeout-abandon that persists after a refused apply; no non-Change startup writers that bypass the path.
- **Managed actor pool** — long-running work launches off the apply queue with job identity and cancel; Actors finish by sending Changes into **inner apply** (no HTTP self-post). See [[plan/event-sourced-ops/details/actors-and-jobs.md]] and issues 07–08.
- **Modular, understood pieces** — a tight apply core; everything else asks that core to apply Changes ([[plan/event-sourced-ops/overview.md]]). If an Actor hangs, abort it; the Graph stays consistent.
- **Not required for this bar:** process-level crash isolation (outer work crashing must not take down a separate core process). Full Workspace Upload/Load redesign and every connector stay parked.

Persistence: **file mode is view-only** (rollback). **db mode** is the authority path for ACID apply and kernel work.

### Four-call surface

Internal Actors and the Server share one small API; no side door into the Graph:

1. **Files** — send / get files
2. **Changes** — send / get graph Changes
3. **Query** — query graph elements (read; no change)
4. **Command** — run command (name, selection)

### Kernel candidate today

The Shared apply/amend stack is the small kernel candidate (~apply path on the order of a couple thousand lines inside a much larger codebase). Persistence, parse, and upload are hard to fold into that kernel without redesign; leave them outside until the bar above is met. Full inventory: [[../reports/solid-core-module-fit.md]].

Rough establish effort (focused): ~1–2 weeks ACID apply cleanup + ~2–3 weeks managed actor pool ≈ **~3–5 weeks** for four-call surface + ACID path + pool Actors share. Not months.

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
