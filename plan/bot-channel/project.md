# bot-channel

Stage: chart
Summary: Ambit is a Slack-like direct-message channel to Grok Bots — message in, message out, threads — with Ambit itself as the channel (not Slack in the middle).
Updated: 2026-09-23

## Objective

Make Ambit the durable messaging surface for talking to Grok Bots. Cognition stays in Ambit’s folding UI. Git/Origin stays the shared backend for files and docs. Wake a bot with an event-oriented HTTP POST; bots write durable replies back through an Ambit write/event API that is thread- and correlation-aware.

## Epic home

- [[plan/roadmap/epics/operate-connected-channels.md]] — Chapter [[plan/roadmap/epics/chapters/ambit-as-bot-dm-channel.md]]
- Cross-cut: [[plan/transport-layer/map.md]] (inbound wake + outbound reply as a connector leg)
- Related but distinct: [[plan/llm-connector/project.md]] owns `?ai` / CloudAgents Run Agent; this Project owns Ambit ↔ Grok Bot webhook messaging

## Out of scope (this Project)

- Slack as document or store of record (Slack may stay an optional human side-channel later)
- MCP wrap of reply/query verbs (later optional)
- Replacing Run Agent / CloudAgents (`?ai`)
- Hosting bot cognition outside Ambit’s folding UI
- Admiral webhook hub implementation (already stood up: `ambit-inbound-hub`); Ambit only configures URL/key that Alan copies from Admiral’s routine panel

## Notes

- 2026-09-23 — Charted from Nectar + Alan: settle wake POST, reply write API, messaging shape, Origin boundary, first vertical slice. Credentials are not blocked on Nectar; Alan wires webhook URL/key from Admiral when config lands.
