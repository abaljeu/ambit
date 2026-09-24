# Code review: CloudAgents Console stream

Range: uncommitted changes vs `HEAD` on `dev`. Spec: [17 — CloudAgents Console stream](../issues/17-cloudagents-console-stream.md). Axes stayed separate. This report is not approval. Ticket status stays `coded`.

## Standards

### Hard

1. **Bare id** — [stream-response-switch.md](stream-response-switch.md) line 65: `Ticket 17 checklist`. Scan `BARE_ID`. [.agents/rules/refer-by-name.md](../../../.agents/rules/refer-by-name.md): never refer by only the id; include the name. Same file, line 14 `Blocked until 17 is stable` and the heading `Next (18)` name those issues by number alone.
2. **Adjacent edit** — [Program.fs](../../../src/CloudAgents.Console/Program.fs) replaces `printModels models source` with `//printModels models source`. [.agents/rules/core-agent-behavior.md](../../../.agents/rules/core-agent-behavior.md) Surgical Changes: every changed line traces to the request. Catalog printing sits outside the stream slice.

### Judgement

- **Duplicated Code** — `interpretSseDocument` and `readSse` share one `advanceSse` / `finishSse` loop. Quote: `| line :: tail -> match advanceSse onAssistant acc line with | SseContinue next -> loop next tail | SseDone outcome -> outcome` and the `ReadLine` twin in `readSse`.
- **Mysterious Name** — `cancelledStream` is the shared cancel error, also called from `waitLive`: `let private cancelledStream () = Error(ApiError("cancelled", "Agent run was cancelled"))`.
- **Parameter Explosion** — `streamUntilComplete` takes `config`, `agentId`, `runId`, `pollIntervalMs`, `maxWaitMs`, `onEvent`. [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md) says group related parameters.
- **Mutable cells** — new `ref` in [Program.fs](../../../src/CloudAgents.Console/Program.fs) (`wroteText`) and Fake in [AgentRunner.fs](../../../src/CloudAgents/AgentRunner.fs) (`streamHandler`, `streamEvents`). [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md): "Don't use mutable."

## Spec

### (a) Missing or partial

Partial: a live SSE `error` never becomes `RunFailed`. `CursorAdapter.streamRun` returns `NetworkError` and does not call `onEvent`. Spec: “Cursor adapter consumes SSE; map at least: `assistant` `{ text }` deltas, terminal `result` / `done` / `error`.”

### (b) Scope creep

`printCatalog` no longer prints models (`//printModels models source`). The ticket does not change catalog output.

The same change set edits [ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md](../../roadmap/epics/chapters/ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md) (“always exclude” → “default exclude”). Spec: “This ticket adds a vendor-neutral stream on the CloudAgents DLL and proves it in Console.”

### (c) Implemented but wrong

The Console README still says the catalog is printed. Spec: “Document usage in CloudAgents.Console README.”

## Summary

Standards: 6 findings (2 hard, 4 judgement); worst is the commented-out `printModels` call in [Program.fs](../../../src/CloudAgents.Console/Program.fs). Spec: 4 findings; worst is a live SSE `error` returned as `NetworkError` instead of `RunFailed`.
