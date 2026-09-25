# 05 — gbot Finish on response end

**Status:** `defined`
**Blocked by:** [[03-gbot-wake-inbox-focus.md|03 — gbot Run Agent: wake + inbox → Focus stream]], [[04-testactor-gbot-simulation.md|04 — TestActor gbot simulation]]
**Type:** coding

## Context

[03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) and [04 — TestActor gbot simulation](04-testactor-gbot-simulation.md) ship the channel plumbing: one `?ai gbot` wake, inbox deliver, FocusXmlStream growth, and a `?test gbot` canned stream. After wake ack the Actor can still sit live. The first-slice variant is the easier Cursor-Cloud-like job: when that streamed reply concludes, the Actor Finishes so Done / chrome match `?ai` cursor. Next query is a new Run / new `sessionId`. Cancel still drops mid-stream. Destination keep-alive (live until Cancel/drop; multi-turn without Finish-on-every-reply) stays later — this ticket does not delete it. Memory policy (clean vs continue chat context) stays deferred. Do not invent a second product command or Actor name.

## What to build

After the first-slice stream is in place, gbot (and `?test gbot`) Finishes when that response concludes. Chrome clears like cursor. The live row and `sessionId` drop. A later inbound on that `sessionId` is 404. The next person query is a new Run that mints a new `sessionId`. Cancel mid-stream still aborts (no Finish-as-success). Exact inbound Done seam stays Unsettled — see [06 — Done seam for response concluded](06-done-seam-response-concluded.md). Until that lock lands, TestActor canned-end is a valid Finish trigger; do not invent an inbound body field on [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md) here. [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md) already clears the session index on Finish — no extra pool ticket.

### 1. Run Agent Actor — gbot function

Per [[../arch.md|bot-channel architecture]] module **Run Agent Actor — gbot function** and story path **Finish on response end (first slice)**.

1. [ ] 5.2 Actor Finishes — when the streamed reply concludes, Finish (same class as CloudAgents RunFinished / ActorFinished on `?ai` cursor). Do not Finish on wake ack
2. [ ] 5.3 Chrome and wire drop — live row and `sessionId` drop; Browser chrome clears like cursor Done
3. [ ] 5.5 Next query new job — a new Run on the same Command mints a new `sessionId`; do not keep a long-lived live wire for more queries in this slice
4. [ ] 4.1 Cancel still aborts — Cancel / drop mid-stream still stops the loop; accepted Focus Children stay; no close-notify from Ambit
5. [ ] Unsettled Done seam — call out that the wire signal is Unsettled (`kind: close` may be it). Consume [06 — Done seam for response concluded](06-done-seam-response-concluded.md) when locked; until then do not invent inbound kind on the deliver door

### 2. TestActor

Per [[../arch.md|bot-channel architecture]] module **TestActor** and story path **Simulate gbot via TestActor**. Same `?test gbot` path — not a second product command.

1. [ ] 8.6 Finish after canned stream — after the canned texts are applied through deliver + FocusXmlStream, the Actor Finishes (first-slice parity)
2. [ ] Hello unchanged — `?test hello` and unknown `?test` behaviors stay as [04 — TestActor gbot simulation](04-testactor-gbot-simulation.md) left them

### 3. Proof

1. [ ] 1.5 Finish path — after a streamed gbot reply concludes (fake inbound or `?test gbot` canned-end), Poll shows ActorFinished / live Focus gone the way `?ai` cursor does; `sessionId` no longer delivers
2. [ ] 1.4 Cancel still — Cancel mid-stream still drops the wire without a successful Finish

## See also

[[../arch.md|bot-channel architecture]] story path **Finish on response end (first slice)**, modules **Run Agent Actor — gbot function** and **TestActor**, [[../spec.md]] User Story **Finish on response end (first slice)**, [[../map.md]] Decisions (2026-09-24/25 Finish-on-response-end), [06 — Done seam for response concluded](06-done-seam-response-concluded.md)
