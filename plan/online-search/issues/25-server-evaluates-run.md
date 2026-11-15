# 25 — The server evaluates Run
**Status:** defined
One job: The Server evaluates a Run line on the full database and returns the existing Node ids on `ActorQuery`. The Focus node's children stay as they are. The Query spec states that return in [spec](../spec.md).

Type: coding
Blockers: none

[ ] [22 — Run sends = lines to Amble](22-run-sends-equals-lines-to-amble.md) chooses which line runs Amble.
[ ] This ticket is where the plan runs.
[ ] [24 — Remove CoreMsg.RecordSearchStop](24-remove-coremsg-recordsearchstop.md) stays the Find stop.
[ ] [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md) inserts Refs under the query line.
[ ] This ticket returns existing Node ids. The Focus node's children stay as they are.
[ ] [Amble tools](../../bot-amble-tools/project.md) follows this server evaluation.

## Inputs

### Working

[x] `execRunOp` in `src/Client/Commands.fs` calls `submitQuery` before `execAmbleRunOp`.
[x] `submitQuery` calls `CommandRequest.tryQueryStart`. A focused line whose text contains `=` and does not start with `?` queues `ActorStart` through `queueRun`. That path does not call `runAmbleOp`.
[x] `execAmbleRunOp` in `src/Client/Commands.fs` calls `runAmbleOp` when `AmbleRun.shouldExec` is true.
[x] `runAmbleOp` in `src/Client/UpdateAmbleRun.fs` calls `AmbleRun.runPlanOnNode` with `committed.graph`. That graph is the Browser Graph.
[x] `applyRunPlan` in `src/Client/UpdateAmbleRun.fs` calls `applyAndPost` with `plan.ops`. The Browser applies those ops and posts them.
[x] `AmbleRun.runPlanOnNode` in `src/Shared/AmbleRun.fs` reads the focused node's text and calls `AmbleRun.runPlan`.
[x] `AmbleRun.runPlan` calls `ExprRun.run`. `ExprRun.Apply` becomes the plan. A line that starts with `>` and is not a run statement stays on `legacyRun`.
[x] `AmbleRun.shouldExec` is true when the focused line is a run statement or the trimmed line starts with `>`.
[x] `ExprRun.maxMaterialisedAnswers` in `src/Shared/ExprRun.fs` is 50. `ExprRun.run` takes at most that many Answers through `ExprEval.take`.
[x] `ExprRun.addAnswer` emits `ChildNode.reference` for a Node Answer that is present on the Graph. It emits `Op.NewNode` and `ChildNode.owner` for a Text Answer. `ExprRun.run` is that materialisation. This ticket leaves that function in place and does not call it.
[x] `ExprRun.answerNodeIds` returns Node ids of Node Answers that exist on the graph. `nodeIdOf` returns a Node id only for `ExprAnswer.Node` when `graph.nodes` contains that id. A Text Answer yields no Node id.
[x] `ExprDialog.tryHits` in `src/Shared/ExprDialog.fs` returns an empty list when `ExprCompile.evalOutcome` is `Hits` of `ExprAnswerType.Text`. A Node Answer becomes a row with that Node's id, text, and name.
[x] `SearchActor.queryActorBody` in `src/Server/SearchActor.fs` returns `ActorQuery` of `ExprRun.answerNodeIds` on the Server Graph. It reads that graph once. It posts no ops.
[x] `SearchActor.functionActorAt` and `SearchActor.functionStart` call `queryActorBody`. `getGraph` supplies the Server Graph.
[x] `CoreMailboxActors.admittedQueue` queues that actor when `SearchActor.isQueryRequest` is true. The actor name is `query`.
[x] `tests/Server.Tests/QueryActorTests.fs` test ``ActorStop reads the carrier graph once`` expects one `getGraph` call and `ActorQuery` of the named node id.
[x] ``empty equals line stops with no node ids`` expects `ActorQuery` of an empty id list for a line whose text is `=`.

### Reference

