# Stream response switch — CloudAgents first, Actor second

Date: 2026-09-23

## Why ~60s feels slow today

Live Ambit path ([RunAgentActor.fs](../../../src/Server/RunAgentActor.fs)) calls `AgentRunner.start` then polls `GET /v1/agents/{id}/runs/{runId}` every **50ms** until `FINISHED`. Polling is cheap; the wait is almost entirely **Cursor Cloud Agent runtime** (VM boot, model work, tools). Console used **5000ms** poll intervals, which only adds up to seconds of extra latency after the run completes, not the main minute.

Nothing streams assistant text to the client until the full `result` is available, so the UI stays blank for the whole run.

## Recommended slice order

1. **[17 — CloudAgents Console stream](../issues/17-cloudagents-console-stream.md)** — vendor-neutral stream on the DLL, SSE in `CursorHttp` / `CursorAdapter`, `setFakeStream` proof, Console prints deltas. **Implemented (slice 1) in this change set.**
2. **[18 — AI Actor stream](../issues/18-ai-actor-stream.md)** — `RunAgentActor` consumes `streamUntilComplete`, pending-buffer `addChild` for `<>` fragments, cancel mid-stream. **Blocked until 17 is stable in production.**

Poll APIs (`start` / `poll` / `cancel` / `waitUntilComplete`) stay unchanged for callers that have not moved yet.

## Public API shape (CloudAgents DLL)

Types in `PublicTypes.fs`:

- `AgentStreamEvent` — `AssistantText`, `RunFinished`, `RunFailed`, `RunCancelled`

Runner in `AgentRunner.fs`:

- `streamUntilComplete config agentId runId pollIntervalMs maxWaitMs onEvent` — invokes `onEvent` for each event; returns final `AgentResult` or `AgentError`
- `setFakeStream (StartArgs -> AgentStreamEvent list) option` — CI-friendly delta sequences (requires active `setFake Some`)
- Without `setFakeStream`, fake mode **synthesizes** two `AssistantText` chunks from the poll handler’s `Finished` text

Settings-blind: still only `RunnerConfig.ApiKey` on the public surface.

## Cursor SSE consumption (F#)

Endpoint: `GET https://api.cursor.com/v1/agents/{agentId}/runs/{runId}/stream` with the same Basic auth as other `CursorHttp` calls.

Implementation ([CursorHttp.fs](../../../src/CloudAgents/Internal/CursorHttp.fs)):

1. `HttpClient.GetAsync(..., ResponseHeadersRead)` — do not buffer the full body.
2. `StreamReader.ReadLine()` loop — SSE blocks separated by blank lines.
3. Accumulate `event:` and `data:` lines; on blank line, dispatch.
4. Map events (vendor-specific) to callbacks:
   - `assistant` + JSON `{ "text": "..." }` → assistant callback
   - `result` + JSON (same shape as poll `result` / `git`) → terminal `AgentResult`
   - `error` → fail
   - `done` → terminal empty result (deltas may already have been printed)
5. `parseSseDocument` — pure function for tests and replay.

`CursorAdapter.streamRun` wraps HTTP, maps auth errors, and raises `AgentStreamEvent` on the public callback.

## Test strategy

| Layer | What |
|-------|------|
| `CursorHttpJsonTests` | `parseSseDocument` on fixture SSE text |
| `AgentRunnerFakeTests` | `setFakeStream` emits ordered deltas + `RunFinished`; no live key |
| Existing poll/fake tests | Unchanged — poll path still green |
| Live Cursor | Manual via Console + `CURSOR_API_KEY` (not CI) |

## Slice 1 status (this PR)

- [x] `AgentStreamEvent` + `streamUntilComplete` + `setFakeStream`
- [x] `CursorHttp.streamRun` + `CursorAdapter.streamRun`
- [x] Console uses stream (50ms fake wait, SSE live)
- [x] README updates (library + Console)
- [ ] Ticket 17 checklist: mark **coded** after review; optional live SSE soak

## Next (18)

- Wire `RunAgentActor` to `streamUntilComplete` instead of `pollUntilDone` on live path
- Pending-buffer tokenizer for Focus children
- Fake-stream Actor tests (multi-delta Focus growth, cancel mid-stream)
