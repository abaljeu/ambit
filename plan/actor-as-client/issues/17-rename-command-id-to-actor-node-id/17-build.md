# 17 — Rename commandId to actorNodeId build

Ticket: [[17-rename-command-id-to-actor-node-id-ticket.md]]

## Stage

[x] Stage is build in [17 — Rename commandId to actorNodeId](CONTEXT.md).

## Edits

[x] `ActorStart.actorNodeId` in `src/Shared/History.fs`.
[x] `CommandRequest.actorStart` takes `actorNodeId` in `src/Shared/CommandRequest.fs`.
[x] `CommandRequest.actorNodeOnOwnerPath` replaces `commandOnOwnerPath`.
[x] The private binding `noRunnableActor` holds `No command found`.
[x] `EventJson.startRequestFields` writes `start.actorNodeId` under the wire key `"commandId"`.
[x] `ActorInput.actorNodeId`, `FunctionStart.actorNodeId`, and the live-row field are `actorNodeId` in `src/Server/Core/CoreActorPool.fs`.
[x] `SearchActor`, `TestActor`, and `RunAgentActor` read `actorNodeId`.
[x] `RunAgentActor.completeGrok` still sets `GrokBotWakeArgs.CommandId`.
[x] `src/Client/UpdateWorkspaceLoad.fs` sets `actorNodeId` on the `ActorStart`.
[x] Each test file in Outputs that constructed or read the field now uses `actorNodeId`.

## Check

[x] Verdict is `pass` in [17 — Rename commandId to actorNodeId check](plan/actor-as-client/reports/17-rename-command-id-to-actor-node-id-check.md).

## Status

[x] Ticket status is `coded`.
