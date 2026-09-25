# 02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig

**Status:** coded
Actual: 45m
**Blocked by:** None
**Type:** coding

**Eventual / deferred from the oneshot first slice.** Keep as the eventual inbound door. First slice binds `GrokBotConfig` at composition for [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md) only — do not invent inbound body fields in Server now. Library oneshot is [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) / [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md). Do not delete this ticket. Do not implement it in the oneshot slice.

## Context

The bot must post durable text into Ambit without a Graph write API and without Actor Credential secrets. Grill locked the door as `POST /ambit/actors/deliver` with header `X-Ambit-Inbound-Secret` and body exactly `sessionId` + `text`. Secrets bind from .NET User Secrets under `grokbot:*` for localhost and Azure alike. The door still does not Finish Actors — fuller-channel inbox Finish is the eventual gbot function’s job ([03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md)), not the inbound door.

## What to build

Composition binds GrokbotConfig from User Secrets. RouteRegistration exposes `POST /ambit/actors/deliver`: check `X-Ambit-Inbound-Secret`, decode `{ sessionId, text }`, call pool `deliver`, map not-live to 404. No Graph writes at the door. Does not require FocusXmlStream ([18 — AI Actor stream](../../llm-connector/issues/18-ai-actor-stream.md)).

### 1. GrokbotConfig

Per [bot-channel architecture](../arch.md) module **GrokbotConfig**.

1. [x] 2.2.2.1 fromConfig — bind `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret` (strings; may be empty until Alan loads them)
2. [x] 2.2.2.2 No Graph secrets — secrets never persist into Graph Nodes

### 2. InboundAuth + ActorsDeliverDoor

Per [bot-channel architecture](../arch.md) module **InboundAuth + ActorsDeliverDoor** on [[src/Server/RouteRegistration.fs]].

1. [x] 2.4.2.1 Secret header — require `X-Ambit-Inbound-Secret` equal to configured `InboundSecret`; missing/wrong rejected; empty configured secret fails closed
2. [x] 2.4.2.2 Body shape — JSON body exactly `sessionId` + `text` (no optional `commandId`)
3. [x] 2.4.2.3 deliver + 404 — call `pool.deliver(sessionId, text)`; unknown / not live → HTTP 404; success → ack
4. [x] 2.4.2.4 Door stays thin — door does not write Graph and does not Finish Actors

### 3. Proof

1. [x] 1.2 Inbound seam — harness: wrong secret rejected; good secret + unknown sessionId → 404; good secret enqueues and feeds `GrokBotRunner.deliver`

## See also

[bot-channel architecture](../arch.md) modules **GrokbotConfig**, **InboundAuth + ActorsDeliverDoor**, [[../spec.md]] User Stories **Inbound shared-secret header**, **Secrets from User Secrets**, [[../map.md]] Decisions (inbound path + User Secrets)

## Comments

- 2026-09-25 — Coded `POST /ambit/actors/deliver` + `X-Ambit-Inbound-Secret`. Status `coded`.

## Time

- 2026-09-25 45m — inbound door + Api proofs (from chat)
