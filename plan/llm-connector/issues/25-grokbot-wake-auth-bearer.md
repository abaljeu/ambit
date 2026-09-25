# 25 — Grok Bot wake auth Bearer

**Status:** coded
Actual: 1h 30m
**Blocked by:** None — [24 — CloudAgents Grok Bot oneshot stream](24-cloudagents-grokbot-oneshot.md) is `done`.
**Type:** coding

## Context

[24 — CloudAgents Grok Bot oneshot stream](24-cloudagents-grokbot-oneshot.md) left outbound wake auth Unsettled: `GrokBotHttp.applyWakeAuth` was the adapter seam and forbade inventing an Ambit-only header (`X-Ambit-Wake-Secret`) or reusing inbound `X-Ambit-Inbound-Secret` for wake. Ready later attached `X-Ambit-Wake-Secret`. Alan confirmed from the Admiral hub panel: wake uses `Authorization: Bearer <key>`. This ticket settles that Unsettled and replaces the invented header. Inbound stays `X-Ambit-Inbound-Secret` (eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](../../bot-channel/issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](../../bot-channel/issues/03-gbot-wake-inbox-focus.md)). Prefer `WakeSecret` ≠ `InboundSecret` in config values.

## What to build

### 1. Plan lock

1. [x] Record wake auth as `Authorization: Bearer {WakeSecret}` on [24 — CloudAgents Grok Bot oneshot stream](24-cloudagents-grokbot-oneshot.md) and [bot-channel](../../bot-channel/map.md) map / spec / arch so they agree. Do not leave hub header name Unsettled.
2. [x] Name-and-link issue ids with `[label](path)` links.

### 2. Library wake header

1. [x] [GrokBotHttp.applyWakeAuth](../../../src/CloudAgents/Internal/GrokBotHttp.fs) sets `Authorization: Bearer <WakeSecret>` on the `HttpRequestMessage`.
2. [x] Empty or whitespace secret does not invent a fake header; leave unset. [GrokBotAdapter.wake](../../../src/CloudAgents/Internal/GrokBotAdapter.fs) already fails closed (`missing wake secret` / `unauthorized`) without writing the secret into error text.
3. [x] Do not send `X-Ambit-Wake-Secret` or `X-Ambit-Inbound-Secret` on wake.

### 3. Proof

1. [x] CloudAgents grokbot collection: wake request carries `Authorization: Bearer …` with the configured `WakeSecret`.
2. [x] Same request does not carry `X-Ambit-*` for wake. Empty secret leaves Authorization unset.
3. [x] Existing CloudAgents grokbot facts stay green. Live hub not required.

### 4. Non-goals

1. Actor wiring, inbound door, Done seam, or User Secret key names (`grokbot:WakeUrl` / `WakeSecret` / `InboundSecret`).
2. Changing inbound to Bearer.
3. Land / squash onto ready or staging without Alan accept.

## See also

[24 — CloudAgents Grok Bot oneshot stream](24-cloudagents-grokbot-oneshot.md), [bot-channel spec](../../bot-channel/spec.md), [bot-channel map](../../bot-channel/map.md), [bot-channel architecture](../../bot-channel/arch.md), [GrokBotHttp](../../../src/CloudAgents/Internal/GrokBotHttp.fs), [GrokBotOneshotTests](../../../tests/CloudAgents.Tests/GrokBotOneshotTests.fs)

## Comments

- 2026-09-25 — Filed: Alan confirmed hub wake is `Authorization: Bearer <WakeSecret>`. Status `defined`.
- 2026-09-25 — Coded: `applyWakeAuth` sets `Authorization: Bearer <WakeSecret>`; empty/whitespace leaves Authorization unset; no `X-Ambit-*` on wake. Proofs in [GrokBotOneshotTests](../../../tests/CloudAgents.Tests/GrokBotOneshotTests.fs). Status `coded`.

## Time

- 2026-09-25 1h 30m — Plan lock + Bearer wake header + CloudAgents grokbot proofs (from chat)
