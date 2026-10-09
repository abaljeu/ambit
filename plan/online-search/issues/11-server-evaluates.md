# 11 — Server evaluates

**Status:** `coded`
**Type:** coding
**Actual:** 5h
**Blocked by:** [07 — Server completes the picture](07-server-completes-the-picture.md)
**Binding arch:** [Online search architecture](../arch.md), [Single event source architecture](../../single-event-source/arch.md), [Core refinement architecture](../../core-refinement/arch.md)

## Context

A person runs a query line such as `= root descendants with name like "Bob"`. The Query Actor evaluates that line on the server. Story path **Server evaluates** is [Online search architecture](plan/online-search/arch.md) §1. [05 — Query fulfillment while eval stays local](05-query-fulfillment-while-eval-stays-local.md) stays in its current shape. This ticket waits on [07 — Server completes the picture](07-server-completes-the-picture.md) because both Actors live in the locked file `src/Server/SearchActor.fs`.

Alan, 2026-10-07. The door is the existing Run command. This ticket changes that door from local run to remote run. The observable reply is Node ids. The Ref post stays [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md).

## What to build

The Query Actor evaluates the query once, when Run hits a line whose text contains `=`, then stops. A keypress does not start this Actor. Query keeps its own Actor in `src/Server/SearchActor.fs`. It does not use the Find globe. The observable reply is Node ids.

### 1. Query Actor

State, Interface, and Uses for **Query Actor** stay on [Online search architecture](plan/online-search/arch.md) §2 item 2 **Query Actor**. Eval is remote. See [Online search map](plan/online-search/map.md) Decisions so far item 6 **Remote query eval**. The Ref post stays a proposed design for [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md).

1. [x] Server Graph — The Query Actor evaluates the query on the full server Graph once, when the line runs. The eval runs off the mailbox. The Actor then stops.
2. [x] Not a keypress — A keypress does not start this Actor. The Find quiet gap does not start this Actor.
3. [x] Own Actor — Query starts its own Actor in `src/Server/SearchActor.fs`. It does not use the Find and Move Actor or the Find globe.

### 2. Start

The request is the existing Run [ActorStart](src/Shared/History.fs). [CommandRequest.actorStart](src/Shared/CommandRequest.fs) already builds it. The query line is a Node whose text contains `=`. [CommandRequest.isScanStopText](src/Shared/CommandRequest.fs) is that test. An example line is `= root descendants with name like "Bob"`.

1. [x] Query line — Run on that line starts the remote eval. `focusId` is the Focus. When Run is on the query line, `focusId` is that Node. `commandId` is that same Node. `=` is not a `?` actor name.
2. [x] Existing fields — The request keeps `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`. `zoomId` is the zoom. `graphIds` stay the Included expand. The query-line Node is in that start so the server can read its text. `eventId` is the current event id. The line text stays the Node text on the server Graph.

```json
{
  "zoomId": "550e8400-e29b-41d4-a716-446655440000",
  "focusId": "550e8400-e29b-41d4-a716-446655440001",
  "commandId": "550e8400-e29b-41d4-a716-446655440001",
  "graphIds": [
    "550e8400-e29b-41d4-a716-446655440000",
    "550e8400-e29b-41d4-a716-446655440001"
  ],
  "eventId": 0
}
```

### 3. Door

The door is Run on the ordered event list. [22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md) removed `POST /ambit/command`, `POST /ambit/cancel`, and `SubmitCommand`. A `=` line queues its ActorStart on `syncInfo.pending` behind the edit. Find keeps `POST /ambit/search`.

1. [x] Local run today — [execRunOp](src/Client/Commands.fs) sees an Amble scan stop and calls [execAmbleRunOp](src/Client/Commands.fs). [CommandRequest.isAmbleScanStop](src/Shared/CommandRequest.fs) is that stop. Run on the query line is handled before that stop. A Focus under the line stays Amble.
2. [x] Remote run — Run on a Node whose text contains `=` calls [RunLaunch.queueStart](src/Shared/RunLaunch.fs). That appends the ActorStart on `syncInfo.pending` behind the edit. One post goes to `POST /ambit/changes`. `POST /ambit/events` is the alias. Both call [Api.postEvents](src/Server/Api.fs).
3. [x] No new door — This ticket adds no route and no `CoreMsg`. The list turn classifies the line from State after the earlier events in that list. A start error stores ActorStart and ActorStop `ActorFailed`. The list applies whole. Only the list-level credential check refuses.

