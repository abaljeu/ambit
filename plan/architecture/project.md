# Architecture

Stage: build
Summary: A browsable description of how Gambol is coded and how it runs.
Updated: 2026-10-01
Started: 2026-09-28
Actual: 45m

## Notes

- 2026-09-28 — Locked Server Core description from Alan inbound: [[server-core.md]]. Correction the same day: git use is Core.
- 2026-09-28 — Pointer: special-node Parse/Persist axes (step 1) live on [core-refinement architecture](plan/core-refinement/arch.md). Git use stays Core ([[server-core.md]]).
- 2026-09-30 — Alan: one sole authority for the Core seam is [[plan/core-refinement/arch.md]]. [[server-core.md]] is the compact description of that same target, linked from the master. This Project is not a second active Core-seam plan.
- 2026-09-30 — Alan: the architecture wiki is the wiki [[doc/current/]] is becoming, and [[doc/current/arch.md]] is part of it. That wiki is the broader home for Is and Should Become. [[plan/core-refinement/arch.md]] still holds Is, Should Become, and the path, and stays sole authority for what will be coded. Updating the arch documents is a task of that effort. This Project is still not a second active Core-seam plan.
- 2026-09-30 — Alan: browse quality is a wiki you can walk (linked pages; how it runs and how it is coded are one description). Page grain is named in the note below. The cite-[[doc/current/]] question is dissolved. No gloss-versus-pointer rule.
- 2026-09-30 — Alan: page grain on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md) is one subject per page, linked from a home page. Code and runtime are one description. How to use and Committed Decisions stay linked. The path stays in the plan. No page list ahead of a subject. Reference is primary. Explanation is secondary. A subject page is what you look up: what Is, what Should Become, and where that lives. Explanation is why, and it is not the lead. Whether explanation, or any other part, grows into its own page is decided when it grows, not in advance. Existing reference documents such as [[doc/api.md]] are migrated into this wiki when the time comes to look at them. Not now. The earlier unnamed clause is superseded.
- 2026-09-30 — Alan: Reference's first and most definitive view, on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md), is the project structure. Gambol.CloudAgents and Gambol.CloudAgents.Console are on that home because they are projects in the solution. Their pages wait until that cluster is looked at.
- 2026-09-30 — Alan: Reference categories on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md): project structure is the first view. Modules is a further category, filled as we go. No module list ahead of looking. The interface-and-implementation wording is superseded below.
- 2026-09-30 — Alan: a Module on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md) is an F# `module`, as in `module Gambol.Client.App` in [[src/Client/App.fs]]. The category is filled as we go. The wording that a Module is anything with an interface and an implementation is superseded.
- 2026-09-30 — Alan: a Module entry on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md) includes the things associated with that module. The illustration is [[src/Shared/Model.fs]]: `type NodeKind` with `module NodeKind`, and `type SpecialKind` which `NodeKind.Special` carries. The Core / Core API illustration is superseded. No list ahead of looking.
- 2026-09-30 — Alan: Subsystem is a Reference category on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md). Core is the first Subsystem, of the Server project. Glossary: [[GLOSSARY.md]] **Subsystem** and **Core**. Others wait until looked at.
- 2026-09-30 — Alan: file [Create the initial wiki structure with the established elements](plan/architecture/issues/05-create-initial-wiki-structure.md). Stage stays chart.
- 2026-09-30 — Initial structure is the Reference lead on [[doc/current/arch.md]]. [Create the initial wiki structure with the established elements](plan/architecture/issues/05-create-initial-wiki-structure.md).
- 2026-10-01 — Alan: this Project owns reconcile of seven workspace-file docs with existing plans and with [[doc/current/]]. The files stay in [[doc/roadmap/]]. This note does not merge their claims. [Parse thread](plan/parse-thread/project.md) may later claim Parse product leftovers; the two Parse docs are charted here first.
- 2026-10-01 — [06 — Parse / Upload for Current Files (Warm Reconcile)](plan/architecture/issues/06-parse-upload-for-current-files-warm-reconcile.md) — [Parse / Upload for Current Files (Warm Reconcile)](doc/roadmap/parse-file-reconcile-current.md).
- 2026-10-01 — [07 — ParseFile document reader](plan/architecture/issues/07-parsefile-document-reader.md) — [ParseFile → Document Reader](doc/roadmap/parsefile-document-codec-import.md).
- 2026-10-01 — [08 — Revising Workspace File Model](plan/architecture/issues/08-revising-workspace-file-model.md) — [Revising Workspace File Model](doc/roadmap/revising-workspace-file-model.md).
- 2026-10-01 — [09 — File and Directory owner placement](plan/architecture/issues/09-file-and-directory-owner-placement.md) — [File and Directory owner placement](doc/roadmap/workspace-file-directory-placement.md).
- 2026-10-01 — [10 — Workspace File Model](plan/architecture/issues/10-workspace-file-model.md) — [Workspace File Model](doc/roadmap/workspace-file-model.md).
- 2026-10-01 — [11 — Workspace File Persistence](plan/architecture/issues/11-workspace-file-persistence.md) — [Workspace File Persistence](doc/roadmap/workspace-file-persistence.md).
- 2026-10-01 — [12 — Workspace scale file and db management](plan/architecture/issues/12-workspace-scale-file-and-db-management.md) — [Workspace scale file and db management](doc/roadmap/workspace-scale-file-and-db-management.md).
