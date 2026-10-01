# Architecture

Category: Architecture
See Also: [[GLOSSARY.md]], [[doc/Decisions/]], [[doc/api.md]], [End-user wiki](plan/end-user-wiki/map.md)

## 1. Project structure

The first view of Reference is the project structure. This page is the home of the architecture wiki. The architecture wiki is the wiki [[doc/current/]] is becoming. This page is part of that wiki.

These projects are in [[gambol.sln]]. A project page waits until this wiki looks at that project. Gambol.CloudAgents and Gambol.CloudAgents.Console are on this home. Their pages wait until that cluster is looked at.

1. **Gambol.Client** — [[src/Client]]. F# to JavaScript (Fable). The spoken name is Browser ([[GLOSSARY.md]]). [[doc/current/browser.md]]
2. **Gambol.Server** — [[src/Server]]. ASP.NET Core (minimal API), Npgsql. The spoken name is Server ([[GLOSSARY.md]]). [[doc/current/server.md]]
3. **Gambol.Shared** — [[src/Shared]]. Pure F# domain model, ops, and serialization. Preferred home for testable logic. Ops: [[doc/current/operations.md]]. Node and Graph fields: [[doc/current/persistence-model.md]]. Workspace special nodes: [[doc/current/workspace-graph.md]].
4. **Gambol.CloudAgents** — [[src/CloudAgents]].
5. **Gambol.CloudAgents.Console** — [[src/CloudAgents.Console]].
6. **Gambol.Shared.Tests** — [[tests/Shared.Tests]]. xUnit. [[doc/current/testing.md]]
7. **Gambol.CloudAgents.Tests** — [[tests/CloudAgents.Tests]].
8. **Gambol.Server.Tests** — [[tests/Server.Tests]]. xUnit. [[doc/current/testing.md]]
9. **Gambol.Desktop** — [[src/Desktop]]. .NET WPF and WebView2, local proxy. The spoken name is App ([[GLOSSARY.md]]). Optional host: WebView2 shell + local HTTP proxy to the cloud app; local file import only. [[doc/current/desktop-local-files.md]]. Roadmap: [[doc/roadmap/postgres-roadmap.md]] §7.
10. **Gambol.Shared.DotNet** — [[src/Shared/dotnet]].
11. **Gambol.Shared.Documents** — [[src/Shared/documents]].

Separate test projects reference only the code under test (Shared vs Server) and keep dependencies clean.

### 1.1 Additional items

1. gambol.sln
2. data/              correlated on-disk document artifacts under DataDir (local dev default)
3. doc/               architecture, API notes, deployment, future plans
4. scripts/           desktop.sh, fullstack-build.sh, azure helpers



## 2. Modules

A Module is an F# `module`, plus the types associated with that module. This category is filled as this wiki looks at modules. This page does not list modules.

1. **Gambol.Client.App** — illustration. `module Gambol.Client.App` in [[src/Client/App.fs]].
2. **NodeKind** — illustration. [[src/Shared/Model.fs]] has `type NodeKind` with `module NodeKind`, and `type SpecialKind`. `NodeKind.Special` carries `SpecialKind`.

## 3. Subsystems

A Subsystem is a named body inside a project, made of F# modules and the types associated with them, with one Interface. Glossary: [[GLOSSARY.md]] **Subsystem** and **Core**.

1. **Core** — the first Subsystem, of the Server project. Core behavior stays on [Server Core](plan/architecture/server-core.md). Other Subsystems wait until this wiki looks at them.

## 4. Claims

Later pages use Is and Should Become.

1. **Planned** — `[ ]`
2. **Started** — `[/]`
3. **Implemented** — `[x]`
4. **Obsolete yet implemented** — `[o]`

An `[o]` claim is paired with the `[ ]` claim that retires it. When that claim is `[x]`, the `[o]` claim is removed.

## 5. Other corpora

1. **How to use** — [End-user wiki](plan/end-user-wiki/map.md). This wiki does not copy how to use.
2. **Committed Decision** — [[doc/Decisions/]]. This wiki links a Committed Decision. It does not copy the decision.
3. **HTTP contract** — [[doc/api.md]] stays where it is.

## 6. Explanation

Reference is primary. Explanation is secondary. One subject is one page, and this home links that page. Code and runtime are one description. A subject page states what Is, what Should Become, and where that lives. Explanation is why. Explanation is not the lead. Explanation stays on the page until it grows.

- Client/server architecture with a client-side MVU-style loop
- Bias toward small download size and low conceptual overhead
- Full-stack authored in F# with an immutable domain model
  - Main containers may be mutable; elements should remain immutable

## Building

VS Code: default build runs Fable watch + server (`dev: Watch + Run`). Watch-task Server and F5 (`Local Server` / `Full Stack`) are alternate starters on `:5215` — see [[doc/reference/dev-debug-workflow.md]]. Desktop: `desktop: Run` → `scripts/desktop.sh run`.
