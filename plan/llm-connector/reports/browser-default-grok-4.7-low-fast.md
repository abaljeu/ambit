# Browser default: grok-4.7 Low Fast

## Change

[`RunAgentActor.fs`](../../../src/Server/RunAgentActor.fs) `askOptions` for `?ai` / Browser create matches catalog variant `context=256k,reasoning_effort=low,fast=true` (display **Grok 4.7 Low Fast**):

- `ModelHint = Some "grok-4.7"`
- `ModelParams`: `context=256k`, `reasoning_effort=low`, `fast=true`

Replaces invalid `cursor-grok-4.7-high` with empty params.

## Create wire path

`toStartArgs` passes `askOptions` into `AgentRunner.start` → [`CursorAdapter.startAgent`](../../../src/CloudAgents/Internal/CursorAdapter.fs) maps `ModelHint` + `ModelParams` to `model: { id, params }` → [`CursorHttp.createRequestJson`](../../../src/CloudAgents/Internal/CursorHttp.fs).

No Console or picker changes.

## Tests

- `CursorHttpJsonTests`: ``browser default grok create JSON sends model params`` (three params on create JSON)
- Filtered CloudAgents tests: `CursorHttpJsonTests`, `AgentRunnerFakeTests`

## Not done

- Ticket 22 status unchanged
- No commit
