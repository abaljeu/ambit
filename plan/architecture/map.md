# Architecture

Labels: wayfinder:map

## Destination

A browsable description of how Gambol is coded and how it runs: processes, layers, data flow, and where behavior lives (Browser, Server, App, Shared, Document).

## Notes

- Charted from [[plan/roadmap/map.md]] after [[plan/roadmap/issues/02-inventory-live-projects-and-roadmap-remainder.md]].
- [[doc/current/arch.md]] is a thin engineering overview. [[doc/current/]] holds feature baselines. [[doc/agents/domain.md]] says where canonical docs live. This Project is the standing effort to describe coding and runtime, not a second source of feature truth.
- Sister Projects: [[plan/end-user-wiki/map.md]] (what the software is for users), [[plan/marketing-wiki/map.md]] (uses).
- Related: [[plan/debug-reload/project.md]] -- how a person on watch loads debug modules and picks up an esbuild rebuild (Browser hard-reload). Homed on [[plan/roadmap/epics/robust-outliner.md]].
- 2026-09-28 — Locked [[server-core.md]] from Alan inbound Server description. Correction the same day: git use is Core.
- 2026-09-28 — Pointer: special-node Parse/Persist axes (step 1) live on [core-refinement architecture](plan/core-refinement/arch.md). Git use stays Core ([[server-core.md]]).
- 2026-09-30 — Alan: Core seam sole authority is [[plan/core-refinement/arch.md]]. [[server-core.md]] is the compact description of that same target, linked from the master.
- 2026-09-30 — Filed [Create the initial wiki structure with the established elements](plan/architecture/issues/05-create-initial-wiki-structure.md).
- 2026-10-01 — Alan: this Project owns reconcile of seven workspace-file docs with existing plans and with [[doc/current/]]. The files stay in [[doc/roadmap/]]. This chart does not merge their claims into [[doc/current/]]. [Parse thread](plan/parse-thread/project.md) may later claim Parse product leftovers; the two Parse docs are charted here first. Open disagreements stay on the tickets.
- 2026-10-02 — Stub pages for Server description outline headers only (no Alan body): [[configuration-secrets.md]], [[client-contract-surface.md]]. Link existing homes; not product locks.

## Reconcile

1. [06 — Parse / Upload for Current Files (Warm Reconcile)](issues/06-parse-upload-for-current-files-warm-reconcile.md) — [Parse / Upload for Current Files (Warm Reconcile)](doc/roadmap/parse-file-reconcile-current.md) says server-apply landed. [[doc/index.md]] has no feature page for it. [Parse thread](plan/parse-thread/project.md) is the Parse product home.
2. [07 — ParseFile document reader](issues/07-parsefile-document-reader.md) — [ParseFile → Document Reader](doc/roadmap/parsefile-document-codec-import.md) says Current warm remains. The warm-reconcile doc says server-apply landed.
3. [08 — Revising Workspace File Model](issues/08-revising-workspace-file-model.md) — [Revising Workspace File Model](doc/roadmap/revising-workspace-file-model.md) says server file persistence is not fully implemented. [Workspace stage plan](doc/current/workspace-stage-plan.md) marks Stage 7 and Stage 8 done.
4. [09 — File and Directory owner placement](issues/09-file-and-directory-owner-placement.md) — [File and Directory owner placement](doc/roadmap/workspace-file-directory-placement.md) Status is In progress. [Workspace graph](doc/current/workspace-graph.md) states those rules as the baseline. [Workspaces checklist](doc/roadmap/workspaces-checklist.md) marks the item done.
5. [10 — Workspace File Model](issues/10-workspace-file-model.md) — [Workspace File Model](doc/roadmap/workspace-file-model.md) says the whole graph is one document today, and also says per-document `DataDir` persistence is implemented. [Workspace graph](doc/current/workspace-graph.md) still calls the Stage 6 TRASH name token a target. [Workspace stage plan](doc/current/workspace-stage-plan.md) marks Stage 6 done.
6. [11 — Workspace File Persistence](issues/11-workspace-file-persistence.md) — [Workspace File Persistence](doc/roadmap/workspace-file-persistence.md) is Draft. [Persistence model](doc/current/persistence-model.md) describes auto-persist as current and lists full per-document layout under Not implemented.
7. [12 — Workspace scale file and db management](issues/12-workspace-scale-file-and-db-management.md) — [Workspace scale file and db management](doc/roadmap/workspace-scale-file-and-db-management.md) marks `DataDir` live-save done and leaves later steps on [Transport layer](plan/transport-layer/project.md), [Workspace scale import](doc/roadmap/workspace-scale-import.md), and [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md).

## Pages

