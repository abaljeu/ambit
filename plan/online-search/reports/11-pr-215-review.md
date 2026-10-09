# Code review — Server evaluates

Review of `git diff origin/staging...HEAD` for [Server evaluates](../issues/11-server-evaluates.md). A `=` line queues ActorStart behind the edit. The list turn classifies from the post-edit State.

## Standards

[History.fs](../../../src/Shared/History.fs) drops the blank line between `State` and `ApplyResult`. [Core agent behavior](../../../.agents/rules/core-agent-behavior.md) says do not change adjacent formatting. [F# source](../../../.agents/rules/fsharp-source.md) says 800 lines or less. The file is 800 with that blank removed.

No smells on the tip. The empty-`graphIds` duplicate in `startChosen` is gone. `queueRun` is the one queue call. `idsFromSource` uses `ExprCompile.evalOutcome`.

## Spec

No findings.

## Arch

No findings.

Standards: 1 finding, the History.fs blank line. Spec: no findings. Arch: no findings.
