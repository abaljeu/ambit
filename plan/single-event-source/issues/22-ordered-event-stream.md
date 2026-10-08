# 22 — One ordered event stream

**Status:** `defined`
**Type:** coding
**Blocked by:** [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) — draft PR #215. The `ActorQuery` Node ids land on that ticket. Start this ticket when that ticket is `done`.

## Context

A person edits a line and then Runs that line. The edit and the Run are connected. The Run must see the edit.

Today the Browser queues the edit and posts the Run on a separate route. The Run can reach the server first. The server then classifies the line from State that does not contain the edit. A `=` from that edit can be missing.

Alan, 2026-10-08. One Event source means one ordered stream. Connected Events are queued in order, posted in that order, and processed in that order. A later Event does not pass an earlier connected Event. Removing `POST /ambit/command`, and making Cancel an Event in that same stream, is the mechanism. The order is the goal.

## Bug

This is fact from the code on this tree.

1. **Queue** — [applyAndPost](../../../src/Client/UpdateHelpers.fs) calls [SyncPlanner.enqueuePending](../../../src/Shared/SyncPlanner.fs). That appends the Change to `syncInfo.pending`. When the queue can send, the effect is `SubmitPendingBatch`. When `syncInfo.syncState` is already `Sending`, `tryStartSubmit` does not emit `SubmitPendingBatch`. The Change stays on `syncInfo.pending`.
2. **Run** — [execRunOp](../../../src/Client/Commands.fs) calls `afterEditCommit`. When [CommandRequest.tryStart](../../../src/Shared/CommandRequest.fs) returns Ok, `execRunOp` returns `commitEffects @ [ SubmitCommand request ]`.
3. **Effect order** — [runEffects](../../../src/Client/App.fs) runs that list in order.
4. **Edit post** — [runSubmitPendingBatch](../../../src/Client/App.fs) calls [postJson](../../../src/Client/JsInterop.fs) to `/{file}/changes`. `postJson` is `fetch`. The function returns before the response. It does not wait.
5. **Command post** — [runSubmitCommand](../../../src/Client/App.fs) then calls `postJson` to `/{file}/command`. It does not read or write `syncInfo.pending`. It does not wait for the in-flight `/{file}/changes` batch from `runSubmitPendingBatch`. The command fetch can arrive first.
6. **Server classify** — `POST /ambit/command` calls [CoreMailbox.startActor](../../../src/Server/Core/CoreMailbox.fs). [CoreActorPool.runStartActor](../../../src/Server/Core/CoreActorPool.fs) reads the command Node from `getState()` and takes the actor name from that text (`actorNameFrom`). That State is stale when the edit is still in flight.
7. **Amble on this tree** — When [CommandRequest.isAmbleScanStop](../../../src/Shared/CommandRequest.fs) is true, `execRunOp` calls `execAmbleRunOp` and does not return `SubmitCommand`. A `=` line on this tree does not post `/ambit/command`.
8. **Equals on PR #215** — [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) (draft PR #215) sends a `=` line through `POST /ambit/command`. `CoreMailboxBackend.startChosen` classifies from server State. The review on that PR records that a `=` from the edit can still be absent. This ticket is the order that makes that read see the edit.
9. **Events door rejects lifecycle** — [CoreEventDispatch.completeAction](../../../src/Server/Core/CoreEventDispatch.fs) returns the error `Actor lifecycle Events are mailbox-generated` for `EventBody.ActorStart` and `EventBody.ActorStop`. [CoreMailbox.postEvents](../../../src/Server/Core/CoreMailbox.fs) does not launch an Actor.
10. **Cancel** — [execCancelOp](../../../src/Client/Commands.fs) calls [cancelFocusOp](../../../src/Client/UpdateActorLive.fs). That returns `SubmitCancel`. [runSubmitCancel](../../../src/Client/App.fs) calls `postJson` to `/{file}/cancel`. It does not touch `syncInfo.pending`. It does not wait for `runSubmitPendingBatch`. `POST /ambit/cancel` calls [CoreMailbox.cancelByFocus](../../../src/Server/Core/CoreMailbox.fs).

The Browser event door is `/{file}/changes`. On the Ambit page that path is `POST /ambit/changes`. `POST /ambit/events` is the alias. Both routes call [Api.postEvents](../../../src/Server/Api.fs). The Browser does not post the queue to `/events`.

## What to build