[x] The Server evaluates the Run line on the full database. `ActorQuery` carries the Node ids of existing nodes. This path posts no ops. The Focus node's children stay as they are. The cap is `ExprRun.maxMaterialisedAnswers`.
[x] [Size limit](../../context/size-limit.md) allows 8 source files and 4 tests.
[x] The Query spec named in the One job line states this return, the cap of 50, and the Find word-search cap of 200.

## Do not load

[ ] The owner scan, the `?` Actor start, and which line runs Amble. Those rules stay in 22 — Run sends = lines to Amble.
[ ] `CoreMsg.RecordSearchStop` and the Find stop path in 24 — Remove CoreMsg.RecordSearchStop.
[ ] A line that starts with `>`. `AmbleRun.legacyRun` stays the client plan for that line.
[ ] `src/Shared/History.fs`, `src/Shared/EventJson.fs`, and `src/Shared/ActorLive.fs`. `ActorStop` stays `ActorQuery`.
[ ] The Find and Move walk in `SearchActor.reply`. The Find word-search cap stays 200.
[ ] Expression-language files, including [14 — Server-side search](../../expression-language/issues/14-server-side-search.md).
[ ] `plan/search-amble`.
[ ] The insert of Refs under the query line. That insert stays 16 — Insert Refs under the query line.
[ ] `ExprRun.run` and `ExprRun.addAnswer`. Text Answers stay on that materialisation. This ticket does not call it.
[ ] `AmbleRun.applyUnfold`. This path writes no children.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `queryActorBody` in `src/Server/SearchActor.fs` so the actor reads the Server Graph once and returns `ActorQuery` of the existing Node ids for that line. The actor posts no ops. The Focus node's children stay as they are.
3. [ ] Change that id list so a run whose Answers are all Node Answers returns at most `ExprRun.maxMaterialisedAnswers` ids. That number is 50.
4. [ ] Change `execAmbleRunOp` in `src/Client/Commands.fs` so a run statement does not call `runAmbleOp`. A focused run statement that `submitQuery` queues stays on that queue.
5. [ ] Change `runAmbleOp` in `src/Client/UpdateAmbleRun.fs` so a run statement returns the committed model and the commit effects. That return does not call `runPlanOnNode`.

## Outputs

[ ] Source files: `src/Server/SearchActor.fs`, `src/Client/Commands.fs`, and `src/Client/UpdateAmbleRun.fs`.
[ ] That list is 3 source files. The limit in Reference names 8.
[ ] `tests/Server.Tests/QueryActorTests.fs` adds ``server run returns the existing node id``. The line is `= root descendant named "Bob"`. `ActorQuery` carries that existing node id. The focus node's children stay the children they had. ``ActorStop reads the carrier graph once`` still expects one `getGraph` call and `ActorQuery` of that node id.
[ ] The same file adds ``server run returns at most maxMaterialisedAnswers node ids``. The Answers are Node Answers. `ActorQuery` carries at most `ExprRun.maxMaterialisedAnswers` ids.
[ ] That list is 2 tests added. The same limit names 4 tests.

## Open question

[ ] A Text Answer stays, and this path returns existing Node ids and writes no children. A Text Answer is not an existing node. `ExprRun.answerNodeIds` carries no Node id for a Text Answer. `ExprDialog.tryHits` returns an empty list when the hits are `ExprAnswerType.Text`. The choice is which result `ActorQuery` carries for a Text Answer: no Node id, as those two functions do, or the text, with no new node and no child.

## Stop when

[ ] Rewrite `25-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit posts ops, generates a Ref, or writes a child on the Focus node.
[ ] Stop when the edit raises the cap from `ExprRun.maxMaterialisedAnswers` to 200, or changes the Find word-search cap.
[ ] Stop when the edit removes Text Answers from `ExprRun.run`.
[ ] Stop when the edit chooses the Open question. That choice is open.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorStop reads the carrier graph once``
  - `tests/Server.Tests`: ``empty equals line stops with no node ids``
  - `tests/Server.Tests`: ``server run returns the existing node id``
  - `tests/Server.Tests`: ``server run returns at most maxMaterialisedAnswers node ids``
[ ] All executed tests pass.
[ ] Rewrite `25-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