1. **Browser and App auth** — [[browser-and-app-auth.md]] — how the Browser and the App present `gambol_auth` to Core, and how a Server restart keeps the same derived token.
2. **Server Core** — [[server-core.md]] — compact description of the Core target; sole authority is [[plan/core-refinement/arch.md]] (Target — Server Core).
3. **Configuration / secrets** — [[configuration-secrets.md]] — stub; points at [[doc/reference/secrets.md]], [[doc/reference/deploy-azure.md]], and [[browser-and-app-auth.md]]. Not a product lock.
4. **Client contract surface** — [[client-contract-surface.md]] — stub; points at [[doc/api.md]]. Not a product lock.

## Decisions so far

- Goal is how it is coded and how it runs, not user how-to and not use-case marketing.
- 2026-09-28 — Server Core description locked from Alan inbound. Git use is Core. Sole Core seam authority is [[plan/core-refinement/arch.md]] Target — Server Core; [[server-core.md]] is the compact description linked from that master.
- 2026-09-30 — The architecture wiki is the wiki [[doc/current/]] is becoming. [[doc/current/arch.md]] is part of that wiki. It is not a separate file and not a GitLab wiki. [Choose the architecture wiki home](plan/architecture/issues/01-choose-wiki-home.md).
- 2026-09-30 — The wiki holds Is and Should Become for how it works, broader than the Core seam. How to use stays the end-user wiki. A Committed Decision stays under [[doc/Decisions/]]; the wiki links it and does not restate it. Plans point at the wiki and are not a second home for the idea. [Boundary vs End-user wiki and Committed Decisions](plan/architecture/issues/04-boundary-vs-end-user-wiki-and-decisions.md).
- 2026-09-30 — Revision: [[plan/core-refinement/arch.md]] holds Is, Should Become, and the path for the Core seam. It remains sole authority for what will be coded. [[server-core.md]] remains the compact description. The wiki is the broader home for Is and Should Become. Updating the arch documents so the wiki matches those aims is a task of that effort. The 2026-09-30 sole-authority note stands.
- 2026-09-30 — Browse quality is a wiki you can walk: linked pages, not one file, and not a checklist tree. How it runs and how it is coded are one description. Page grain is named in the revision below. [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md).
- 2026-09-30 — Revision: page grain on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md) is one subject per page, linked from a home page. Code and runtime are one description. How to use and Committed Decisions stay linked. The path stays in the plan. No page list ahead of a subject. Reference is primary. Explanation is secondary. A subject page is what you look up: what Is, what Should Become, and where that lives. Explanation is why, and it is not the lead. Whether explanation, or any other part, grows into its own page is decided when it grows, not in advance. Existing reference documents such as [[doc/api.md]] are migrated into this wiki when the time comes to look at them. Not now. The earlier clause that page grain and navigation stay unnamed is superseded.
- 2026-09-30 — Revision: Reference's first and most definitive view, on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md), is the project structure. Gambol.CloudAgents and Gambol.CloudAgents.Console are on that home because they are projects in the solution. Their pages wait until that cluster is looked at.
- 2026-09-30 — Revision: Reference categories on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md): project structure is the first view. Modules is a further category, filled as we go. No module list ahead of looking. The interface-and-implementation wording is superseded below.
- 2026-09-30 — Revision: a Module on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md) is an F# `module`, as in `module Gambol.Client.App` in [[src/Client/App.fs]]. The category is filled as we go. The wording that a Module is anything with an interface and an implementation is superseded.
- 2026-09-30 — Revision: a Module entry on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md) includes the things associated with that module. The illustration is [[src/Shared/Model.fs]]: `type NodeKind` with `module NodeKind`, and `type SpecialKind` which `NodeKind.Special` carries. The Core / Core API illustration is superseded. No list ahead of looking.
- 2026-09-30 — Revision: Subsystem is a Reference category on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md). A Subsystem is a named body inside a project, made of F# modules and the types associated with them, with one Interface. Core is the first Subsystem, of the Server project. Others wait until looked at. Glossary: [[GLOSSARY.md]] **Subsystem** and **Core**.
- 2026-09-30 — The cite question is dissolved. There is no outside architecture wiki that cites [[doc/current/]]. This wiki holds Is and Should Become for how it works. No gloss-versus-pointer rule. [How this wiki cites doc/current without restating](plan/architecture/issues/03-cite-doc-current-without-restating.md).
- 2026-09-30 — The home is [[doc/current/arch.md]]. The initial structure is the Reference lead on that page: project structure, then Modules and Subsystems. [Create the initial wiki structure with the established elements](plan/architecture/issues/05-create-initial-wiki-structure.md).

## Not yet specified

None.

## Out of scope

- Changing how the software runs; this Project describes it.
- Replacing [[doc/current/]] as the authority for implemented feature behavior.
- A marketing campaign.
