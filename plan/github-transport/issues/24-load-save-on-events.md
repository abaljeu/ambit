# 24 — Load and Save on the events list

**Status:** `defined`
**Type:** coding
**Blocked by:** [22 — Drop the Browser git Load after-step](22-drop-browser-git-load-after-step.md)
**Trial step:** 5. Next: [25 — Pace server functions](25-pace-server-functions.md).

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 5. Load and Save leave `POST /ambit/load-save-command`. They become ActorStart on the events list. The same rule is [22 — One ordered event stream](../../single-event-source/issues/22-ordered-event-stream.md) for `POST /ambit/command`: expand the events list, move the caller, then delete the old route.

That ticket left Load/Save on their own door (scope question 2). This ticket is that later move. The events door is `POST /ambit/events`. [23 — Endpoint switch](../../single-event-source/issues/23-endpoint-switch-events.md) removes `POST /ambit/changes`. Do not add a route.

The queued Load path is in this ticket. It runs desk Load in the Browser and never reaches the server.

## Current state

1. **Route** — [RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) line 329 maps `POST /ambit/load-save-command`.
2. **Browser post** — [LoadSaveCommandClient.fs](../../../src/Client/LoadSaveCommandClient.fs) `runWith` (line 45) posts `"/" + fileName + "/load-save-command"` (line 52).
3. **Effect** — [App.fs](../../../src/Client/App.fs) line 96 runs `SubmitLoadSaveCommand` through `LoadSaveCommandClient.run`.
4. **Queued Load** — [App.fs](../../../src/Client/App.fs) line 101: `RunQueuedRequest QueuedLoad` dispatches `deskLoadOp`. That call does not post the load-save command.

Directory and File targets already post to the parse stack ([23 — Directory or File Load posts to the parse stack](23-directory-file-load-parse-stack.md)). Git Load already posts the Workspace ([21 — Git Load posts the Workspace](21-git-load-posts-workspace.md)). This ticket changes the door, not that post.

## What to build

Load and Save are ActorStart events on the events list. The old route goes away after no caller uses it. Queued Load uses that same list.

1. [ ] Expand — An ActorStart whose command is Load or Save takes the path `POST /ambit/load-save-command` takes today, after earlier events in that list commit. The old route stays in this step.
2. [ ] Migrate, command — The Browser appends that ActorStart on the pending event list. It does not post `/load-save-command`.
3. [ ] Migrate, queue — `RunQueuedRequest QueuedLoad` appends that same ActorStart. It does not dispatch `deskLoadOp`. The server desk Load still performs the parse-stack post from [23 — Directory or File Load posts to the parse stack](23-directory-file-load-parse-stack.md).
4. [ ] Contract — Delete `POST /ambit/load-save-command` and the client poster when nothing calls them.
5. [ ] Test, door — A Load and a Save each post ActorStart on `POST /ambit/events`. Neither posts `/load-save-command`.
6. [ ] Test, queue — A queued Load posts that ActorStart and does not call `deskLoadOp`.
7. [ ] Test, contract — `POST /ambit/load-save-command` is absent.
