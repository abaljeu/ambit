# 28 — Limit the AI extract to the visible list
**Status**: defined
One job: A person who runs `?ai` sends the AI the Included context: the Zoom root and the descendants under expanded rows, stopping at a folded row.
`ActorStart.graphIds` already holds that list, and this ticket makes the `?ai` extract use it.

[ ] This ticket starts without waiting. `queueAdmittedAi` already calls `RunAgentActor.aiActorOrder` with the posted `ActorStart`.
[ ] This ticket is the `?ai` extract.

Type:

[x] coding

Blockers:

[ ] none

## Inputs

### Working

[x] `CommandRequest.actorStart` in `src/Shared/CommandRequest.fs` sets `ActorStart.graphIds` with `IncludedDescendantIds.throughChildrenOfExpandedNodes`.
[x] `IncludedDescendantIds.throughChildrenOfExpandedNodes` in `src/Shared/IncludedDescendantIds.fs` lists the Zoom root and each child of an expanded row. A folded row is in that list. The walk stops there, so that row's descendants stay out of the list.
[x] `queueAdmittedAi` in `src/Server/Core/CoreMailboxActors.fs` calls `RunAgentActor.aiActorOrder` with that `ActorStart`.
[x] `RunAgentActor.aiActorOrder` in `src/Server/RunAgentActor.fs` calls `ActorOrderFromStart.orderFromStart` with `RunAgentActor.actorFn` applied to that `start`.
[x] `ActorOrder` in `src/Shared/History.fs` stores `actor`, `actorNodeId`, and `actorName`.
[x] `RunAgentActor.actorFn` closes over that `start`, so `start.graphIds` is already in the function.
[x] `functionStartOf` in `src/Server/Core/CoreMailboxLoop.fs` sets `getGraph` to `CoreMailboxEvents.graphNow`.
[x] `CoreMailboxEvents.graphNow` in `src/Server/Core/CoreMailboxEvents.fs` returns the loaded graph.
[x] `RunAgentActor.inputFor` sets `graph` from `run.getGraph ()` and copies `zoomId`, `focusId`, and `runDefinitionId` from `start`.
[x] `RunAgentActor.packExtract` calls `AiExtractPack.packExtract` with `input.graph`, `input.zoomId`, and `input.focusId`.
[x] `AiExtractPack.packExtract` in `src/Server/AiExtractPack.fs` walks every child under `zoomId`.
[x] `AiExtractPack.writePresent` reads `Graph.children` and omits an id that has no node.
[x] `CoreActorPool.actorGraphFrom` in `src/Server/Core/CoreActorPool.fs` is private.
[x] `putActorStart` and `runStartActor` call `actorGraphFrom`.
[x] `actorGraphFrom` keeps the nodes whose ids are in `request.graphIds`, copies those nodes' child lists from the full graph, and builds the graph with `Graph.fromExtracted` at `request.zoomId`.
[x] `withFake` and `startAiRun` in `tests/Server.Tests/AskCancelHarness.fs` start `?ai` and show the prompt.
[x] `tests/Server.Tests/AiExtractPackTests.fs` already calls `AiExtractPack.packExtract`.

### Reference

[x] [Arch](../../arch.md).
[x] Included context in [GLOSSARY.md](../../../../GLOSSARY.md).

## Do not load

[x] `CommandRequest.actorStart` and `IncludedDescendantIds.throughChildrenOfExpandedNodes`.
[x] `ActorOrder` and `ActorOrderFromStart.orderFromStart`.
[x] `TestActor`, `SearchActor`, and `getGraph` for any actor other than this extract.
[x] [02 — Graph handoff](../02-graph-handoff.md) and [04 — Start surface](../04-start-surface.md) keep the later question of dropping `graphIds`.
[x] `tests/Server.Tests/AskCancelHarness.fs`.
[x] The body of `actorGraphFrom`.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change: make `CoreActorPool.actorGraphFrom` public. `putActorStart` and `runStartActor` still call it. The body stays.
3. [x] Change: pass `start` from `RunAgentActor.actorFn` through `runBody` to `packExtract`. `packExtract` calls `CoreActorPool.actorGraphFrom input.graph start` and passes that graph to `AiExtractPack.packExtract`.
4. [x] Leave `RunAgentActor.inputFor`. `input.graph` stays `run.getGraph ()`. `commandArgs`, `planOnCurrent`, and `postAdd` keep reading that graph.
5. [x] Add ``pack omits a folded child's descendants and keeps an expanded child's`` in `tests/Server.Tests/AiExtractPackTests.fs`. The graph has Zoom, an expanded row, that row's child, a folded row, and that row's child. `graphIds` contains Zoom, the expanded row, the expanded row's child, and the folded row. `graphIds` omits the folded row's child. The child is on the graph. The test calls `CoreActorPool.actorGraphFrom`, then `AiExtractPack.packExtract`. The text contains the expanded row's child and the folded row. The text omits the folded row's child.
6. [x] Add ``?ai prompt keeps an expanded child's text and omits a folded child's descendant`` in `tests/Server.Tests/AgentAskTests.fs`. The server graph has the same rows plus a `?ai` command node. `ActorStart.graphIds` contains Zoom, the command node, the expanded row, the expanded row's child, and the folded row, and it omits the folded row's child. The test posts that graph with `CoreMailboxApi.postGraphOnly`, starts through `startAiRun` inside `withFake`, and reads the prompt. The prompt contains the expanded row's child and the folded row. The prompt omits the folded row's child.

## Outputs

[x] `src/Server/Core/CoreActorPool.fs`
[x] `src/Server/RunAgentActor.fs`
[x] `tests/Server.Tests/AiExtractPackTests.fs` `--filter` ``pack omits a folded child's descendants and keeps an expanded child's``
[x] `tests/Server.Tests/AgentAskTests.fs` `--filter` ``?ai prompt keeps an expanded child's text and omits a folded child's descendant``

## Stop when

[x] Rewrite `28-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when `ActorOrder` gains a field.
[ ] Stop when `input.graph` becomes the cut graph.
[ ] Stop when a second cut is written.
[ ] Stop when `putActorStart` or `runStartActor` stops calling `actorGraphFrom`.
[ ] Stop when the extract contains a node whose id is absent from `start.graphIds`.
[ ] Stop when the extract omits the folded row, or omits a descendant of an expanded row, while that id is in `start.graphIds`.
[ ] Stop when the change passes 8 source files or 4 test files. [Size limit](../../../context/size-limit.md).

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``pack omits a folded child's descendants and keeps an expanded child's``
  - `tests/Server.Tests`: ``?ai prompt keeps an expanded child's text and omits a folded child's descendant``
[x] All executed tests pass.
[x] Rewrite `28-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
