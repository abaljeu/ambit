# 17 — CloudAgents Console stream

**Status:** done
**Blocked by:** None — [16 — AiRepos from appsettings](16-airepos-from-appsettings.md) is `done`. Poll path remains.
**Type:** task
Estimate: 3h

## Context

Cursor Cloud Agents exposes SSE for one run: `GET /v1/agents/{id}/runs/{runId}/stream` (status, assistant text deltas, thinking, tool_call, result, error, done). Today [AgentRunner](../../../src/CloudAgents/AgentRunner.fs) and [CloudAgents.Console](../../../src/CloudAgents.Console/Program.fs) only **poll** until `FINISHED`, then print once. That feels slow and one-shot.

This ticket adds a **vendor-neutral stream** on the CloudAgents DLL and proves it in **Console**. Graph / AI Actor write-back is [18 — AI Actor stream](18-ai-actor-stream.md).

## What to build

### 1. CloudAgents DLL stream face

1. [x] Public stream API beside `start` / `poll` / `cancel` / `waitUntilComplete` (name freely; keep settings-blind).
2. [x] Cursor adapter consumes SSE; map at least: `assistant` `{ text }` deltas, terminal `result` / `done` / `error`. Other event types may be ignored or forwarded as opaque progress.
3. [x] `setFake` can simulate a short delta sequence then terminal (no live key required for proof).
4. [x] Poll path stays working; do not break existing callers.

### 2. Console proof

1. [x] Console prefers stream when available: print assistant deltas as they arrive; print terminal summary (and git if present) on `result` / `done`.
2. [x] Still reads `CURSOR_API_KEY` from env (Console does not read AiKeys).
3. [x] Document usage in [CloudAgents.Console README](../../../src/CloudAgents.Console/README.md).

### 3. Non-goals

1. AI Actor / Focus Graph writes ([18](18-ai-actor-stream.md)).
2. Incremental XML / `addChild` algorithm ([18](18-ai-actor-stream.md)).
3. Appsettings / AiKeys / AiRepos.
4. Requiring live Cursor for CI — fake stream is enough for green.

## See also

[18 — AI Actor stream](18-ai-actor-stream.md), [CloudAgents README](../../../src/CloudAgents/README.md), Cursor docs: Stream A Run `GET /v1/agents/{id}/runs/{runId}/stream`

## Comments

- 2026-09-20 — Filed from chat: split stream work into Console/DLL first, Actor second. Status `defined`.
- 2026-09-23 — Coded slice 1: `AgentStreamEvent`, `streamUntilComplete`, `setFakeStream`, SSE in `CursorHttp`/`CursorAdapter`, Console stream path. Report [stream-response-switch](../reports/stream-response-switch.md). CloudAgents.Tests 33 passed. Status `coded`.
- 2026-09-24 — Fixed gaps from [spec review](../reports/code-review-c5cff271-spec.md). The live SSE stream now honors `MaxWaitMs` and returns `Timeout`. `PollIntervalMs` is removed from `StreamArgs`. The fake emits events one at a time with a short delay, and checks cancel and the deadline between events. Stream errors, timeouts, and cancels now reach the fold as `RunFailed` / `RunCancelled` before the `Error`, on both the live and fake paths. The Console no longer reprints streamed text under `=== Result ===`. CloudAgents.Tests 41 passed.
- 2026-09-24 — Cursor cancel: `AgentRunner.cancel` now returns `CancelOutcome` (`CancelRequested` or `NotCancellable`). HTTP 409 `run_not_cancellable` maps to `Ok NotCancellable`. A missing key or 401 maps to `AuthenticationFailed`, other failures to `NetworkError`. The live stream delivers `RunCancelled` when a `status`, `result`, or `done` event carries `status: CANCELLED`. If the stream ends with some other error, one poll confirms whether the run was `CANCELLED`. The fake answers `NotCancellable` once its stream has emitted a terminal event or its polled status is terminal. CloudAgents.Tests 47 passed.
- 2026-09-24 — Alan approved staging publish of `origin/ready` (`46f6edb4`). Status `done`.
