# Architecture

This page is the home of the architecture wiki. Architecture describes what the pieces of the program are, and how they connect.  It is not detail of every behavior within the smallest pieces nor documentation of every command's detailed behavior.

One subject is one page. This home links that page.
Agents must read: [CONTEXT](CONTEXT.md)

## Summary

[x] Client/server architecture multi-client outliner.
[x] Server authority.
[x] Server centered on events queued through a mailbox, handled asynchronously, with intelligent actors.
[x] Database keeps graph and event info.
[x] Parse reads disk into the graph.
[x] Persist writes graph information to disk.
[x] Client-side MVU-style loop, optimistic editing.
[x] Bias toward small download size and low conceptual overhead.
[x] Full-stack authored in F# with an immutable domain model.
[x] Main containers may be mutable; elements should remain immutable.

## Building blocks

[x] First view of Reference: the project structure, under Building blocks.
[x] Projects in gambol.sln.

[Gambol.Client](gambol-client.md)
[Gambol.Server](gambol-server.md)
[Gambol.Shared](gambol-shared.md)
[Gambol.CloudAgents](gambol-cloud-agents.md)
[Gambol.CloudAgents.Console](gambol-cloud-agents-console.md)
[Gambol.Shared.Tests](gambol-shared-tests.md)
[Gambol.CloudAgents.Tests](gambol-cloud-agents-tests.md)
[Gambol.Server.Tests](gambol-server-tests.md)
[Gambol.Desktop](gambol-desktop.md)
[Gambol.Shared.DotNet](gambol-shared-dotnet.md)
[Gambol.Shared.Documents](gambol-shared-documents.md)

[x] Separate test projects reference only the code under test (Shared vs Server) and keep dependencies clean.

### Additional items

[x] `data/`: test workspace for localhost deployment (local dev default).
[x] `doc/`: architecture, API notes, deployment, future plans.

[doc/reference/index.md](../reference/index.md)

[x] Tooling.

### Subsystems

[Core](core.md)
[Mailbox](mailbox.md)

### Modules

[x] Module: an F# module plus the types associated with that module.

## Capabilities

[Browser](browser.md)
[Server](server.md)
[Operations](operations.md)
[Search Actor](search-actor.md)
[Query Actor](query-actor.md)
[Want nodes for hits](want-nodes.md)
[View](view.md)
[Multi-client sync](sync-mvp.md)
[Desktop local files](desktop-local-files.md)
[Workspace local mapping](workspace-local-mapping.md)
[Workspace file sync](workspace-file-sync.md)
[Testing](testing.md)

## Information

[Op](op.md)
[Persistence model](persistence-model.md)
[Workspace graph](graph.md)

## Contracts

[API](api.md)
[AI agent protocol](ai-agent-protocol.md)
