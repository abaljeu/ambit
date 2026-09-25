# 04 — CloudAgents Grok Bot oneshot library

**Status:** done
Actual: see [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md)
**Blocked by:** None — [17 — CloudAgents Console stream](../../llm-connector/issues/17-cloudagents-console-stream.md) is `done`
**Type:** coding

## Context

Alan remapped this ticket (2026-09-24/25). It is the CloudAgents Grok Bot oneshot library that already landed as [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) (`done` on staging). It is not TestActor `?test gbot` simulation. That older story was a mis-slot; move it under eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md) / [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md) / [03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md), or a later note — not as the current [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md).

The library slice is one wake, stream assistant text, terminate on `RunFinished`, cancel mid-stream. Next query is a new oneshot. Server Actor wiring is [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md). Eventual channel (pool `sessionId` + deliver, inbound door, fuller wake+inbox) stays [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md).

## What landed

Pointer only — do not re-implement. See [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) for the coded checklist.

1. [x] `GrokBotConfig` / `GrokBotWakeArgs` / `GrokBotStreamArgs` — library stays settings-blind; caller binds User Secrets `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret`
2. [x] `GrokBotRunner.wake` — ack-only POST; empty `WakeUrl` fails without sending and without writing secrets
3. [x] `GrokBotRunner.streamUntilComplete` folds shared `AgentStreamEvent` until `RunFinished`
4. [x] `GrokBotRunner.cancel` aborts mid-stream (no success Finish; no close-notify wake)
5. [x] Sibling modules `GrokBotRunner`, `GrokBotFake`, `Internal/GrokBotHttp`, `Internal/GrokBotAdapter` — Cursor `AgentRunner` unchanged
6. [x] Oneshot Done is empty `text` on deliver (`GrokBotRunner.deliver` → `RunFinished`). Prior research **06 — Done seam for response concluded** is superseded. Absolute wake `responseUrl` is [06 — Wake response URL](06-wake-response-url.md). Fake `setFakeStream` still emits harness `RunFinished`

## Later (not this ticket)

1. TestActor `?test gbot` canned simulation — eventual / later, under [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md). Not the current [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md).

## See also

[24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md), [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md), [src/CloudAgents/GrokBotRunner.fs](../../../src/CloudAgents/GrokBotRunner.fs)

## Comments

- 2026-09-25 — Alan remapped [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) from TestActor gbot simulation to the CloudAgents oneshot library already `done` as [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md). Status `done`.

## Time

- 2026-09-25 — Remap only; implementation time lives on [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) (from chat)
