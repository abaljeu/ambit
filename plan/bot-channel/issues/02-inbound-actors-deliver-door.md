# 02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig

**Status:** `defined`
**Blocked by:** None — can start immediately (may land beside 01; wire `deliver` when 01 is present, or stub the call behind the same interface)
**Type:** coding

## Context

The bot must post durable text into Ambit without a Graph write API and without Actor Credential secrets. Grill locked the door as `POST /ambit/actors/deliver` with header `X-Ambit-Inbound-Secret` and body exactly `sessionId` + `text`. Secrets bind from .NET User Secrets under `grokbot:*` for localhost and Azure alike. The door still does not Finish Actors — first-slice Finish-on-response-end is the gbot function’s job ([[03-gbot-wake-inbox-focus.md|03 — gbot Run Agent: wake + inbox → Focus stream]]), not the inbound door.

## What to build

Composition binds GrokbotConfig from User Secrets. RouteRegistration exposes `POST /ambit/actors/deliver`: check `X-Ambit-Inbound-Secret`, decode `{ sessionId, text }`, call pool `deliver`, map not-live to 404. No Graph writes at the door. Does not require FocusXmlStream (llm-connector 18).

### 1. GrokbotConfig

Per [[arch.md|bot-channel architecture]] module **GrokbotConfig**.

1. [ ] 2.2.2.1 fromConfig — bind `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret` (strings; may be empty until Alan loads them)
2. [ ] 2.2.2.2 No Graph secrets — secrets never persist into Graph Nodes

### 2. InboundAuth + ActorsDeliverDoor

Per [[arch.md|bot-channel architecture]] module **InboundAuth + ActorsDeliverDoor** on [[src/Server/RouteRegistration.fs]].

1. [ ] 2.4.2.1 Secret header — require `X-Ambit-Inbound-Secret` equal to configured `InboundSecret`; missing/wrong rejected; empty configured secret fails closed
2. [ ] 2.4.2.2 Body shape — JSON body exactly `sessionId` + `text` (no optional `commandId`)
3. [ ] 2.4.2.3 deliver + 404 — call `pool.deliver(sessionId, text)`; unknown / not live → HTTP 404; success → ack
4. [ ] 2.4.2.4 Door stays thin — door does not write Graph and does not Finish Actors

### 3. Proof

1. [ ] 1.2 Inbound seam — harness or test host: wrong secret rejected; good secret + unknown sessionId → 404; with a live row from 01 (or test double) → deliver enqueues

## See also

[[../arch.md|bot-channel architecture]] modules **GrokbotConfig**, **InboundAuth + ActorsDeliverDoor**, [[../spec.md]] User Stories **Inbound shared-secret header**, **Secrets from User Secrets**, [[../map.md]] Decisions (inbound path + User Secrets)
