# Create the initial wiki structure with the established elements

**Type:** task
**Status:** coded
Actual: 45m
Blocked by:

## Question

Create the initial wiki structure with the established elements.

The elements below are already recorded on [[../map.md]] and in [[GLOSSARY.md]].

- The architecture wiki is the wiki [[doc/current/]] is becoming. [[doc/current/arch.md]] is part of that wiki. This ticket creates the initial structure in that wiki. It does not create a second home.
- Reference is primary. Explanation is secondary. Page grain is one subject per page, linked from a home. Code and runtime are one description. A subject page states what Is, what Should Become, and where that lives. Explanation is why, and it is not the lead. Explanation stays on the page until it grows. No page list ahead of a subject. Recorded on [What GitLab-level browsable means for architecture](plan/architecture/issues/02-gitlab-level-browsable.md).
- The first view of Reference is the project structure. Gambol.CloudAgents and Gambol.CloudAgents.Console are on that home. Their pages wait until that cluster is looked at.
- A Module is an F# `module`, plus the types associated with that module. Illustrations only: `module Gambol.Client.App` in [[src/Client/App.fs]], and [[src/Shared/Model.fs]] `type NodeKind` with `module NodeKind`, and `type SpecialKind` which `NodeKind.Special` carries. No module catalog.
- A Subsystem is a named body inside a project, made of F# modules and the types associated with them, with one Interface. Core is the first Subsystem, of the Server project. Others wait. Glossary terms: [[GLOSSARY.md]] **Subsystem** and **Core**. Core behavior stays on [[../server-core.md]].
- Claims on later pages use Is and Should Become. Marks Alan stated: `[ ]` planned, `[/]` started, `[x]` implemented, `[o]` obsolete yet implemented. An `[o]` claim is paired with the `[ ]` that retires it. When that claim is `[x]`, the `[o]` claim is removed. Recorded on [Boundary vs End-user wiki and Committed Decisions](plan/architecture/issues/04-boundary-vs-end-user-wiki-and-decisions.md).
- [[doc/api.md]] is not migrated in this ticket.
- How to use stays linked from the [End-user wiki](plan/end-user-wiki/map.md). A Committed Decision stays linked from [[doc/Decisions/]]. This ticket does not copy either. [[doc/current/arch.md]]

## Comments

- 2026-09-30 — Filed from Alan. Map: [[../map.md]].
- 2026-09-30 — Structure is the Reference lead on [[doc/current/arch.md]].

## Time

- 2026-09-30 45m — Reference lead on [[doc/current/arch.md]] (from chat)
