# 17 — Rename commandId to actorNodeId
**Status**: coded
One job: The actor node's id is `actorNodeId`. Editor commands keep the name `CommandId`.

[ ] This ticket runs before 06 — Log ActorOrder. 06 depends on it. It does not add `ActorOrder`.
[ ] The source files are 8. The test files are 30. The size limit is 8 source files and 4 test files. The rename does not compile in slices, so this stays one ticket. The test count is over the limit.
[x] Alan approved the over-limit size on 2026-11-04. The build does not stop for size.
[x] `commandOnOwnerPath` becomes `actorNodeOnOwnerPath`. The private binding `noRunnableCommand` becomes `noRunnableActor`. Both edits stay in `CommandRequest.fs` and `CommandRequestTests.fs`, which are already in this ticket.
[x] The Run error string stays `No command found`. `isCommandText` keeps its name.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[x] `ActorStart.commandId` is in `src/Shared/History.fs`.
[x] `CommandRequest.actorStart` takes that field in `src/Shared/CommandRequest.fs`.
[x] `EventJson` encodes it in `src/Shared/EventJson.fs`. The wire key is `"commandId"`.
[x] `ActorInput.commandId`, `FunctionStart.commandId`, and the live-row field are in `src/Server/Core/CoreActorPool.fs`. `commandIsLive` uses that field.
[x] `SearchActor`, `TestActor`, and `RunAgentActor` read `commandId` from the Actor input or the start.
[x] `src/Client/UpdateWorkspaceLoad.fs` sets `commandId` on an `ActorStart`.
[x] `rg commandId` also hits `CommandId`, `commandIds`, and `GrokBotWakeArgs.CommandId`. Those are editor commands or the wake payload. They are not this field.

### Reference

[x] [Arch](../../arch.md). `ActorOrder.actorNodeId` is the actor node's id.

## Do not load

[x] Do not rename `CommandId`, `commandIds`, `dockCommandIds`, or `isCommandText`.
[x] Do not rename `GrokBotWakeArgs.CommandId`.
[x] Do not rename load-save command names or `/ambit/load-save-command`.
[x] Do not change the wire key `"commandId"` in `EventJson` or `GrokBotHttp`.
[x] Do not change the Run error string `No command found`.
[x] Do not add `ActorOrder`. That is 06 — Log ActorOrder.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change: Rename the actor node field from `commandId` to `actorNodeId` on `ActorStart`, `ActorInput`, `FunctionStart`, and the pool live row.
3. [x] Change: Rename `commandOnOwnerPath` to `actorNodeOnOwnerPath`. Rename the private binding `noRunnableCommand` to `noRunnableActor`. Leave the string `No command found`.
4. [x] `EventJson` still writes and reads the wire key `"commandId"`.
5. [x] Update each test file in Outputs that constructs or reads that field, including the `commandOnOwnerPath` tests.
6. [x] Leave `CommandId`, `isCommandText`, load-save command names, and the Grok wake field as they are.

## Outputs

[x] `src/Shared/History.fs`
[x] `src/Shared/CommandRequest.fs`
[x] `src/Shared/EventJson.fs`
[x] `src/Server/Core/CoreActorPool.fs`
[x] `src/Server/SearchActor.fs`
[x] `src/Server/TestActor.fs`
[x] `src/Server/RunAgentActor.fs`
[x] `src/Client/UpdateWorkspaceLoad.fs`
[x] `tests/Shared.Tests/SyncLogicTests.fs`
[x] `tests/Shared.Tests/MailboxLogTests.fs`
[x] `tests/Shared.Tests/RunEditCommitTests.fs`
[x] `tests/Shared.Tests/SerializationTests.fs`
[x] `tests/Shared.Tests/ActorLiveTests.fs`
[x] `tests/Shared.Tests/CommandRequestTests.fs`
[x] `tests/Shared.Tests/EventJsonTests.fs`
[x] `tests/Shared.Tests/EventTests.fs`
[x] `tests/Server.Tests/GithubTransportActorTests.fs`
[x] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[x] `tests/Server.Tests/Issue42PersistHandlersTests.fs`
[x] `tests/Server.Tests/LoadSaveCommandClientTests.fs`
[x] `tests/Server.Tests/LoadSaveCommandTests.fs`
[x] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[x] `tests/Server.Tests/OrderedEventStreamTests.fs`
[x] `tests/Server.Tests/ParseThreadLoadTests.fs`
[x] `tests/Server.Tests/PersistHandlersRestoreTests.fs`
[x] `tests/Server.Tests/QueryActorTests.fs`
[x] `tests/Server.Tests/SearchActorTests.fs`
[x] `tests/Server.Tests/TestActorCommandErrorTests.fs`
[x] `tests/Server.Tests/TestActorHelloTests.fs`
[x] `tests/Server.Tests/AgentAskTests.fs`
[x] `tests/Server.Tests/ApiPostCommandTests.fs`
[x] `tests/Server.Tests/AskCancelHarness.fs`
[x] `tests/Server.Tests/CancelByFocusTests.fs`
[x] `tests/Server.Tests/CoreActorPoolDeliverTests.fs`
[x] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs`
[x] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[x] `tests/Server.Tests/CoreMsgActorCasesTests.fs`
[x] `tests/Server.Tests/ActorCoreChangesDoorTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/17-rename-command-id-to-actor-node-id-stop.md`, then ask.
[ ] Stop when the wire key `"commandId"` changes.
[ ] Stop when `CommandId` or `GrokBotWakeArgs.CommandId` is renamed.
[ ] Stop when a file outside Outputs must change.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test file in Outputs.
[x] All executed tests pass.
[x] Write the verdict to `plan/actor-as-client/reports/17-rename-command-id-to-actor-node-id-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
