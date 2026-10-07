# Code review — 11 Server evaluates — PR 215

Review of `git diff origin/staging...HEAD` for [11 — Server evaluates](../issues/11-server-evaluates.md). Commits: lock the ticket on the Run command, then eval that line on the server Graph.

## Standards

Hard violation. [Refer by name](../../../.agents/rules/refer-by-name.md): never identify a section only by number. [11 — Server evaluates](../issues/11-server-evaluates.md) said architecture §2 item 2 with no name on that item. The same sentence already named **Query Actor**. The line now says §2 item 2 **Query Actor**.

The scan's other `item N` lines name the item (**Query Actor**, **Eval**, **Remote query eval**). They do not break the rule. `let mutable found` is in a test. [F# source](../../../.agents/rules/fsharp-source.md) does not apply that rule to tests. Function sizes in the diff are under 40 lines.

No smells.

## Spec

The Node-id reply does not leave the server. [11 — Server evaluates](../issues/11-server-evaluates.md) section 4 **Reply** says the observable reply is Node ids and shows `{ "ids": [...] }`. Section 5 item 3 **ActorStop** says the result of the eval is the Node ids. The HTTP body of `POST /ambit/command` stays the existing command response.

[SearchActor.runQuery](../../../src/Server/SearchActor.fs) builds that list, then posts `ActorStop` with `ActorSucceeded` and no ids. [SearchActor.functionStart](../../../src/Server/SearchActor.fs) drops the returned list. [ActorStop](../../../src/Shared/History.fs) is `focusId * ActorResult`. [Api.postCommand](../../../src/Server/Api.fs) still encodes `{ nodes; events; latestId }`. [App.runSubmitCommand](../../../src/Client/App.fs) dispatches `CommandDone` with those events only. Tests call `queryReply` again to read ids. The client cannot see the list.

[ExprRun.answerNodeIds](../../../src/Shared/ExprRun.fs) uses `ExprEval.toList`. [14 — Cap of 200](../issues/14-cap-of-200.md) section 2 **Query cap** still owns that stop. This diff does not close those checkboxes.

Standards: 1 finding. Worst: architecture §2 item 2 **Query Actor** had no name on that item. That line now names **Query Actor**. Spec: 1 finding. Worst: the client cannot see the Node ids. That reply stays a gap. `ActorStop` carries `ActorSucceeded`. The command response stays `{nodes, events, latestId}`.

A later `dotnet test tests/Server.Tests -c Debug --filter FullyQualifiedName~QueryActorTests` run printed `Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 225 ms`. That run still reads ids from `queryReply`.
