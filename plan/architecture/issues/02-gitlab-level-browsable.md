# What GitLab-level browsable means for architecture

**Type:** grilling
**Status:** done
Blocked by:

## Question

What does “GitLab-level browsable” mean for this wiki: navigation, page grain, runtime maps vs code maps? Decide the browse quality. Do not implement product features.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Map: [[../map.md]].
- 2026-09-30 — Answer recorded from Alan.
- 2026-09-30 — Page grain named: one subject per page, linked from a home page. The unnamed clause is superseded.
- 2026-09-30 — Reference's first view is the project structure. Gambol.CloudAgents and Gambol.CloudAgents.Console are on that home. Their pages wait.
- 2026-09-30 — Modules is a further Reference category, filled as we go. No module list ahead of looking.
- 2026-09-30 — A Module is an F# `module`, as in `module Gambol.Client.App`. The interface-and-implementation wording is superseded.
- 2026-09-30 — A Module entry includes the things associated with that module. The illustration is `type NodeKind` with `module NodeKind`, and `type SpecialKind`. The Core / Core API illustration is superseded.
- 2026-09-30 — Subsystem is a Reference category. Core is the first Subsystem, of the Server project. Others wait until looked at.

## Answer

GitLab as a host is a distraction. The home is the wiki that [[doc/current/]] is becoming, with [[doc/current/arch.md]] as part of it, as settled on [Choose the architecture wiki home](plan/architecture/issues/01-choose-wiki-home.md).

Browse quality on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md) is a wiki you can walk: linked pages, not one file, and not a checklist tree. How it runs and how it is coded are one description. Page grain is named in the revision below. Do not take page grain from today's [[doc/current/]] headings. Do not add a folder layout.

**Revision (2026-09-30):** Page grain is one subject per page, linked from a home page. Code and runtime are one description. How to use and Committed Decisions stay linked. The path stays in the plan. No page list ahead of a subject. Reference is primary. Explanation is secondary. A subject page is what you look up: what Is, what Should Become, and where that lives. Explanation is why, and it is not the lead. Whether explanation, or any other part, grows into its own page is decided when it grows, not in advance. Existing reference documents are migrated into this wiki when the time comes to look at them. Not now. The HTTP contract now lives at [[doc/current/http-contract.md]]. The index is [[doc/current/api.md]]. The earlier clause that page grain and navigation stay unnamed is superseded.

**Revision (2026-09-30):** Reference's first and most definitive view is the project structure. Gambol.CloudAgents and Gambol.CloudAgents.Console are on that home because they are projects in the solution. Their pages wait until that cluster is looked at.

**Revision (2026-09-30):** Reference categories on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md): project structure is the first view. Modules is a further category, filled as we go. No module list ahead of looking. The interface-and-implementation wording is superseded below.

**Revision (2026-09-30):** A Module is an F# `module`, as in `module Gambol.Client.App` in [[src/Client/App.fs]]. The category is filled as we go. The wording that a Module is anything with an interface and an implementation is superseded.

**Revision (2026-09-30):** A Module entry includes the things associated with that module. The illustration is [[src/Shared/Model.fs]]: `type NodeKind` with `module NodeKind`, and `type SpecialKind` which `NodeKind.Special` carries. The Core / Core API illustration is superseded. No list ahead of looking.

**Revision (2026-09-30):** Subsystem is a Reference category on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md). A Subsystem is a named body inside a project, made of F# modules and the types associated with them, with one Interface. Core is the first Subsystem, of the Server project. Others wait until looked at. Glossary: [[GLOSSARY.md]] **Subsystem** and **Core**.
