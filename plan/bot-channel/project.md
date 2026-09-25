# bot-channel

Stage: build
Summary: Ambit is a Slack-like direct-message channel to Grok Bots. First slice is oneshot library [24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) plus Actor wiring [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md). Next: [06 — Wake response URL](issues/06-wake-response-url.md) so Azure oneshot replies can POST back to `/ambit/actors/deliver`. Eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) keep the pool/inbox channel. Destination keep-alive wire stays planned.
Updated: 2026-09-25
Started: 2026-09-25
Actual: 5h 5m

## Objective

Make Ambit the durable messaging surface for talking to Grok Bots. Cognition stays in Ambit’s folding UI. Git/Origin stays the shared backend for files and docs. First slice: `?ai gbot` wakes via `GrokBotRunner` and streams Focus through the same FocusXmlStream fold as cursor. Eventual: bots deliver text into a live Actor via `pool.deliver(sessionId)`. Destination remains a keep-alive chat wire.

## Epic home

- [plan/roadmap/epics/operate-connected-channels.md](../roadmap/epics/operate-connected-channels.md) — Chapter [plan/roadmap/epics/chapters/ambit-as-bot-dm-channel.md](../roadmap/epics/chapters/ambit-as-bot-dm-channel.md)
- Cross-cut: [plan/transport-layer/map.md](../transport-layer/map.md)
- Related: [plan/llm-connector/project.md](../llm-connector/project.md) owns cursor `?ai` / CloudAgents; gbot shares Run Agent Actor module helpers but is a separate function

## Out of scope (this Project)

- Slack as document or store of record
- MCP wrap of reply/query verbs
- Replacing CloudAgents job runner
- Persistent secrets product design beyond first-slice User Secrets `grokbot:*`
- Subsequent outbound wakes while a session is live (later ticket; destination keep-alive)
- Fuller-channel `kind: close` / close-notify (later; oneshot Done is empty `text` on deliver)
- Memory policy (clean vs continue chat context)
- Admiral webhook hub implementation (`ambit-inbound-hub`)
- Implementing eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) in the oneshot slice

## Notes

- 2026-09-25 — Alan lock: remap [06 — Wake response URL](issues/06-wake-response-url.md) to outbound wake `responseUrl` (absolute `/ambit/actors/deliver`) so oneshot `?ai gbot` replies work when Ambit runs on Azure. Prior research ticket **06 — Done seam for response concluded** is superseded. Empty `text` on deliver is the live oneshot Done terminus. Do not put `InboundSecret` on the wake body. Do not expand [03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md). Stage stays `build`.
- 2026-09-25 — Coded [25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md): wake auth is `Authorization: Bearer {WakeSecret}`. Status `coded`. Stage stays `build`.
- 2026-09-25 — Filed [25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md): wake auth is `Authorization: Bearer {WakeSecret}`. Hub header Unsettled is settled. Status `defined`. Stage stays `build`.
- 2026-09-25 — Live Grokbot corrections: [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) Status `coded`. Wake hub header later settled on [25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md). Report [live-grokbot-corrections.md](reports/live-grokbot-corrections.md).
- 2026-09-25 — [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md) Status `done`. Alan accepted; squash-landed. Fake Grok stream / Cancel / empty WakeUrl proofs on existing Run Agent Actor.
- 2026-09-25 — Alan lock: remap tickets. [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) eventual / deferred (do not implement now). [04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md) = CloudAgents oneshot library (`done`, pointer to [24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md)). [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md) implemented on existing RunAgentActor. Then-ticket **06 — Done seam for response concluded** stayed Unsettled under the Grok adapter (superseded the same day; see [06 — Wake response URL](issues/06-wake-response-url.md)). Stage `build`.
- 2026-09-25 — Filed first-slice variant tickets (later remapped). Old [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md) Finish-on-response-end story / old TestActor simulation (not current [04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md)) superseded by the 2026-09-24/25 lock.
- 2026-09-25 — Alan locked an intermediate first slice: gbot wake/webhook is functionally like Cursor Cloud. Destination keep-alive chat wire stays planned.
- 2026-09-24 — Alan locked: streaming Actors including gbot may use mailbox Append when that op exists.
- 2026-09-24 — Alan accepted [arch.md](arch.md). `?test gbot` later / eventual, not current [04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md).
- 2026-09-24 — Arch published ([arch.md](arch.md), Sequence `module-build`); tickets [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) filed; Stage was `slice`.
- 2026-09-24 — Spec published ([spec.md](spec.md)); Stage was `spec`. Grill locks in [map.md](map.md).
- 2026-09-24 — Grill closed; locks in [map.md](map.md) Decisions.
- 2026-09-23 — Charted from Nectar + Alan.
