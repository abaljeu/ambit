# 06 — Done seam for response concluded

**Status:** `defined`
**Blocked by:** None — can start immediately
**Type:** research

## Question

How does “the response message concluded” arrive so the first-slice gbot Actor can Finish (Cursor Cloud class of terminus)? Product behavior is already locked on [[../map.md]] Decisions 2026-09-24/25: Actor Finish when that reply ends; next query is a new Run / new `sessionId`; Cancel still drops mid-stream. The **wire signal** is Unsettled. This ticket locks that seam. It does not implement Server code. It does not design memory policy (clean vs continue chat context). It does not cancel destination keep-alive close-notify or fuller-channel `kind: close` decisions.

## Options

1. **Bot `kind: close`** — the bot POSTs an inbound close kind as the “response concluded” signal. May need a body field beyond `{ sessionId, text }`; that would amend the inbound-body lock on [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md). Prefer this if the Admiral hub already has `kind: close`.
2. **Harness Done** — the Actor or a test harness treats stream completion (or a test-only Done) as terminus. No new inbound field. Thinnest for [04 — TestActor gbot simulation](04-testactor-gbot-simulation.md) canned-end.
3. **Explicit inbound kind** — a dedicated inbound kind other than `close` (still an inbound field; same door-amend cost as option 1).
4. **Empty sentinel** — empty `text` (or a similar empty payload) means end. Reuses the locked body shape; easy to send by accident.

## What to lock

Record one first-slice Done seam. Prefer option 1 when the hub already has `kind: close`; otherwise pick the thinnest seam that [05 — gbot Finish on response end](05-gbot-finish-on-response-end.md) can consume. Write the choice into [[../arch.md]] Unsettled **Done seam for “response concluded”** (settle it) and a one-line pointer on [[../map.md]] Not yet specified. If the choice needs a body field, name the 02 amend; do not invent the field in this ticket. Destination keep-alive close-notify / fuller-channel `kind: close` stay later.

## See also

[[../arch.md|bot-channel architecture]] Unsettled **Done seam for “response concluded”**, story path **Finish on response end (first slice)**, [[../map.md]] Decisions (2026-09-24/25 Finish-on-response-end), [05 — gbot Finish on response end](05-gbot-finish-on-response-end.md)
