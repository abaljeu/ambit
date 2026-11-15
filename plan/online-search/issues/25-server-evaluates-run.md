# 25 — The server evaluates Run
**Status:** defined
One job: The Server evaluates a Run line on the Server Graph and posts the Ref children and the Text children. The Browser applies those ops from the event log. Alan, 2026-11-15, because the residency holds only the Browser Graph, and the plan already built by `ExprRun.run` is the result that belongs on the Server. The Query spec states the server-eval lock in [spec](../spec.md).

Type: coding
Blockers:

STOP: [22 — Run sends = lines to Amble](22-run-sends-equals-lines-to-amble.md) is defined and not started. Its target runs Amble on the client. A focused line that contains `=` still starts the query actor and returns node ids.

[24 — Remove CoreMsg.RecordSearchStop](24-remove-coremsg-recordsearchstop.md) removes `CoreMsg.RecordSearchStop` on Find. That ticket shares no file with this one, and it does not block this ticket.

22 — Run sends = lines to Amble chooses the line. A line that contains `=` runs Amble. A line that starts with `?` starts that Actor. The owner scan stays in that ticket. This ticket does not restate those door rules. This ticket is the place the Amble plan runs.

[16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md) posts Ref children from the query actor. This ticket posts the same plan, and that plan also carries Text children. The Browser does not build the plan on the residency Graph.

[Amble tools](../../bot-amble-tools/project.md) runs `ExprRun.run` on the graph the actor posts to, and it posts that plan. That project depends on this server execution. Search Amble is draft pull request 623. Its folder `plan/search-amble` is not on staging. It evaluates an expression on the Server Graph for the Find and Move dialog, and it lists at most 50 Node Answers. It depends on server-side evaluation. It does not write the outline children this ticket posts.

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
[x] `ExprRun.maxMaterialisedAnswers` in `src/Shared/ExprRun.fs` is 50. `ExprRun.run` materialises at most that many Answers.
[x] `ExprRun.addAnswer` emits `ChildNode.reference` for a Node Answer that is present on the Graph. It emits `Op.NewNode` and `ChildNode.owner` for a Text Answer.
[x] `SearchActor.queryActorBody` in `src/Server/SearchActor.fs` returns `ActorQuery` of `ExprRun.answerNodeIds` on the Server Graph. It does not call `ExprRun.run`. It does not post ops.
[x] `SearchActor.functionActorAt` and `SearchActor.functionStart` call `queryActorBody`. `getGraph` supplies the Server Graph.
[x] `CoreMailboxActors.admittedQueue` queues that actor when `SearchActor.isQueryRequest` is true. The actor name is `query`.
[x] `ActorChanges.postChanges` accepts an event list. `RunAgentActor` posts one `EventBody.Change` through that function. `CoreDoor.actorChanges` builds `ActorChanges` from the pool door.
[x] `tests/Server.Tests/QueryActorTests.fs` test ``ActorStop reads the carrier graph once`` expects one `getGraph` call and `ActorQuery` of the named node id.
[x] ``empty equals line stops with no node ids`` expects `ActorQuery` of an empty id list for a line whose text is `=`.

### Reference

[x] Alan, 2026-11-15: the Server evaluates the Run line and produces the ops. The Browser receives those ops on the event log.
[x] [Size limit](../../context/size-limit.md) allows 8 source files and 4 tests.
[x] The Query spec named in the One job line locks server evaluation. Section 2 says results are Refs, the cap is 200, and Owned Children are out of scope.

## Do not load

[ ] The owner scan, the `?` Actor start, and the removal of the query-by-`=` path. Those rules stay in 22 — Run sends = lines to Amble.
[ ] `CoreMsg.RecordSearchStop` and the Find stop path in 24 — Remove CoreMsg.RecordSearchStop.
[ ] A line that starts with `>`. `AmbleRun.legacyRun` stays the client plan for that line.
[ ] `src/Shared/History.fs`, `src/Shared/EventJson.fs`, and `src/Shared/ActorLive.fs`. `ActorStop` stays `ActorQuery`.
[ ] The Find and Move walk in `SearchActor.reply`.
[ ] Expression-language files, including [14 — Server-side search](../../expression-language/issues/14-server-side-search.md).
[ ] `plan/search-amble` and the files on draft pull request 623.
[ ] `AmbleRun.applyUnfold`. Unfold stays a Browser SiteMap step. This ticket does not post it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] If the STOP line in Blockers is still true, stop and leave every source file unchanged.
3. [ ] Change `queryActorBody` in `src/Server/SearchActor.fs` so the actor reads the Server Graph once, calls `AmbleRun.runPlan` for that line, and posts `plan.ops` as one `EventBody.Change` through `ActorChanges.postChanges`. The event shape matches the Change that `RunAgentActor` posts. `functionActorAt` passes its `ActorChanges` into that body. `functionStart` builds `ActorChanges` with `CoreDoor.actorChanges` and passes that same body.
4. [ ] Change that body so `ActorQuery` carries the ids of the Ref children in the posted plan. A Text child stays in the ops and stays out of the id list. The actor returns that `ActorQuery` after the post.
5. [ ] Change `execAmbleRunOp` in `src/Client/Commands.fs` so a run statement does not call `runAmbleOp`. A focused run statement already queues `ActorStart` in `submitQuery`. That queue stays the only evaluation of the line.
6. [ ] Change `runAmbleOp` in `src/Client/UpdateAmbleRun.fs` so a run statement returns the committed model and the commit effects. That return does not call `runPlanOnNode`.

## Outputs

[ ] Source files: `src/Server/SearchActor.fs`, `src/Client/Commands.fs`, and `src/Client/UpdateAmbleRun.fs`.
[ ] That list is 3 source files. The limit in Reference names 8.
[ ] `tests/Server.Tests/QueryActorTests.fs` adds ``server run posts the Ref child for a Node Answer``. The line is `= root descendant named "Bob"`. The event log contains `Op.Replace` whose new children include `ChildNode.reference` of that node. ``ActorStop reads the carrier graph once`` still expects one `getGraph` call and `ActorQuery` of that node id.
[ ] The same file adds ``server run posts an owned child for a Text Answer``. The line is `= text`. The event log contains `Op.NewNode` and `ChildNode.owner` for that Text. `ActorQuery` does not contain that new id.
[ ] That list is 2 tests added. The same limit names 4 tests.

## Stop when

[ ] Rewrite `25-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit drops Text children or raises the plan cap from `ExprRun.maxMaterialisedAnswers` to 200. The Query spec still says Refs only and a cap of 200. Those two sentences are not this plan.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorStop reads the carrier graph once``
  - `tests/Server.Tests`: ``empty equals line stops with no node ids``
  - `tests/Server.Tests`: ``server run posts the Ref child for a Node Answer``
  - `tests/Server.Tests`: ``server run posts an owned child for a Text Answer``
[ ] All executed tests pass.
[ ] Rewrite `25-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
