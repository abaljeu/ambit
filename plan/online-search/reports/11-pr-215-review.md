# Code review — 11 Server evaluates — PR 215

Review of `git diff origin/staging...HEAD` for [11 — Server evaluates](../issues/11-server-evaluates.md). The Query Actor evals a Run of an equals line on the server Graph. Node ids ride ActorStop as `ActorQuery`.

## Standards

No findings.

The scan's `item N` lines name the item. `let mutable found` is in a test. [F# source](../../../.agents/rules/fsharp-source.md) does not apply that rule to tests. New functions are under 40 lines. [History.fs](../../../src/Shared/History.fs) stays at 800 lines.

No smells.

## Spec

The ActorStop event the client decodes carries the Node ids. [ActorLive.resultOf](../../../src/Shared/ActorLive.fs) then stores the same success chip as `ActorSucceeded` and does not keep the id list. [11 — Server evaluates](../issues/11-server-evaluates.md) section 4 **Reply** says the client reads the ids from that event.

Run commits an edit and posts the command without waiting for that commit. [CoreMailboxBackend.startChosen](../../../src/Server/Core/CoreMailboxBackend.fs) classifies the line from server State. A just-typed `=` can still be absent there. That race is the same shape as other Run starts. It is larger than a small fix on this ticket.

`functionStart` posts `ActorQuery`. Find still stops as `ActorSucceeded`. No Ref post. [14 — Cap of 200](../issues/14-cap-of-200.md) section 2 **Query cap** stays open. `ActorQuery` is named in section 4 **Reply**.

Standards: no findings. Spec: 2 findings. Worst: the applied chip does not keep the Node ids. The edit-then-Run race is the other.
