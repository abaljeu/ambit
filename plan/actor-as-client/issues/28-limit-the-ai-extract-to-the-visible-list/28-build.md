Check: pass

`CoreActorPool.actorGraphFrom` is public. `putActorStart` and `runStartActor` still call it. The body is unchanged.

`RunAgentActor.actorFn` passes `start` through `runBody` to `packExtract`. `packExtract` calls `CoreActorPool.actorGraphFrom input.graph start` and passes that graph to `AiExtractPack.packExtract`.

`RunAgentActor.inputFor` still sets `input.graph` from `run.getGraph ()`. `commandArgs`, `planOnCurrent`, and the replace path still read that graph.

``pack omits a folded child's descendants and keeps an expanded child's`` calls `CoreActorPool.actorGraphFrom`, then `AiExtractPack.packExtract`. The text contains the expanded row's child and the folded row. The text omits the folded row's child. That child stays on the full graph.

``?ai prompt keeps an expanded child's text and omits a folded child's descendant`` posts the server graph with `CoreMailboxApi.postGraphOnly`, starts through `startAiRun` inside `withFake`, and reads the prompt. The prompt contains the expanded row's child and the folded row. The prompt omits the folded row's child.

## Suites

- `scripts/agent-test.sh` with no arguments passed 16, failed 0, skipped 0.
- ``pack omits a folded child's descendants and keeps an expanded child's`` passed 1, failed 0, skipped 0.
- ``?ai prompt keeps an expanded child's text and omits a folded child's descendant`` passed 1, failed 0, skipped 0.
- `tests/Server.Tests` with `AMBIT_FULL_SUITE=1` and `-nodeReuse:false` passed 764, failed 0, skipped 1. Total 765.
- `tests/Shared.Tests` with `AMBIT_FULL_SUITE=1` and `-nodeReuse:false` passed 1940, failed 0, skipped 3. Total 1943.
