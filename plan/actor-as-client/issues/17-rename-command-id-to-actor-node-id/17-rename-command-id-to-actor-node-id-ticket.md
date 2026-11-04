# 17 — Rename commandId to actorNodeId
**Status**: defined
One job: The actor node's id is `actorNodeId`. Editor commands keep the name `CommandId`.

[ ] This ticket runs before 06 — Log ActorOrder. 06 depends on it. It does not add `ActorOrder`.
[ ] The source files are 8. The test files are 30. The size limit is 8 source files and 4 test files. The rename does not compile in slices, so this stays one ticket. The test count is over the limit.
[x] Alan approved the over-limit size on 2026-11-04. The build does not stop for size.
[ ] `commandOnOwnerPath` becomes `actorNodeOnOwnerPath`. The private binding `noRunnableCommand` becomes `noRunnableActor`. Both edits stay in `CommandRequest.fs` and `CommandRequestTests.fs`, which are already in this ticket.
[ ] The Run error string stays `No command found`. `isCommandText` keeps its name.

Type:

[ ] coding

Blockers:

[ ] none

## Inputs

### Working

[ ] `ActorStart.commandId` is in `src/Shared/History.fs`.
[ ] `CommandRequest.actorStart` takes that field in `src/Shared/CommandRequest.fs`.
[ ] `EventJson` encodes it in `src/Shared/EventJson.fs`. The wire key is `"commandId"`.
[ ] `ActorInput.commandId`, `FunctionStart.commandId`, and the live-row field are in `src/Server/Core/CoreActorPool.fs`. `commandIsLive` uses that field.
[ ] `SearchActor`, `TestActor`, and `RunAgentActor` read `commandId` from the Actor input or the start.
[ ] `src/Client/UpdateWorkspaceLoad.fs` sets `commandId` on an `ActorStart`.
[ ] `rg commandId` also hits `CommandId`, `commandIds`, and `GrokBotWakeArgs.CommandId`. Those are editor commands or the wake payload. They are not this field.

### Reference

[ ] [Arch](../../arch.md). `actorNodeId` is the field on `ActorStart`, `ActorInput`, `FunctionStart`, and the pool live row. The Run endpoint closes over that id in its `FunctionActor`.

## Do not load

[ ] Do not rename `CommandId`, `commandIds`, `dockCommandIds`, or `isCommandText`.
[ ] Do not rename `GrokBotWakeArgs.CommandId`.
[ ] Do not rename load-save command names or `/ambit/load-save-command`.
[ ] Do not change the wire key `"commandId"` in `EventJson` or `GrokBotHttp`.
[ ] Do not change the Run error string `No command found`.
[ ] Do not add `ActorOrder`. That is 06 — Log ActorOrder.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: Rename the actor node field from `commandId` to `actorNodeId` on `ActorStart`, `ActorInput`, `FunctionStart`, and the pool live row.
3. [ ] Change: Rename `commandOnOwnerPath` to `actorNodeOnOwnerPath`. Rename the private binding `noRunnableCommand` to `noRunnableActor`. Leave the string `No command found`.
4. [ ] `EventJson` still writes and reads the wire key `"commandId"`.
5. [ ] Update each test file in Outputs that constructs or reads that field, including the `commandOnOwnerPath` tests.
6. [ ] Leave `CommandId`, `isCommandText`, load-save command names, and the Grok wake field as they are.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `src/Shared/CommandRequest.fs`
[ ] `src/Shared/EventJson.fs`
[ ] `src/Server/Core/CoreActorPool.fs`
[ ] `src/Server/SearchActor.fs`
[ ] `src/Server/TestActor.fs`
[ ] `src/Server/RunAgentActor.fs`
[ ] `src/Client/UpdateWorkspaceLoad.fs`
[ ] `tests/Shared.Tests/SyncLogicTests.fs`
[ ] `tests/Shared.Tests/MailboxLogTests.fs`
[ ] `tests/Shared.Tests/RunEditCommitTests.fs`
[ ] `tests/Shared.Tests/SerializationTests.fs`
[ ] `tests/Shared.Tests/ActorLiveTests.fs`
[ ] `tests/Shared.Tests/CommandRequestTests.fs`
[ ] `tests/Shared.Tests/EventJsonTests.fs`
[ ] `tests/Shared.Tests/EventTests.fs`
[ ] `tests/Server.Tests/GithubTransportActorTests.fs`
[ ] `tests/Server.Tests/Issue41CoreMailboxTests.fs`
[ ] `tests/Server.Tests/Issue42PersistHandlersTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandClientTests.fs`
[ ] `tests/Server.Tests/LoadSaveCommandTests.fs`
[ ] `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`
[ ] `tests/Server.Tests/OrderedEventStreamTests.fs`
[ ] `tests/Server.Tests/ParseThreadLoadTests.fs`
[ ] `tests/Server.Tests/PersistHandlersRestoreTests.fs`
[ ] `tests/Server.Tests/QueryActorTests.fs`
[ ] `tests/Server.Tests/SearchActorTests.fs`
[ ] `tests/Server.Tests/TestActorCommandErrorTests.fs`
[ ] `tests/Server.Tests/TestActorHelloTests.fs`
[ ] `tests/Server.Tests/AgentAskTests.fs`
[ ] `tests/Server.Tests/ApiPostCommandTests.fs`
[ ] `tests/Server.Tests/AskCancelHarness.fs`
[ ] `tests/Server.Tests/CancelByFocusTests.fs`
[ ] `tests/Server.Tests/CoreActorPoolDeliverTests.fs`
[ ] `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs`
[ ] `tests/Server.Tests/CoreMailboxDoorTests.fs`
[ ] `tests/Server.Tests/CoreMsgActorCasesTests.fs`
[ ] `tests/Server.Tests/ActorCoreChangesDoorTests.fs`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/17-rename-command-id-to-actor-node-id-stop.md`, then ask.
[ ] Stop when the wire key `"commandId"` changes.
[ ] Stop when `CommandId` or `GrokBotWakeArgs.CommandId` is renamed.
[ ] Stop when a file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test file in Outputs.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/17-rename-command-id-to-actor-node-id-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
