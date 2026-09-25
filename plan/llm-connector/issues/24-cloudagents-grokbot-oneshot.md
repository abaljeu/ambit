# 24 — CloudAgents Grok Bot oneshot stream

**Status:** done
Actual: 2h 20m
**Blocked by:** None — [17 — CloudAgents Console stream](17-cloudagents-console-stream.md) is `done`.
**Type:** coding

## Context

Alan asked for a oneshot Grok Bot stream face on the CloudAgents library ([Gambol.CloudAgents.fsproj](../../../src/CloudAgents/Gambol.CloudAgents.fsproj)). Bot-channel architecture rejected stretching Cursor `AgentRunner` into a keep-alive webhook conversation for the Server Actor product path. This ticket is the library slice only: one wake, stream assistant text, terminate on `RunFinished`, cancel aborts mid-stream. Next query is a new oneshot. Server Actor wiring (CoreActorPool, inbound door, Run Agent Actor `gbot`) is out of scope.

## What to build

### 1. Grok Bot oneshot runner

1. [x] Config types `GrokBotConfig` / `GrokBotWakeArgs` / `GrokBotStreamArgs` (one typed object each). Library stays settings-blind; caller binds User Secrets `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret`.
2. [x] `GrokBotRunner.wake` — ack-only POST; HTTP body is never bot reply text. Empty `WakeUrl` fails without sending and without writing secrets.
3. [x] `GrokBotRunner.streamUntilComplete` folds shared `AgentStreamEvent` until `RunFinished`. Fake/harness Done until the live Done seam locks. Do not invent inbound body fields.
4. [x] `GrokBotRunner.cancel` aborts mid-stream (no success Finish; no close-notify wake).
5. [x] Sibling modules (`GrokBotRunner`, `GrokBotFake`, `Internal/GrokBotHttp`, `Internal/GrokBotAdapter`). Do not change Cursor `AgentRunner.start` / Cursor HTTP.

### 2. Wake auth and Done seam (Unsettled)

1. [x] Wake auth header matches the Admiral hub / bot webhook contract — do not invent an Ambit-only header. Repo has no hub header evidence: `GrokBotHttp.applyWakeAuth` is the adapter seam (no extra header until confirmed).
2. [x] Done seam stays Unsettled (`kind: close` may be it). Live stream without fake returns `InvalidResponse` (Done seam Unsettled). Fake `setFakeStream` emits `RunFinished`.

### 3. Proof

1. [x] Fake oneshot emits `AssistantText` chunks then `RunFinished`.
2. [x] Cancel mid-stream yields cancelled (no success Finish).
3. [x] Empty `WakeUrl` fails safely without writing secrets.
4. [x] Existing CloudAgents Cursor tests still pass. Live hub not required.

### 4. Non-goals

1. CoreActorPool `sessionId` / `deliver` / commandId exclusivity.
2. Inbound `POST /ambit/actors/deliver` Server door.
3. Run Agent Actor `gbot` function / FocusXmlStream writes.
4. Stretching `AgentRunner` into a keep-alive wire.
5. Land / squash onto staging without Alan accept.

## See also

[CloudAgents README](../../../src/CloudAgents/README.md), [17 — CloudAgents Console stream](17-cloudagents-console-stream.md), [plan/bot-channel/spec.md](../../bot-channel/spec.md)

## Comments

- 2026-09-25 — Filed and coded: sibling `GrokBotRunner` oneshot (wake ack-only, fake stream until `RunFinished`, cancel). Wake auth and Done seam Unsettled as documented. Status `coded`.
- 2026-09-25 — Standards review hard item: split `cancel mid-stream yields cancelled not Finish` via helpers. Reports folded from the independent review. Status stays `coded`.
- 2026-09-25 — Alan accepted; squash-landed. Status `done`.

## Time

- 2026-09-25 2h — Library oneshot runner + fakes + CloudAgents.Tests proofs (from chat)
- 2026-09-25 20m — Split cancel test under 40 lines; fold review reports (from chat)
