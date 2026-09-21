# 53 — Cancel HTTP conveys ActorStop

**Status:** defined
**Type:** bug-fixing
**Blocked by:** None — [22 — Client cancels a job](22-client-cancels-a-job.md) and [21 — Client shows live Actor](21-client-shows-lock-present.md) are `done`.
Estimate: 2h

## Context

Alan: Cancel control / Cancel key stops the Cloud Agent job, but `amb-actor-live` chrome remains.

### Start path (confirmed)

POST `/command` returns Events including `ActorStart`. Client `CommandDone` → `applyCommandEvents` updates `actorLiveFocusIds` immediately.

### Cancel path (confirmed)

`Api.postCancel` acknowledges without Events (`{"ok":true}`). Client `runSubmitCancel` success is `(fun _ -> ())`. [22 — Client cancels a job](22-client-cancels-a-job.md) expected chrome to clear only via Poll applying `ActorStop`. If Poll is late, filtered, or the tab is not applying tails, chrome sticks while the Agent is already dead. Server `dispatchCancelActor` does commit `ActorStop` + `pool.finish` (CTS cancel). Server [CancelByFocusTests](../../../tests/Server.Tests/CancelByFocusTests.fs) wait for `ActorStop` — the Event exists.

### Finish then body stop

After cancel `finish` drops the live row, the Actor body still calls `actorStop`. `getFocusId` may be None and default to `Graph.rootId`. Prefer finish/stop ordering that does not emit a wrong Focus stop.

## What to build

### 1. Cancel HTTP Events

1. [ ] Cancel HTTP success carries Events the same way Command carries `ActorStart`: at least the Cancelled `ActorStop` for that Focus, encoded as universal `{ nodes; events; latestId }`.
2. [ ] Client applies those Events (`applyCommandEvents` / equivalent) so `actorLiveFocusIds` drops and chrome + Cancelled (or Error) result update without waiting on Poll.
3. [ ] Proof: Cancel while live → chrome off and Cancelled (or Error) result from the Cancel response Events. Server still cancels CTS / refuses later Actor output.

### 2. No wrong Focus stop

1. [ ] After cancel `finish` drops the live row, a later Actor-body `actorStop` does not emit `ActorStop` for `Graph.rootId` or any other Focus that was not live.
2. [ ] Proof: cancel then body stop → one Cancelled `ActorStop` for the Focus; no root stop.

### 3. Non-goals

1. Undo semantics.
2. XML AI pack / Focus cssClass.
3. Changing Poll protocol generally beyond this conveyance.

## See also

[21 — Client shows live Actor](21-client-shows-lock-present.md), [22 — Client cancels a job](22-client-cancels-a-job.md), [17 — Cancel a job](17-cancel-a-job.md), llm-connector [10 — Cancel by Focus](../../llm-connector/issues/10-cancel-by-focus.md), [Core creation architecture](../arch.md) HTTP Adapter

## Comments

- 2026-09-21 — Filed from Alan repro: Cancel stops the Agent; `amb-actor-live` stays on. Status `defined`.

## Time

- 2026-09-21 — Ticket (from chat)
