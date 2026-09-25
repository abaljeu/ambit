# 06 — Done seam for response concluded

**Status:** defined
**Blocked by:** None — research; leave Unsettled
**Type:** research

## Question

How does “the response message concluded” arrive so the first-slice gbot Actor can Finish (Cursor Cloud class of terminus)? Product behavior is already locked: Actor Finish when that reply ends; next query is a new Run / new oneshot; Cancel still drops mid-stream. The **wire signal** stays Unsettled under the Grok adapter ([src/CloudAgents/Internal/GrokBotAdapter.fs](../../../src/CloudAgents/Internal/GrokBotAdapter.fs) `streamRun` returns `InvalidResponse`). This ticket locks that seam later. It does not implement Server code in this slice. It does not invent inbound body fields in Server. It does not design memory policy. It does not cancel destination keep-alive close-notify or fuller-channel `kind: close` decisions.

## Options

1. **Bot `kind: close`** — the bot POSTs an inbound close kind as the “response concluded” signal. May need a body field beyond `{ sessionId, text }`; that would amend the inbound-body lock on eventual [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md). Prefer this if the Admiral hub already has `kind: close`.
2. **Harness Done** — the Actor or a test harness treats stream completion (or a test-only Done) as terminus. No new inbound field. First-slice proofs already use `GrokBotRunner.setFakeStream` `RunFinished` this way.
3. **Explicit inbound kind** — a dedicated inbound kind other than `close` (still an inbound field; same eventual-door amend cost as option 1).
4. **Empty sentinel** — empty `text` (or a similar empty payload) means end. Reuses a locked body shape; easy to send by accident.

## What to lock

Record one first-slice Done seam when the hub contract is known. Until then leave Unsettled under the Grok adapter. Do not invent inbound body fields in Server for [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md). Prefer option 1 when the hub already has `kind: close`; otherwise keep harness `RunFinished` (option 2) for proofs. Write the choice into [arch.md](../arch.md) Unsettled **Done seam for “response concluded”** (settle it) and a one-line pointer on [map.md](../map.md) Not yet specified. If the choice needs a body field, name the eventual [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md) amend; do not invent the field in this ticket. Destination keep-alive close-notify / fuller-channel `kind: close` stay later.

## See also

[arch.md](../arch.md) Unsettled **Done seam for “response concluded”**, [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md), [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md), [src/CloudAgents/Internal/GrokBotAdapter.fs](../../../src/CloudAgents/Internal/GrokBotAdapter.fs)
