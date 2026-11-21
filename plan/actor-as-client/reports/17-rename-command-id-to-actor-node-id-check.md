pass

## 17 — Rename commandId to actorNodeId

Branch `cursor/rename-command-id-actor-node-id-dd05`. The actor node id field is `actorNodeId` on `ActorStart`, `ActorInput`, `FunctionStart`, and the pool live row. `CommandRequest.actorNodeOnOwnerPath` replaces `commandOnOwnerPath`. The private binding is `noRunnableActor`. The string stays `No command found`. The wire key stays `"commandId"`.

## Build

[x] `dotnet build gambol.sln` succeeded. 0 warnings. 0 errors.

## Full suite

[x] `scripts/agent-test.sh --full` log `/tmp/ambit-full-suite-20261104-143052.log`.
[x] `tests/Server.Tests` passed 742, failed 0, skipped 1.
[x] `tests/Shared.Tests` passed 1931, failed 0, skipped 3.
[x] `dotnet test tests/CloudAgents.Tests -p:AMBIT_FULL_SUITE=1` passed 62, failed 0, skipped 0.

## Mapped run

[x] `scripts/agent-test.sh` with no arguments passed.
[x] `tests/Server.Tests` filter passed 221, failed 0, skipped 0.
[x] `tests/Shared.Tests` filter passed 274, failed 0, skipped 0.
[x] The mapped filter also named `CoreActorPoolTests` and `HistoryTests` because `CoreActorPool.fs` and `History.fs` changed. Those files were not edited.

## Per-file filter

[x] `scripts/agent-test.sh --filter` ran for each test file in Outputs. Log `/tmp/ticket17-filters.log`. Failed 0.
[x] `tests/Server.Tests/Issue42PersistHandlersTests.fs` matched no test. The file is not a `Compile` item in `tests/Server.Tests/Gambol.Server.Tests.fsproj`.
[x] `tests/Server.Tests/AskCancelHarness.fs` matched no test. The file has no facts. `AgentAskTests` passed 6. `CancelByFocusTests` passed 9.

## Departures

[x] Inside Output files, every identifier `commandId` that named the actor node id became `actorNodeId`. That includes local bindings, `Seed.actorNodeId`, `SeededAsk.actorNodeId`, helper parameters, and fact names.
[x] `commandIsLive` keeps its name. Its parameter is `actorNodeId`.
[x] The doc comment on `actorNodeOnOwnerPath` says `Actor node id`.
[x] The red-green loop was one rename. [17 — Rename commandId to actorNodeId](../issues/17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md) says the rename does not compile in slices.
[x] `Issue42PersistHandlersTests.fs` stayed out of the fsproj. Adding that `Compile` item is outside Outputs.
