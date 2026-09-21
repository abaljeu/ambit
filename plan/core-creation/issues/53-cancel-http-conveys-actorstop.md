# 53 — Cancel HTTP conveys ActorStop

**Status:** done
**Type:** bug-fixing
**Blocked by:** None — [22 — Client cancels a job](22-client-cancels-a-job.md) and [21 — Client shows live Actor](21-client-shows-lock-present.md) are `done`.
Estimate: 2h
Actual: 2h15m

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

1. [x] Cancel HTTP success carries Events the same way Command carries `ActorStart`: at least the Cancelled `ActorStop` for that Focus, encoded as universal `{ nodes; events; latestId }`.
2. [x] Client applies those Events (`applyCommandEvents` / equivalent) so `actorLiveFocusIds` drops and chrome + Cancelled (or Error) result update without waiting on Poll.
3. [x] Proof: Cancel while live → chrome off and Cancelled (or Error) result from the Cancel response Events. Server still cancels CTS / refuses later Actor output.

### 2. No wrong Focus stop

1. [x] After cancel `finish` drops the live row, a later Actor-body `actorStop` does not emit `ActorStop` for `Graph.rootId` or any other Focus that was not live.
2. [x] Proof: cancel then body stop → one Cancelled `ActorStop` for the Focus; no root stop.

### 3. Non-goals

1. Undo semantics.
2. XML AI pack / Focus cssClass.
3. Changing Poll protocol generally beyond this conveyance.

## See also

[21 — Client shows live Actor](21-client-shows-lock-present.md), [22 — Client cancels a job](22-client-cancels-a-job.md), [17 — Cancel a job](17-cancel-a-job.md), llm-connector [10 — Cancel by Focus](../../llm-connector/issues/10-cancel-by-focus.md), [Core creation architecture](../arch.md) HTTP Adapter

## Comments

- 2026-09-21 — Alan accepted. Squash-landed on staging. Status `done`.
- 2026-09-21 — Filed from Alan repro: Cancel stops the Agent; `amb-actor-live` stays on. Status `defined`.
- 2026-09-21 — Independent review: Standards Needs changes; Spec Approve with nits. Status stays `coded`. Report: [independent-review-53-cancel-http-conveys-actorstop](../reports/independent-review-53-cancel-http-conveys-actorstop.md).
- 2026-09-21 — Implemented Cancel universal Events; Client `CommandDone` apply; no root Focus stop after finish. Actor admit-on-live already blocked the second stop; `getFocusId` no longer defaults to `Graph.rootId`. Status `coded`.
- 2026-09-21 — Review must-fix: restore Development `AiKeys[0].ApiKey` to empty (match staging); drop unrelated code-review skill wording and directional-links report. App.fs / RouteRegistration FILE growth left as nit. Status stays `coded`.

## Time

- 2026-09-21 — Ticket (from chat)
- 2026-09-21 2h — Cancel HTTP Events, Client apply, focused proofs (from chat)
- 2026-09-21 15m — Review must-fix: empty Development ApiKey, drop unrelated PR files (from chat)
