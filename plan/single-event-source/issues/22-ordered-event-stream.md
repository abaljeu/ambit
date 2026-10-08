# 22 — One ordered event stream

**Status:** `coded`
**Type:** coding
**Order:** This ticket lands first. [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) (draft pull request [215 — Server evaluates](https://github.com/abaljeu/ambit/pull/215)) rebases onto this ticket and is corrected there. Do not land that pull request first.

## Context

A person edits a line and then Runs that line. The edit and the Run are connected. The Run must see the edit.

Today the Browser queues the edit and posts the Run on a separate route. The Run can reach the server first. The server then classifies the line from State that does not contain the edit. A `=` from that edit can be missing.

Alan, 2026-10-08. One Event source means one ordered stream. Connected Events are queued in order, posted in that order, and processed in that order. A later Event does not pass an earlier connected Event. A posted list is applied in order, as one unit; lists from different clients do not interleave. The only rejection is the credential check. That check refuses the whole list before anything applies. Otherwise every posted list applies whole. Removing `POST /ambit/command`, and making Cancel an Event in that same stream, is the mechanism. The order is the goal.

This ticket changes every Run that [execRunOp](../../../src/Client/Commands.fs) posts. That includes a query line and a named actor. Find (`POST /ambit/search`) and Load/Save (`POST /ambit/load-save-command`) keep their own doors for now.

## Settled

1. **Posted list** — Alan, 2026-10-08. If you post 3 events and they post 3, it is ABCDEF or DEFABC, nothing else. A posted list is applied in order, as one unit. Lists from different clients do not interleave.
2. **Credential only** — Alan, 2026-10-08. "we aren't writing rejectable events, except for credential." The credential check refuses the whole list before anything applies. Otherwise every posted list applies whole. A start bookkeeping error is not a rejection. `UnknownActor` stores ActorStart and ActorStop `unknown actor`. `Rejected` stores ActorStart and ActorStop with that message.

## Current state

These facts are from this tree, plus the code-read of the [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) branch for the `=` path.

1. **Edit enqueue** — `execRunOp` calls `afterEditCommit`, which calls [commitIfEditing](../../../src/Client/UpdateHelpers.fs). When the text changed, `commitTextEdit` calls [applyAndPost](../../../src/Client/UpdateHelpers.fs). That calls [SyncPlanner.enqueuePending](../../../src/Shared/SyncPlanner.fs) and appends the Change to `syncInfo.pending`.
2. **Batch gate** — `enqueuePending` emits `SubmitPendingBatch` only when no post is in flight. [tryStartSubmit](../../../src/Shared/SyncPlanner.fs) does not emit it when `syncState` is `Sending`, `Polling`, `Uploading`, `Parsing`, or `Loading`. The Change then stays on `syncInfo.pending`.
3. **Edit post** — [runSubmitPendingBatch](../../../src/Client/App.fs) calls [postJson](../../../src/Client/JsInterop.fs) to `/{file}/changes`. On the Ambit page that path is `POST /ambit/changes`. `postJson` is `fetch`. It returns before the response.
4. **Run post** — When [CommandRequest.tryStart](../../../src/Shared/CommandRequest.fs) returns Ok, `execRunOp` returns `commitEffects @ [ SubmitCommand request ]`. [runEffects](../../../src/Client/App.fs) runs that list in order. [runSubmitCommand](../../../src/Client/App.fs) then posts `/{file}/command` (`POST /ambit/command`) at once. It does not read or write `syncInfo.pending`. It does not wait for the changes batch.
5. **Run is not parked** — [SyncPlanner.tryReleaseQueued](../../../src/Shared/SyncPlanner.fs) parks Load and workspace push until `pending` is empty and `syncState` is `Idle`. It does not park Run.
6. **List is not one unit** — [CoreMailbox.postEvents](../../../src/Server/Core/CoreMailbox.fs) calls `mergePostLoop`. Each event is its own `PostEvent`. The loop waits for that `PostEvent` before it posts the next. Another client's message can land between those posts. Two lists of three can apply as ABDCEF. That is the gap Expand closes.
7. **Per-item reject** — [CoreEventDispatch.completeAction](../../../src/Server/Core/CoreEventDispatch.fs) returns `Actor lifecycle Events are mailbox-generated` for `EventBody.ActorStart` and `EventBody.ActorStop`. That path is a gap. A client ActorStart and a client Cancel must be valid event types. They are not rejectable. Today an ActorStart in the list is treated as rejectable. Earlier items are already committed. Later items are not posted. A client ActorStop is not a client event type. The durable ActorStop stays mailbox-generated.
8. **Server classify** — `POST /ambit/command` calls [CoreMailbox.startActor](../../../src/Server/Core/CoreMailbox.fs). [CoreActorPool.runStartActor](../../../src/Server/Core/CoreActorPool.fs) reads the command Node from `getState()` and takes the actor name from that text (`actorNameFrom`). That State is stale when the edit is still in flight.
9. **Amble on this tree** — When [CommandRequest.isAmbleScanStop](../../../src/Shared/CommandRequest.fs) is true, `execRunOp` calls `execAmbleRunOp` and does not return `SubmitCommand`. A `=` line on this tree does not post `/ambit/command`.
10. **Equals on [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md)** — That draft pull request sends a `=` line through `POST /ambit/command`. `CoreMailboxBackend.startChosen` classifies from server State. A `=` from the edit can still be absent. This ticket is the order that makes that read see the edit.
11. **Cancel** — [execCancelOp](../../../src/Client/Commands.fs) calls [cancelFocusOp](../../../src/Client/UpdateActorLive.fs). That returns `SubmitCancel`. [runSubmitCancel](../../../src/Client/App.fs) posts `/{file}/cancel` (`POST /ambit/cancel`) at once. It does not touch `syncInfo.pending`. It does not wait for `runSubmitPendingBatch`. The route calls [CoreMailbox.cancelByFocus](../../../src/Server/Core/CoreMailbox.fs).

The Browser event door is `/{file}/changes`. On the Ambit page that path is `POST /ambit/changes`. `POST /ambit/events` is the alias. Both routes call [Api.postEvents](../../../src/Server/Api.fs). The Browser does not post the queue to `/events`.

## What to build

One ordered stream from the Browser to the mailbox. The change follows [API expansion](../../../doc/current/api.md#api-expansion): expand, then migrate, then contract. Connected Events are queued in order, posted in that order, and processed in that order. A later Event does not pass an earlier connected Event. A posted list is applied in order, as one unit; lists from different clients do not interleave. The only rejection is the credential check. That check refuses the whole list before anything applies. Otherwise every posted list applies whole.

The events door is `POST /ambit/changes`. `POST /ambit/events` is the alias. Both call [Api.postEvents](../../../src/Server/Api.fs). The Browser posts `/{file}/changes`.

### 1. Expand

A `PostEvent` whose body is ActorStart takes the same path as `CoreMailbox.startActor` after the earlier events in that list commit. The old routes stay. This is not a new POST route. A new route would not fit [07 — Lock the Run Agent architecture](../../llm-connector/issues/07-lock-run-agent-architecture.md): one mailbox orders Change and launch, and HTTP is an adapter. There is no fifth Core API.

1. [x] ActorStart path — After the earlier events commit, that `PostEvent` runs `startActor` bookkeeping: admission, registry, and revision. The client body is a launch request, not the durable event. The stored ActorStart stays mailbox-generated. The Actor body, including query eval, stays off the mailbox. [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md) holds.
2. [x] Valid types — Client ActorStart and Cancel are valid event types. The mailbox does not reject them. A client ActorStop is not a client event type. The durable ActorStop stays mailbox-generated. Cancel runs `cancelByFocus` inside the list. That step stays fast.
3. [x] Credential only — The credential check is the only refusal. A missing cookie, or a caller the mailbox does not admit, returns 401 and applies nothing from the list. After that check, the list applies whole. A start bookkeeping error stores ActorStart and a failed ActorStop (`unknown actor`, or the `Rejected` message). The HTTP result is success.
4. [x] One mailbox unit — A posted list is one mailbox unit. `postEvents` pushes each event onto the queue as a `PostEvent`, back to back, under a lock, then awaits the replies. Each step stays fast per [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md). The ActorStart Actor body stays off the mailbox. Another client's list cannot enter inside this push.
5. [x] Expand test — A test posts one list on `POST /ambit/events`: an edit, then an ActorStart for that same line. The server classifies the line from State after that edit. The read does not use the State from before the edit. The Actor body does not run inside the mailbox. A failed credential check applies nothing. After admission, the edit and the ActorStart both apply.
6. [x] Two-list test — Two clients each post 3 events at the same time. The applied order is ABCDEF or DEFABC. The order is never interleaved.

### 2. Migrate

`execRunOp` and Cancel go through `syncInfo.pending`. That is every Run `execRunOp` posts: a query line and a named actor. The old routes still exist. Find and Load/Save do not move in this phase.

1. [x] Run joins the queue — `execRunOp` appends the ActorStart behind the edit Events on `syncInfo.pending`. It does not return `SubmitCommand`.
2. [x] Cancel joins the queue — Cancel is queued on `syncInfo.pending` behind earlier Events. It is not a client ActorStop. It does not post `/{file}/cancel`.
3. [x] One post — `runSubmitPendingBatch` posts that list, in order, to `/{file}/changes`.
4. [x] Reply — The events-door answer, or a later Poll, carries the Events the Browser reads today from the command response. That includes ActorStop. On [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) the Node ids ride that ActorStop as `ActorQuery`. They stay on that Event.
5. [x] Migrate test — A test runs an edit and then Run on the same line. Both Events are on `syncInfo.pending` in that order. One post goes to `/{file}/changes`. `execRunOp` does not call `postJson` for `/{file}/command`. The server classifies that line from the post-edit State, every time. A Cancel queued behind an edit does not call `postJson` for `/{file}/cancel`.

### 3. Contract

The old routes go away only after no caller uses them.

1. [x] Delete routes — `POST /ambit/command` and `POST /ambit/cancel` are removed.
2. [x] Delete posters — `runSubmitCommand` and `runSubmitCancel` are removed.
3. [x] Contract test — A test shows `POST /ambit/command` and `POST /ambit/cancel` are absent, and `runSubmitCommand` and `runSubmitCancel` are absent. The events door still accepts an edit followed by ActorStart, and an edit followed by Cancel.

## Scope questions

These doors stay for now. The expansion rule says a new capability expands the events door, not a new POST route. The question is whether a later change moves them onto that door.

1. **Search** — Find keeps `POST /ambit/search` for now. It calls `CoreMailbox.recordSearchStart`, then the walk, then `recordSearchStop`. Does Find stay on that door, or later expand onto the events list?
2. **Load/Save** — Load/Save keeps `POST /ambit/load-save-command` for now. It calls `CoreMailbox.startLoadSaveCommand`. Does Load/Save stay on that door, or later expand onto the events list?
3. **Cancel** — Browser Cancel joins this stream. A client ActorStop is not a client event type. Confirm that no other caller keeps `POST /ambit/cancel`. On this tree the Browser path is `SubmitCancel` only (`execCancelOp` and the row Cancel control).

## Conflicts

1. **Run Agent architecture** — Expand fits [07 — Lock the Run Agent architecture](../../llm-connector/issues/07-lock-run-agent-architecture.md). One mailbox orders Change and launch. HTTP is an adapter. There is no fifth Core API. A new route would not fit. The client ActorStart is a launch request. The stored ActorStart stays mailbox-generated. Launch still registers the Actor and schedules the body before Actor output can be admitted. 07 also says Command is its own request beside Change and Poll, with `{ nodes; events; latestId }`. Contract removes that HTTP door. Expand and migrate leave it in place.
2. **Clear fast** — [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md) says every mailbox message finishes quickly, and Cancel is a fast message. This ticket keeps that. The Actor body stays off the queue. Cancel in the stream is still a fast mailbox step. Mailbox FIFO agrees with the stream order. 0004 does not name the Browser queue. This ticket adds that queue. It does not change the Decision file.
3. **Server evaluates** — Alan, 2026-10-08: do not land the [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) pull request. That ticket said the route stays `POST /ambit/command`. This ticket lands first and removes that route. [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) rebases onto this ticket and is corrected there. The Node ids stay on ActorStop as `ActorQuery`. The events-door answer or Poll carries that Event. Ticket 11 says those ids do not ride a separate field of the command body. That part stays.

## Out of scope

1. **Search** — Scope question 1. `POST /ambit/search` stays for now.
2. **Load/Save** — Scope question 2. `POST /ambit/load-save-command` stays for now.
3. **Deliver** — `POST /ambit/actors/deliver` is inbound text for a live Actor. It is not an Actor start.
4. **Query contract** — This ticket is not [10 — Pointer: Core Query contract](../../core-refinement/issues/10-pointer-core-query-contract.md). That pointer is the typed Query contract. It is not Run launch and not the events list.
5. **Pool rebuild** — [08 — Pointer: Core Actor pool](../../core-refinement/issues/08-pointer-core-actor-pool.md), [12 — Pointer: Launch Actor (Focus registration)](../../core-refinement/issues/12-pointer-launch-actor-and-hold-span.md), and [13 — Pointer: Finish and drop](../../core-refinement/issues/13-pointer-finish-and-drop.md) keep admission, registry, launch, and drop. This ticket calls that bookkeeping. It does not rebuild it.
6. **Core API name** — The Core API call Command stays the in-mailbox launch. There is no fifth Core API. Contract removes the HTTP post that passes the queue.

## Unnamed types

1. `MailboxHost.gate` — the lock around one list's serial push onto the queue.
2. `StartError` — `UnknownActor of name` and `Rejected of message`. The events-list handler matches each case and stores ActorStart plus a failed ActorStop.
3. `EventBody.Cancel` — the client Cancel on the events list. No existing body is a cancel. The durable stop stays ActorStop.

## See also

[API expansion](../../../doc/current/api.md#api-expansion), [Single event source architecture](../arch.md), [02 — Files, Query, and Command as Event work](02-files-query-and-command-as-event-work.md), [07 — Lock the Run Agent architecture](../../llm-connector/issues/07-lock-run-agent-architecture.md), [Core mailbox messages clear fast](../../../doc/Decisions/0004-core-mailbox-messages-clear-fast.md)

## Comments

- 2026-10-07 — Alan. Remove `POST /ambit/command`. Run goes through the events door as an ActorStart in the same ordered list as the edits.
- 2026-10-08 — Alan. The goal is Event Source order. Cancel joins that same stream. The route removal is the mechanism.
- 2026-10-08 — Alan. The change follows the API expansion protocol. Expand accepts ActorStart and Cancel on the events list. Migrate moves `execRunOp` and Cancel onto `syncInfo.pending`. Contract deletes the old routes only after no caller uses them.
- 2026-10-08 — Code-read from the [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md) branch. Today `completeAction` treats a client ActorStart as rejectable. Earlier items stay committed and later items are not posted. That path is a gap. A client ActorStop is not a client event type.
- 2026-10-08 — Alan. If you post 3 events and they post 3, it is ABCDEF or DEFABC, nothing else.
- 2026-10-08 — Alan. "we aren't writing rejectable events, except for credential."
- 2026-10-08 — Alan. Don't land the pull request for [11 — Server evaluates](../../online-search/issues/11-server-evaluates.md). Make this ticket happen, then rebase and correct that ticket on this ticket.
- 2026-10-08 — Alan. `CoreMsg.PostEvents` was not the plan. `postEvents` pushes the list serially onto the queue. An unregistered actor is not a rejection. The list applies whole. The mailbox records the ActorStart and a failed ActorStop (`unknown actor`).
- 2026-10-08 — Alan. Use typed matching for an unregistered actor. Do not re-implement start and cancel inside `CoreEventDispatch`.
- 2026-10-08 — Alan. `StartError.Rejected` stores ActorStart and a failed ActorStop with that message. The list applies whole. On the events list, no start bookkeeping error returns an HTTP error. Only the credential check refuses a list.

## Time

Actual: 4h

- 2026-10-08: 4h (from chat) implement expand, migrate, and contract for the ordered event stream.
