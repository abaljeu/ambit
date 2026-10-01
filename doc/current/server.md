# Server

Category: Architecture
See Also: [[doc/current/arch.md]], [[doc/api.md]], [[doc/current/sync-mvp.md]], [[doc/current/persistence-model.md]], [Server Core](plan/architecture/server-core.md)

The Server is the spoken name for Gambol.Server ([[GLOSSARY.md]]). It is ASP.NET Core (minimal API) and Npgsql. It holds the authoritative graph and revision, an append-only change log, and serves the `/ambit` API and static assets.

## Is

- Serves `GET /ambit` (HTML shell from `gambol.template.html`) and Fable bundles from `wwwroot`
- Exposes a JSON API under `/ambit` (state, poll, changes). The contract stays [[doc/api.md]]. The running server uses the `/ambit` prefix.
- Persists the graph via PostgreSQL. Correlated on-disk artifacts auto-persist from accepted DB state ([[doc/current/persistence-model.md]]). On-disk artifacts under `DataDir` default to `data/` locally and `/home/data` on Azure.
- Optional cookie auth (`Auth:Username` / `Auth:Password` in config → derived token cookie)
- Assumption: multiple clients (up to 5) may operate on the same model concurrently. Current baseline is last-write-wins by arrival order on the server ([[doc/current/sync-mvp.md]]).
- The mailbox owns one in-memory History sequence (process-lifetime until durability) containing both successful Change events and Actor lifecycle events (ActorStarted, ActorFinished). Undo/Redo remain Change-only. Actor Events are not Undo targets.
- Workspace Upload / Download transport is WebDAV under `/ambit/dav/{label}/…` (file-channel [[plan/transport-layer/project.md]], server surface [[doc/roadmap/workspace-webdav.md]]).
- [o] Legacy `FileAgent.fs` / `Persistence:Mode` rollback hooks remain in code.
- [x] Core is the Server subsystem. The mailbox door is [[src/Server/Core/CoreMailbox.fs]]. Compact target: [Server Core](plan/architecture/server-core.md). Seam authority: [core-refinement architecture](plan/core-refinement/arch.md).
- [x] One long-lived Parse stack and parse thread. The thread is not an Actor. The loop runs `DocumentPersistWrite.planParseFile`. Files: [[src/Server/ParseStack.fs]], [[src/Server/ParseThread.fs]].
- [o] [[src/Server/RouteRegistration.fs]] `createPersistenceContext` builds `ParseStack` and calls `ParseThread.start`.
- [o] `WorkspaceGit.withWorkTreeGate` is the exclusive gate on Persist and git paths.

Core is the first Subsystem of this project. Core behavior stays on [Server Core](plan/architecture/server-core.md). The mailbox notes below are the code as it stands.

[[src/Server/Core/CoreMailbox.fs]] owns the shared `CoreMsg` mailbox contract and the qualified callable interface over `MailboxProcessor<CoreMsg>`. The contract has exactly six cases with their existing payloads: `GetState` with a state result reply, `GetRevision` with a revision result reply, `GetChangesSince` with the revision index and change-list result reply, `PostChange` and `PostGraphOnlyChange` with change lists and accepted-change result replies, and `SnapshotDone` with an optional graph. `CoreMailbox` centralizes the mailbox posts, result unwrapping, Revision-to-int conversion, and construction of the six-field `CoreChanges` handle.

[[src/Server/Core/CoreMailboxBackend.fs]] is the internal shared implementation used directly by both persistence agents. It contains the common bounded-execution helper and timeout, fresh-change overlay, operation context, and failure reply routing. These mechanics stay out of the mailbox contract file and are not duplicated between agents.

[[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] are persistence twins: each hosts and starts its own `MailboxProcessor<CoreMsg>`, while its exported `coreChanges` delegates to `CoreMailbox.coreChanges` with the private mailbox and the agent's readiness callback. FileAgent owns file logging, checkpointing, and file-persistence details. DbAgent owns SQL persistence, snapshots, reads, startup, and failed-loop behavior.

The server project compiles [[src/Server/Core/CoreMailbox.fs]] before [[src/Server/Core/CoreMailboxBackend.fs]], then compiles the agents. This mailbox refactor adds no Actor or TestActor message cases, endpoints, registry, command dispatch, lifecycle, or other behavior.

## Should Become

- [ ] Removal of the legacy `Persistence:Mode` / `FileAgent` file-authority path from server startup ([[doc/current/persistence-model.md]]).
- [ ] Merge-based sync, 409 conflicts, and `remoteChanges` ([[doc/api.md]]).
- [ ] Parse setup (stack, push, and consumer) lives only in [[src/Server/Core]]. RouteRegistration does not construct Parse, start it, or hold its handles. This retires the [o] RouteRegistration start.
- [ ] After the workspace lock drains in-flight member file use, pull or Upload land proceeds. Arrived files are marked Unparsed. The lock releases. Unparsed starts the parse thread.
- [ ] Retire today's Load → Parse / graph-push hop once every handoff uses the Parse stack.
- [ ] Stand the workspace lock and per-member persist locks beside `withWorkTreeGate`. While the workspace lock is pending, new persist locks for those member files cannot be taken. In-flight member file writes and parse reads drain. Then pull proceeds, arrived files are marked Unparsed, and the lock releases. Protocol: [core-refinement architecture](plan/core-refinement/arch.md) §6 Core locking model.
- [ ] Persist, parse-thread file use, and git Load/Save follow that lock protocol. `withWorkTreeGate` remains only while both run.
- [ ] Remove `withWorkTreeGate` from Persist and git paths once the workspace lock is the only protocol. This retires the [o] gate.
- [ ] Git use is Core. Core performs git Load, git Save, pull, push, and commit. An Actor requests that work by posting to the mailbox. Compact target: [Server Core](plan/architecture/server-core.md).

Persist stack and path control claims: [Persistence model](doc/current/persistence-model.md). Axis claims: [Workspace graph](doc/current/workspace-graph.md).

## Where

- Project: [[src/Server]]
- [[src/Server/Api.fs]] (`AgentHandle`), [[src/Server/Core/CoreMailbox.fs]], [[src/Server/Core/CoreMailboxBackend.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]], [[src/Server/Database.fs]], [[src/Server/DatabaseSetup.fs]], [[src/Server/ChangeLog.fs]], [[src/Server/DocumentLoader.fs]], [[src/Server/ParseStack.fs]], [[src/Server/ParseThread.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/WorkspaceGit.fs]]
- Schema and rules: [[doc/current/persistence-model.md]]. Environments: [[doc/reference/postgres-environments.md]].
