# Scope-before-encode — implementation report

Date: 2026-08-27
Branch: `w/relaxed-concurrency`

## Problem

Production `/ambit/state` measured ~3.5s TTFB with ~3.7M-char JSON. Pipeline was:

1. Agent `GetState` → encode full graph to JSON
2. `Api.getState` → decode JSON → `bootstrapStateResponse` → re-encode scoped graph

Two full-graph JSON passes on every bootstrap request.

## Approach

**Agents return `StateResponse`; Api scopes then encodes once.**

- `AgentHandle.getState` now returns `Async<Result<StateResponse, string>>` instead of pre-encoded JSON.
- `FileAgent` / `DbAgent` `GetState` replies with in-memory `{ graph; revision; isReady }` — no JSON in the agent path.
- `Api.getState` applies `ResidentProjection.bootstrapStateResponse` (RootClosure + optional `?zoom=`) then calls `encodeStateResponse` once.
- `?scope=full` still skips bootstrap projection; response is a single encode of the full graph.
- Internal callers (`loadPackages`, `postParseFile`, `LazyLoadReconciliationServer`, `SavePrep`) use `StateResponse` directly — no decode round-trip.

## Files changed

| Area | Files |
|------|-------|
| Agents | `src/Server/FileAgent.fs`, `src/Server/DbAgent.fs` |
| Api / routing | `src/Server/Api.fs`, `src/Server/RouteRegistration.fs` |
| Reconciliation / save | `src/Server/LazyLoadReconciliationServer.fs`, `src/Server/SavePrep.fs` |
| Tests | `tests/Server.Tests/ApiGetStateTests.fs`, `ApiPostLoadTests.fs`, `DbAgentTests.fs`, `DbAgentFailureTests.fs`, `FileAgentFailureTests.fs`, `LazyLoadReconciliationServerTests.fs`, `SavePrepTests.fs` |

Shared scoping logic unchanged: `src/Shared/ResidentProjection.fs` (`bootstrapGraph`, `bootstrapStateResponse`).

## Tests

```
dotnet build src/Server -c Debug                          → OK
dotnet test tests/Server.Tests --filter ApiGetStateTests|ApiPostLoadTests|DbAgentTests|FileAgentFailureTests|DbAgentFailureTests|LazyLoadReconciliationServerTests|SavePrepTests
→ Passed: 57, Failed: 0
```

Existing `ApiGetStateTests` cover default RootClosure scoping, `?scope=full`, and `?zoom=` workspace merge.

## Expected TTFB impact

- **Eliminated:** full-graph Thoth JSON encode inside agents on every `GetState`.
- **Eliminated:** full-graph JSON decode in `Api.getState` before scoping.
- **Remaining work per request:** one encode of the scoped graph (RootClosure is much smaller than full graph on large workspaces).
- Production ratio depends on workspace size; with ~3.7M-char full JSON, scoped bootstrap should be orders of magnitude smaller when most nodes live under Workspaces.

Compression (gzip) remains a separate follow-up; it reduces transfer size but does not remove encode cost.

## Follow-up

- HITL / production timing validation after deploy.
- Optional: server response compression middleware.
- Optional: boot instrumentation to confirm TTFB and payload size in the field.

## Suggested commit message

```
Scope graph before JSON encode on GET /ambit/state.

Agents return StateResponse; Api applies bootstrap projection then encodes once.
Removes full-graph encode in agents and decode/re-encode in Api.getState.
```
