# Code review — 07 Server completes the picture — ActorStart

Review of the working tree for [07 — Server completes the picture](../issues/07-server-completes-the-picture.md).

## 1. Standards

No findings.

## 2. Spec

1. **Command chip** — `ActorStart` and `ActorStop` are the ordinary lifecycle events. A poll applies them through `ActorLive.applyEvent` and `withActorCmdResult`, so the command chip can show "Actor succeeded." The ticket asked for the event-source record.
2. **commandId** — `SearchActor.actorStart` sets `commandId` to `graph.root`. The spec names root, focus, and `graphIds` equal to the root. `ActorStart` still requires `commandId`.
3. **Focus fallback** — `Graph.focus` is ephemeral and omitted from history, so server State usually has no focus. The record then stores the root as `focusId`. That is not the dialog focus. The search request does not carry a focus id.
4. **Stop failure** — `recordStop` runs before the HTTP body is returned. A failed stop returns an error, drops the reply, and leaves `ActorStart` on the event source without `ActorStop`.
5. **Door types** — The spec did not name `Api.SearchActorDoor` (`changes`, `recordStart`, `recordStop`) or the mailbox cases `RecordSearchStart` and `RecordSearchStop`.

Standards: 0 findings. Spec: 5 findings. Worst on Standards: none. Worst on Spec: server State has no dialog focus, so `ActorStart.focusId` falls back to root.
