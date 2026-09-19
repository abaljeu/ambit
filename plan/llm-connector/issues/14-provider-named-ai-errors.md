# 14 — Provider-named AI errors

**Status:** defined
**Blocked by:** None — [09 — Agent failure preserves children](09-agent-failure-preserves-children.md), [13 — Vertical proof: Browser Ask from what I see](13-vertical-proof-browser-ask.md), and core-creation [21 — Client shows live Actor](../../core-creation/issues/21-client-shows-lock-present.md) / [22 — Client cancels a job](../../core-creation/issues/22-client-cancels-a-job.md) are `done`.
**Type:** task
Estimate: 2h
Actual: 0m

## Context

When `?ai` cannot talk to the provider (missing key, unauthorized key, or an equivalent start/auth failure), the Client must show an Error that names the **provider** (for example Cursor), never Ask. Core-creation [21 — Client shows live Actor](../../core-creation/issues/21-client-shows-lock-present.md) and [22 — Client cancels a job](../../core-creation/issues/22-client-cancels-a-job.md) already convey Actor stop results. This ticket makes CloudAgents and the Run Agent Actor emit a safe named message, and makes Client `lastCmdResult` show it.

Locked wording example: `Could not send message to Cursor: unauthorized` — provider name from the connector; reason short and safe (`unauthorized`, `missing key`).

## What to build

### 1. CloudAgents named auth failure

1. [ ] Auth map — CloudAgents maps missing-key and unauthorized start failures to a safe `Failed` / `AuthenticationFailed` message that names the provider (Cursor for the current adapter).
2. [ ] Fake auth — `setFake` can simulate Unauthorized / auth failure so proof does not need a live key.

### 2. Actor stop carries the safe message

1. [ ] ActorFailed payload — `ActorResult.ActorFailed` carries a safe client string (`ActorFailed of string`; empty means generic). Event JSON encode/decode and Core paths pass it through.
2. [ ] Run Agent Actor — start/auth failure becomes that named string on Actor stop, not a dropped unit Failed.

### 3. Client conveyance

1. [ ] ActorFailed result — `ActorLive.lastCmdResult` on `ActorFailed` shows that message as Error; command label is **AI**, not Ask.
2. [ ] Ask scrub — remaining Ask labels on ActorSucceeded / ActorCancelled in `ActorLive.lastCmdResult` become AI.

### 4. Non-goals

1. [ ] AiKeys registry / `?ai keyname` parsing.
2. [ ] Graph Error outline text.
3. [ ] Raw provider dumps in Graph (09 lock stays).

## See also

[llm-connector architecture](../arch.md), [09 — Agent failure preserves children](09-agent-failure-preserves-children.md), [13 — Vertical proof: Browser Ask from what I see](13-vertical-proof-browser-ask.md), [21 — Client shows live Actor](../../core-creation/issues/21-client-shows-lock-present.md), [22 — Client cancels a job](../../core-creation/issues/22-client-cancels-a-job.md)

## Comments

- 2026-09-19 — Filed as a follow-up on the done first Agent vertical. Chrome tickets convey stop results; this ticket names the provider on auth/start failure.
