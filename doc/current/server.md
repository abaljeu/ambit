# Server

Category: Capability

See Also:

[Core](core.md)
[Multi-client sync](sync-mvp.md)
[Persistence model](persistence-model.md)
[Workspace graph](workspace-graph.md)
[Gambol.Server](gambol-server.md)

The Server is the spoken name for Gambol.Server.

## Job

[x] ASP.NET Core minimal API with Npgsql.
[x] Holds the authoritative graph and revision.
[x] Serves the `/ambit` API and static assets.
[x] Serves `GET /ambit`. HTML shell: `gambol.template.html`. Fable bundles: `wwwroot`. HTTP entry: [Api.fs](../../src/Server/Api.fs) (`AgentHandle`).
[x] JSON API under `/ambit` for state, poll, and changes. Contract: [HTTP contract](http-contract.md). Running server: the `/ambit` prefix.
[x] Cookie auth: optional. Config keys `Auth:Username` and `Auth:Password` yield a derived token cookie.
[x] Up to five clients may operate on the same model at the same time.
[x] Sync baseline: last-write-wins by arrival order on the server. Detail: [Multi-client sync](sync-mvp.md).
[ ] Sync: merge-based, with 409 conflicts and `remoteChanges`. Contract: [HTTP contract](http-contract.md).
[x] Workspace Upload and Download use WebDAV under `/ambit/dav/{label}/…`. Server surface: [Workspace WebDAV](../roadmap/workspace-webdav.md).
[x] Keeps an append-only change log. File: [ChangeLog.fs](../../src/Server/ChangeLog.fs).

## Core

[x] [Core](core.md) links the mailbox, parse and persist, the file agent, the db agent, and Actors.
