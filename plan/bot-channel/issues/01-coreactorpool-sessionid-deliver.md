# 01 — CoreActorPool sessionId + deliver + commandId exclusivity

**Status:** coded
Actual: 45m
**Blocked by:** None
**Type:** coding

**Eventual / deferred from the oneshot first slice.** Keep as the eventual channel target (pool `sessionId` + `deliver` + `commandId` exclusivity). First slice is library [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) (`done`, pointer to [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md)) plus Actor wiring [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md). Do not delete this ticket. Do not implement it in the oneshot slice.

## Context

A Grok Bot conversation needs a live wire id the bot can echo, and a way for any live Actor to receive inbound text without learning Actor Credential secrets. Today [CoreActorPool](../../../src/Server/Core/CoreActorPool.fs) indexes live rows by Credential and Focus only. Before the inbound HTTP door or the fuller inbox gbot function can land, the pool must mint `sessionId`, reject a second start on the same `commandId`, and expose generalized `deliver`. Destination keep-alive (same `sessionId` across turns) stays later and does not change the pool fields.

## What to build

CoreActorPool live rows carry `sessionId` and `commandId`, reject a second Actor start while that `commandId` is live, and enqueue inbound text by `sessionId`. Focus exclusivity stays. Drop / Cancel / Finish clears the session index. Verifiable with pool-level tests — no HTTP and no FocusXmlStream required.

### 1. CoreActorPool

Extend the live registry per [bot-channel architecture](../arch.md) module **CoreActorPool** (State / Interface / Uses there — do not fork a second map).

1. [x] 2.5.1.2 Live row fields — live row holds `commandId`, `sessionId` (new Guid string per start), and an inbox queue of inbound texts
2. [x] 2.5.1.3 sessionId index — secondary index `sessionId` → live Credential for deliver lookup
3. [x] 2.5.2.2 Mint and exclusivity — on start, mint `sessionId`; store `commandId` from ActorStart; reject if any live row already has that `commandId`
4. [x] 2.5.2.3 Focus exclusivity — keep existing `focus already has a live Actor` admit
5. [x] 2.5.2.4 deliver — `deliver(sessionId, text)` enqueues to that live Actor’s inbox; Error when unknown or not live
6. [x] 2.5.2.5 Inbox opt-in — inbox is readable by the scheduled Actor body (`takeInbox`); Actors that never read ignore messages
7. [x] 2.5.2.6 Drop clears wire — Drop / Cancel / Finish clears `sessionId` index and inbox so a later deliver fails

### 2. Proof

1. [x] 1.Narrowest seam — pool tests: mint → deliver enqueues → second start same `commandId` rejected → drop → deliver Error; Focus exclusivity still rejects

## See also

[bot-channel architecture](../arch.md) module **CoreActorPool**, [[../spec.md]] User Stories **Mint sessionId on start**, **Deliver by sessionId**, **commandId exclusivity**, [[../map.md]] Decisions (three-ticket cut)

## Comments

- 2026-09-25 — Coded pool `sessionId` / `deliver` / `takeInbox` / `commandId` exclusivity. Status `coded`.

## Time

- 2026-09-25 45m — CoreActorPool live-row session + deliver proofs (from chat)
