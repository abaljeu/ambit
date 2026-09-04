# Architecture

Labels: wayfinder:map

## Destination

A browsable description of how Gambol is coded and how it runs: processes, layers, data flow, and where behavior lives (Browser, Server, App, Shared, Document).

## Notes

- Charted from [[plan/roadmap/map.md]] after [[plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md]].
- [[doc/arch.md]] is a thin engineering overview. [[doc/current/]] holds feature baselines. [[doc/agents/domain.md]] says where canonical docs live. This Project is the standing effort to describe coding and runtime, not a second source of feature truth.
- Sister Projects: [[plan/end-user-wiki/map.md]] (what the software is for users), [[plan/marketing-wiki/map.md]] (uses).
- Related: [[plan/debug-reload/project.md]] -- how a person on watch loads debug modules and picks up an esbuild rebuild (Browser hard-reload). Homed on [[plan/roadmap/epics/robust-outliner.md]].

## Decisions so far

- Goal is how it is coded and how it runs, not user how-to and not use-case marketing.

## Not yet specified

- Home: expand [[doc/arch.md]], a `doc/` subtree, a GitLab wiki, or another browse tree.
- What “GitLab-level browsable” means for this wiki (nav, page grain, runtime vs code maps).
- How this wiki cites [[doc/current/]] without restating feature baselines.
- Boundary vs End-user wiki (operation) and vs Committed Decisions under [[doc/Decisions/]].

## Out of scope

- Changing how the software runs; this Project describes it.
- Replacing [[doc/current/]] as the authority for implemented feature behavior.
- A marketing campaign.
