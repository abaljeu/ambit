# 06 — Wake response URL

**Status:** defined
**Blocked by:** None — [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md) is `done`. Eventual [03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) stays deferred.
**Type:** coding

## Context

Cursor Cloud pulls an SSE after start. The Admiral hub wake is ack-only. The hub must POST reply text back to Ambit.

`POST /ambit/actors/deliver` already maps into the CloudAgents fold via `GrokBotRunner.deliver` (same FocusXmlStream path as Cursor SSE). Empty `text` on that door is the live oneshot Done terminus.

Azure is the reachable Ambit. A localhost hub → Ambit deliver does not work without a tunnel.

Today outbound wake JSON is `source`, `kind`, `sentAt`, `commandId`, `focusId`, `sessionId`, `text`, `payload` ([GrokBotHttp.wakeRequestJson](../../../src/CloudAgents/Internal/GrokBotHttp.fs)). The hub has no absolute URL for the deliver door.

The prior research ticket **06 — Done seam for response concluded** is superseded. Empty `text` on deliver is the live oneshot terminus. [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) and [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md) are `done`. Do not expand [03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md). Do not reopen Done-seam research.

## Definition of done

A build running on Azure can post a `?ai gbot` message, and grokbot has the info to send messages to deliver, which — because [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) and [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md) are complete — results in nodes being created under Focus. Architecturally, this happens by `/deliver` calling into the CloudAgents project (grokbot handlers), which then posts Change ops with its held Actor secret.

Ambit-repo scope is the wake `responseUrl` plus any missing Ambit wiring so the hub can POST deliver. Hub/bot reading that URL and POSTing is required for live Done, but hub implementation stays outside this repo (ops / dependency). Live Done is Azure Focus growth via deliver → CloudAgents → Actor Changes.

## What to build

### 1. Absolute response URL on outbound wake

1. [ ] Add an absolute deliver URL on the outbound wake JSON. Recommended field name: `responseUrl`. The Admiral hub must learn this field.
2. [ ] Value is the existing door `{origin}/ambit/actors/deliver`. Production example: `https://collaborative-systems.org/ambit/actors/deliver` (or the equivalent PublicAssetBase / Azure host).
3. [ ] Construct the origin from `PublicAssetBase` when set; otherwise the production host. Server composition owns the origin. The library stays settings-blind.
4. [ ] Pass the URL into the existing wake path (`GrokBotWakeArgs` / `GrokBotHttp.wakeRequestJson`). Do not put `InboundSecret` on the wake body.
5. [ ] Bearer wake auth stays `Authorization: Bearer {WakeSecret}` ([25 — Grok Bot wake auth Bearer](../../llm-connector/issues/25-grokbot-wake-auth-bearer.md)). Empty `WakeUrl` still fails safely without writing secrets.

### 2. Proof

1. [ ] Wake JSON in unit/library tests includes the absolute `responseUrl`.
2. [ ] Empty `WakeUrl` still fails safely (no send; no secrets in the error).
3. [ ] Bearer wake auth is unchanged (no `X-Ambit-*` on wake; no `InboundSecret` in the body).

## Non-goals

1. Admiral hub implementation (`ambit-inbound-hub` is out of this repo)
2. Putting `InboundSecret` on the wake body
3. Expanding fuller-channel [03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md)
4. Cursor SSE changes
5. Done-seam redesign — empty `text` on deliver is already the live oneshot terminus

## Ops checklist

1. Azure build has the deliver door live
2. Hub holds `InboundSecret` and POSTs with `X-Ambit-Inbound-Secret`
3. Hub POSTs text chunks, then empty `text` for oneshot Done
4. Hub reads `responseUrl` from the wake body

## See also

[map.md](../map.md), [arch.md](../arch.md), [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md), [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md), [25 — Grok Bot wake auth Bearer](../../llm-connector/issues/25-grokbot-wake-auth-bearer.md), [GrokBotHttp](../../../src/CloudAgents/Internal/GrokBotHttp.fs), [GrokBotRunner.deliver](../../../src/CloudAgents/GrokBotRunner.fs)

## Comments

- 2026-09-25 — Filed. Alan lock: oneshot `?ai gbot` replies under Focus when Ambit runs on Azure. Outbound wake carries an absolute response URL so the hub can POST to `/ambit/actors/deliver`. Prior Done-seam research ticket superseded (empty-text Done is live). Status `defined`.
- 2026-09-25 — Alan locked Definition of done: Azure `?ai gbot` → grokbot has deliver info → Focus nodes via `/deliver` → CloudAgents grokbot handlers → Actor Changes. Hub POST is an ops dependency. [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) and [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md) stay `done`. Leave [03 — gbot Run Agent: wake + inbox → Focus stream](03-gbot-wake-inbox-focus.md) alone.

## Time

- 2026-09-25 45m — File wake response-URL ticket; remap plan pointers; settle empty-text Done (from chat)
- 2026-09-25 10m — Add Alan Definition of done (from chat)
