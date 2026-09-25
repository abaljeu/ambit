# 05 — Run Agent Actor Grok Bot oneshot

**Status:** done
Actual: 2h
**Blocked by:** None — [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) is `done`. Eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) do not block this slice.
**Type:** coding

## Context

First-slice product path: add the Grok Bot oneshot backend to the existing [src/Server/RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) runner. Same Actor shape as Cursor: pack extract, start/wake, `streamUntilComplete` + FocusXmlStream fold on `AssistantText`, Finish on `RunFinished`, Cancel mid-stream. Divergent details stay abstracted in CloudAgents (`AgentRunner` vs `GrokBotRunner`). Do not build a separate Server inbox loop or WakeHttp Actor path. Do not invent a second Actor name. Do not implement eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md).

The old “Finish-on-response-end after inbox plumbing” story assumed [03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) / [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) channel tickets first. That is superseded: oneshot library is [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) / [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md); this ticket is the Actor wiring.

## What to build

### 1. Behavior selection on Run Agent Actor

Per [arch.md](../arch.md) first-slice cut and module **Run Agent Actor — gbot function** on [src/Server/RunAgentActor.fs](../../../src/Server/RunAgentActor.fs).

1. [x] First token `gbot` — `?ai gbot` (extra tokens ignored) uses `GrokBotRunner` wake + `streamUntilComplete` + cancel
2. [x] Other behaviors — keep Cursor `AgentRunner` (cursor path unchanged)
3. [x] One Actor name — `ai`; no second registration
4. [x] Shared FocusXmlStream fold — one `AssistantText` / `RunFinished` path for both backends

### 2. Grok oneshot on the existing runner

1. [x] Pack extract — same `AiExtractPack` as cursor
2. [x] Wake — `GrokBotRunner.wake` with pack + `commandId` + `focusId` + Actor-minted oneshot `sessionId` (not pool deliver)
3. [x] Stream — `GrokBotRunner.streamUntilComplete` + the shared FocusXmlStream fold
4. [x] Finish — `RunFinished` is the same class of terminus as cursor (`ActorSucceeded` / live Focus gone)
5. [x] Cancel — mid-stream abort via `GrokBotRunner.cancel`; no success Finish; no close-notify
6. [x] Empty `WakeUrl` — failure surfaces safely (`ActorFailed`); secrets do not land in Graph

### 3. Composition bind

1. [x] Bind `GrokBotConfig` from User Secrets / config `grokbot:WakeUrl` / `WakeSecret` / `InboundSecret` at composition
2. [x] Library stays settings-blind
3. [x] Do not invent inbound body fields in Server. Oneshot Done is empty `text` on deliver. Absolute wake `responseUrl` is [06 — Wake response URL](06-wake-response-url.md)

### 4. Proof

Live `GrokBotAdapter.streamRun` waits on inbound `GrokBotRunner.deliver`. Empty `text` is oneshot Done. Proofs use `GrokBotRunner.setFake` / `setFakeStream` the way Cursor tests use fakes.

1. [x] Fake Grok stream → Focus growth / Finish class of terminus
2. [x] Cancel mid-stream drops live and keeps streamed children
3. [x] Empty `WakeUrl` failure surfaces safely (no secrets in Graph)
4. [x] Cursor `?ai` path unchanged

## Comments

- 2026-09-25 — Coded on existing `RunAgentActor`: first token `gbot` uses `GrokBotRunner`; shared FocusXmlStream fold; composition bind `GrokBotSettings.fromConfig`. Fake-stream proofs plus empty `WakeUrl`. Status `coded`.
- 2026-09-25 — Standards review: name-and-link bare issue ids; replace labeled wikilinks; drop unused `open Gambol.Shared`. Reports folded from the independent review. Alan accepted; squash-landed. Status `done`.

## Time

- 2026-09-25 2h — Actor wiring + composition bind + Server.Tests proofs (from chat)
- 2026-09-25 20m — Standards name-and-link + markdown links; fold review reports; Status `done` (from chat)

## Non-goals

1. CoreActorPool `sessionId` / `deliver` / `commandId` exclusivity
2. Inbound `POST /ambit/actors/deliver` Server door
3. Separate Server inbox loop / WakeHttp Actor path
4. TestActor `?test gbot` simulation
5. Inventing inbound Done-seam body fields

## See also

[04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md), [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md), [src/Server/RunAgentActor.fs](../../../src/Server/RunAgentActor.fs), [src/CloudAgents/GrokBotRunner.fs](../../../src/CloudAgents/GrokBotRunner.fs), [06 — Wake response URL](06-wake-response-url.md)