One ordered stream from the Browser to the mailbox. The change follows [API expansion](../../../doc/current/api.md#api-expansion): expand, then migrate, then contract. Connected Events are queued in order, posted in that order, and processed in that order. A later Event does not pass an earlier connected Event.

The events door is `POST /ambit/changes`. `POST /ambit/events` is the alias. Both call [Api.postEvents](../../../src/Server/Api.fs). The Browser posts `/{file}/changes`.

### 1. Expand

ActorStart and Cancel are accepted in the events list. The old routes stay.

1. [ ] ActorStart in the list — `POST /ambit/events` accepts an ActorStart after the edits in that same list. The mailbox applies the edit first. It then runs `startActor` bookkeeping: admission, registry, and revision. It records ActorStart. It schedules the body. It does not run the body on the mailbox queue. `POST /ambit/command` still exists.
2. [ ] Cancel in the list — That same list accepts a Cancel. The mailbox runs `cancelByFocus` at that position. Earlier Events in the list already ran. The Cancel message stays fast. `POST /ambit/cancel` still exists.
3. [ ] Expand test — A test posts one list on `POST /ambit/events`: an edit, then an ActorStart for that same line. The server classifies the line from State after that edit. The read does not use the State from before the edit. A second test posts an edit, then a Cancel, and the Cancel runs after the edit. Both old routes still answer.

### 2. Migrate

`execRunOp` and Cancel go through `syncInfo.pending`. The old routes still exist.

1. [ ] Run joins the queue — `execRunOp` appends the ActorStart behind the edit Events on `syncInfo.pending`. It does not return `SubmitCommand`.
2. [ ] Cancel joins the queue — Cancel is an Event on `syncInfo.pending`. It does not post `/{file}/cancel`.
3. [ ] One post — `runSubmitPendingBatch` posts that list, in order, to `/{file}/changes`.
4. [ ] Reply — The events-door answer, or a later Poll, carries the Events the Browser reads today from the command response. That includes ActorStop. On [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) the Node ids ride that ActorStop as `ActorQuery`. They stay on that Event.
5. [ ] Migrate test — A test runs an edit and then Run on the same line. Both Events are on `syncInfo.pending` in that order. One post goes to `/{file}/changes`. `execRunOp` does not call `postJson` for `/{file}/command`. The server classifies that line from the post-edit State, every time. A Cancel queued behind an edit does not call `postJson` for `/{file}/cancel`.

### 3. Contract

The old routes go away only after no caller uses them.

1. [ ] Delete routes — `POST /ambit/command` and `POST /ambit/cancel` are removed.
2. [ ] Delete posters — `runSubmitCommand` and `runSubmitCancel` are removed.
3. [ ] Contract test — A test shows `POST /ambit/command` and `POST /ambit/cancel` are absent, and `runSubmitCommand` and `runSubmitCancel` are absent. The events door still accepts an edit followed by ActorStart, and an edit followed by Cancel.

## Scope questions

1. **Load-save command** — Does `POST /ambit/load-save-command` join this stream, or stay its own door? It calls `CoreMailbox.startLoadSaveCommand`. This ticket does not fold it in.
2. **Search** — Does `POST /ambit/search` join this stream, or stay its own door? It calls `CoreMailbox.recordSearchStart`, then the walk, then `recordSearchStop`. This ticket does not fold it in.
3. **Cancel** — This framing puts Browser Cancel in the stream and removes `POST /ambit/cancel`. Confirm that no other caller keeps that route. On this tree the Browser path is `SubmitCancel` only (`execCancelOp` and the row Cancel control).

## Conflicts

1. **Run Agent architecture** — [07 — Lock the Run Agent architecture](../../llm-connector/issues/07-lock-run-agent-architecture.md) says the Browser sends a typed launch request, and Command is its own request beside Change and Poll. The response type is `{ nodes; events; latestId }`. This ticket removes that HTTP Command request. ActorStart is an Event in the same list as the edits. The same record says one mailbox orders launch, Change, and cancel. That part agrees. Launch still registers the Actor, appends ActorStart, and schedules the body before Actor output can be admitted.
2. **Clear fast** — [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md) says every mailbox message finishes quickly, and Cancel is a fast message. This ticket keeps that. The Actor body stays off the queue. Cancel in the stream is still a fast mailbox step. Mailbox FIFO agrees with the stream order. 0004 does not name the Browser queue. This ticket adds that queue. It does not change the Decision file.
3. **Server evaluates** — [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) says the route stays `POST /ambit/command`. This ticket removes that route. The Node ids stay on ActorStop as `ActorQuery`. The events-door answer or Poll carries that Event. Ticket 11 says those ids do not ride a separate field of the command body. That part stays.

## Out of scope

1. **Load-save command** — Scope question 1. Not this ticket until Alan answers.
2. **Search** — Scope question 2. Not this ticket until Alan answers.
3. **Deliver** — `POST /ambit/actors/deliver` is inbound text for a live Actor. It is not an Actor start.
4. **Pool rebuild** — [08 — Pointer: Core Actor pool](../../core-refinement/issues/08-pointer-core-actor-pool.md), [12 — Pointer: Launch Actor (Focus registration)](../../core-refinement/issues/12-pointer-launch-actor-and-hold-span.md), and [13 — Pointer: Finish and drop](../../core-refinement/issues/13-pointer-finish-and-drop.md) keep admission, registry, launch, and drop. This ticket calls that bookkeeping. It does not rebuild it.
5. **Core API name** — The Core API call Command stays the in-mailbox launch. This ticket removes the HTTP post that passes the queue.

## See also

[API expansion](../../../doc/current/api.md#api-expansion), [Single event source architecture](../arch.md), [02 — Files, Query, and Command as Event work](02-files-query-and-command-as-event-work.md), [07 — Lock the Run Agent architecture](../../llm-connector/issues/07-lock-run-agent-architecture.md), [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md)

## Comments

- 2026-10-07 — Alan. Remove `POST /ambit/command`. Run goes through the events door as an ActorStart in the same ordered list as the edits.
- 2026-10-08 — Alan. The goal is Event Source order. Cancel joins that same stream. The route removal is the mechanism.
- 2026-10-08 — Alan. The change follows the API expansion protocol. Expand accepts ActorStart and Cancel on the events list. Migrate moves `execRunOp` and Cancel onto `syncInfo.pending`. Contract deletes the old routes only after no caller uses them.
