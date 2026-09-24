# 01 — CoreActorPool sessionId + deliver + commandId exclusivity

**Status:** `defined`
**Blocked by:** None — can start immediately
**Type:** coding

## Context

A Grok Bot conversation needs a live wire id the bot can echo, and a way for any live Actor to receive inbound text without learning Actor Credential secrets. Today [[src/Server/Core/CoreActorPool.fs|CoreActorPool]] indexes live rows by Credential and Focus only. Before the inbound HTTP door or the gbot function can land, the pool must mint `sessionId`, reject a second start on the same `commandId`, and expose generalized `deliver`.

## What to build

CoreActorPool live rows carry `sessionId` and `commandId`, reject a second Actor start while that `commandId` is live, and enqueue inbound text by `sessionId`. Focus exclusivity stays. Drop / Cancel / Finish clears the session index. Verifiable with pool-level tests — no HTTP and no FocusXmlStream required.

### 1. CoreActorPool

Extend the live registry per [[arch.md|bot-channel architecture]] module **CoreActorPool** (State / Interface / Uses there — do not fork a second map).

1. [ ] 2.5.1.2 Live row fields — live row holds `commandId`, `sessionId` (new Guid string per start), and an inbox queue of inbound texts
2. [ ] 2.5.1.3 sessionId index — secondary index `sessionId` → live Credential for deliver lookup
3. [ ] 2.5.2.2 Mint and exclusivity — on start, mint `sessionId`; store `commandId` from ActorStart; reject if any live row already has that `commandId`
4. [ ] 2.5.2.3 Focus exclusivity — keep existing `focus already has a live Actor` admit
5. [ ] 2.5.2.4 deliver — `deliver(sessionId, text)` enqueues to that live Actor’s inbox; Error when unknown or not live
6. [ ] 2.5.2.5 Inbox opt-in — inbox is readable by the scheduled Actor body; Actors that never read ignore messages
7. [ ] 2.5.2.6 Drop clears wire — Drop / Cancel / Finish clears `sessionId` index and inbox so a later deliver fails

### 2. Proof

1. [ ] 1.Narrowest seam — pool tests: mint → deliver enqueues → second start same `commandId` rejected → drop → deliver Error; Focus exclusivity still rejects

## See also

[[../arch.md|bot-channel architecture]] module **CoreActorPool**, [[../spec.md]] User Stories **Mint sessionId on start**, **Deliver by sessionId**, **commandId exclusivity**, [[../map.md]] Decisions (three-ticket cut)