### 4. Reply

The observable reply is Node ids. They ride ActorStop as `ActorQuery` on [ActorResult](src/Shared/History.fs). [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md) owns the Ref post. The client reads the ids from that ActorStop on the events-door answer or on a later Poll. The applied chip keeps those ids as `CmdLastResult.Query`. Find stays the `ActorSucceeded` chip.

1. [x] Node ids — The eval result is the Node ids of the Node Answers. ActorStop carries them as `ActorQuery`. Tests read that event. [ExprRun](src/Shared/ExprRun.fs) `run` builds a Plan that posts Refs. This ticket returns the Node ids on ActorStop. [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md) owns that Plan.

```json
{
  "kind": "actorStop",
  "focusId": "550e8400-e29b-41d4-a716-446655440001",
  "result": "query",
  "ids": [ "550e8400-e29b-41d4-a716-446655440002" ]
}
```

### 5. Lifecycle

1. [x] ActorStart — The list turn records ActorStart before the eval. Classification reads the post-edit State in that turn. `focusId` is the query line when Run is on that line, so ActorStop can pair on that id. The Search Actor keeps its own root-only start.
2. [x] One eval — The Query Actor in `src/Server/SearchActor.fs` evaluates once, off the mailbox, on the full server Graph from State. `graphIds` do not replace that Graph. A `=` line does not select a `?` actor name. A start error is ActorStart plus ActorStop `ActorFailed` of that message. The match is `StartError`, not a string.
3. [x] ActorStop — The Actor posts ActorStop and stops. The id is the query-line Node id. The result is `ActorQuery`. That result carries the Node ids. `ActorSucceeded` stays the result for other Actors.

### 6. Cap

1. [x] Provisional soft-stop — A test on this ticket may stop the eval early. That stop is provisional. [14 — Cap of 200](14-cap-of-200.md) section 2 **Query cap** still owns the Query cap checkboxes. Those checkboxes stay open.

## Out of scope

1. **Ref post** — `ChildNode.reference` under the query line is [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md). That post stays a proposed design.
2. **Want ride** — The Poll that carries Want `nodes` with the Ref Change is [17 — Server items inserted](17-server-items-inserted.md).
3. **Trash** — An ordinary query that skips trash is [12 — Query skips trash](12-query-skips-trash.md). The function `trash` is [13 — Trash function](13-trash-function.md).
4. **Query cap checkboxes** — [14 — Cap of 200](14-cap-of-200.md) section 2 **Query cap** owns those checkboxes.
5. **Find walk** — The Find and Move walk, the globe, and `POST /ambit/search` stay on the Search Actor. This ticket does not add `QueryActorDoor`, `POST /ambit/query`, or `RecordQueryStart`.
6. **Ref insert** — The Ref insert under the query line stays [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md). This ticket's Run change is the remote eval.

## See also

[Online search spec](plan/online-search/spec.md) §2 Query spec, [Online search map](plan/online-search/map.md) Decisions so far item 6 **Remote query eval**

## Comments

- 2026-10-07: Alan. The door is the existing Run command. This ticket changes that door from local run to remote run. The reply is Node ids. The Ref post stays [16 — Insert Refs under the query line](16-insert-refs-under-the-query-line.md). Server eval stays the lock in Decisions so far item 6 **Remote query eval**. A new search-style door is out.
- 2026-10-07: Alan. The Node ids ride ActorStop as `ActorQuery`. They do not ride a command response body.
- 2026-10-09: Alan. Rebase onto [22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md). A `=` line queues ActorStart behind the edit. Classify from post-edit State in the list turn.

## Time

- 2026-10-07 2h — remote query eval on Run (from chat)
- 2026-10-07 1h — Node ids ride ActorStop (from chat)
- 2026-10-08 1h — Query chip keeps the Node ids (from chat)
- 2026-10-09 1h — Rebase onto the ordered event list (from chat)
