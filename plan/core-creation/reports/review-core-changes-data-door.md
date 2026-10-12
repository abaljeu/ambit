# Code review — CoreChanges data door

- Range: `git diff HEAD`
- Spec: [CoreChanges data door](file:///C:/Users/Windows%208/.cursor/plans/corechanges_data_door_d84afc83.plan.md)
- Arch: [Core creation architecture](plan/core-creation/arch.md), [Single event source architecture](plan/single-event-source/arch.md), [Browser residency architecture](plan/browser-residency/arch.md), [Core is a container of subobjects](doc/Decisions/0003-core-is-a-container-of-subobjects.md)

## 1. Standards

1. **Hard** — [F# source](.agents/rules/fsharp-source.md): "Avoid creating a forwarding module in favor of updating the users to point to the right new file." [CoreMailbox](src/Server/Core/CoreMailbox.fs) re-exports [CoreMailboxApi](src/Server/Core/CoreMailboxApi.fs) from `tryGetState` through `dispose`.
2. **Hard** — [F# source](.agents/rules/fsharp-source.md): "That name must not be used by another entity." `getState`, `getEventId`, `getEventsSince`, `isReady`, `postEvents`, `postGraphOnly`, and `actorStop` are each public on `CoreDoor`, `CoreMailbox`, and `CoreMailboxApi`.
3. **Hard** — [F# source](.agents/rules/fsharp-source.md): "Don't use Exceptions. Use Error types." `CoreMailboxApi.unwrap` is `| Error error -> failwith error`. `getEventId` and `getEventsSince` call it.
4. **Judgement — Middle Man** — [CoreDoor](src/Server/Core/CoreDoor.fs), `getState`, `getEventId`, `getEventsSince`, `isReady`. Each only forwards `door.host`. The writable check is not in these four.
5. **Judgement — Duplicated Code** — [CoreDoor](src/Server/Core/CoreDoor.fs), `postEvents`, `postGraphOnly`, `actorStop`, `loadNode`. Each copies `if door.writable then … else readOnlyResult ()`.
6. **Judgement — Duplicated Code** — [Update](src/Client/Update.fs), `WorkspaceRemotesReceived`. `label.Trim().ToLowerInvariant()` repeats [ViewModelRowState](src/Shared/ViewModelRowState.fs) `normalizeWorkspaceLabel`.
7. Scan note: no function over 40 lines, no F# line over 100, no file over 800. Test `let mutable found` in [CoreActorPoolFunctionStartTests](tests/Server.Tests/CoreActorPoolFunctionStartTests.fs) and [QueryActorTests](tests/Server.Tests/QueryActorTests.fs) is exempt. No production mutable. No banned phrasing.

## 2. Spec

4. **Partial** — Actor factory. The plan stores `CoreDoor.create true host` in `bindCoreChanges`. `CoreMailbox.hostPublishing` writes `{ host; caller; writable = true }` inline.
5. **Extra** — `module CoreChanges` was renamed to `CoreChangesAccept`. The plan left `accepted` and `mergeAccepted` in `CoreChanges.fs` under the existing module.
6. **Extra** — `ActorName` and `PeerActorName` moved into [CoreActorNames](src/Server/Core/CoreActorNames.fs). The plan did not split them out of `CoreActorPool.fs`.
7. **Extra** — Workspace remote tips: `WorkspaceGit.remoteTip`, `Api.getWorkspaceRemotes`, `GET /ambit/workspace-remotes`, client `RequestWorkspaceRemotes`, `VM.workspaceRemotes`, and a bullet-tip remote line. No local plan ticket.
8. **Unnamed** — `WorkspaceRemoteTip` (`remote`, `ahead`, `behind`) in [WorkspaceGitRemote](src/Shared/WorkspaceGitRemote.fs). The door plan did not name this record.
9. Landed: function slots, `asCaller`, `CoreRuntime.readOnly`, `CoreAuth.bindHandle`, and `CoreMailbox.coreChanges` are deleted. Call sites use `CoreDoor`. The read-only string and four write refusals match. `RouteRegistration.boundChanges` passes `isWritable`. Actors and `ParseThread` stay writable. Decision 0003 describes a data door.

## 3. Arch

1. **Blocker** — [Core creation architecture](plan/core-creation/arch.md) §2 HTTP Adapter, §3 CoreMailbox door. [Api](src/Server/Api.fs) `postEvents` calls `CoreDoor.postEvents`. [TestActor](src/Server/TestActor.fs), [RunAgentActor](src/Server/RunAgentActor.fs), and [GithubTransportActor](src/Server/GithubTransportActor.fs) call `CoreDoor` for posts, state, load, and stop. The external production and HTTP seam is CoreMailbox, or a CoreRuntime-bound member.
2. **Blocker** — [Single event source architecture](plan/single-event-source/arch.md) §2 CoreChanges, §3 postEvents and postGraphOnly. `postEvents` and `postGraphOnly` are `CoreDoor.postEvents` and `CoreDoor.postGraphOnly` in [CoreDoor](src/Server/Core/CoreDoor.fs). [CoreChanges](src/Server/Core/CoreChanges.fs) holds `CoreChangesAccepted` only. [ParseThread](src/Server/ParseThread.fs) and [LazyLoadReconciliationServer](src/Server/LazyLoadReconciliationServer.fs) call `CoreDoor.postGraphOnly`. Those interfaces are on CoreChanges.
3. **Blocker** — [Browser residency architecture](plan/browser-residency/arch.md) §2 Server Sync doors. [Api](src/Server/Api.fs) `getState`, `postEvents`, and the events-since read call `CoreDoor.getState`, `CoreDoor.getEventsSince`, and `CoreDoor.postEvents`. Server Sync doors use CoreChanges `getState` / `getEventsSince` / `postEvents`.

## 4. Totals

- Standards: 6 findings. Worst: public names repeated on `CoreDoor`, `CoreMailbox`, and `CoreMailboxApi`.
- Spec: 8 findings. Worst: `CoreDoor` calls `CoreMailboxApi` instead of `CoreMailbox`.
- Arch: 3 findings. Worst: the HTTP and production seam is `CoreDoor`, not the CoreMailbox door.
