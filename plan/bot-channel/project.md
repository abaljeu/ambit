# bot-channel

Stage: chart
Summary: Ambit is a Slack-like direct-message channel to Grok Bots — `?ai gbot` wake webhook, live Actor inbox deliver, Focus streaming writes — with Ambit itself as the channel.
Updated: 2026-09-24

## Objective

Make Ambit the durable messaging surface for talking to Grok Bots. Cognition stays in Ambit’s folding UI. Git/Origin stays the shared backend for files and docs. Wake a bot with an ack-only webhook POST; bots deliver text into a live Actor via `pool.deliver(sessionId)`; the Actor writes Focus with the Cursor streaming protocol.

## Epic home

- [[plan/roadmap/epics/operate-connected-channels.md]] — Chapter [[plan/roadmap/epics/chapters/ambit-as-bot-dm-channel.md]]
- Cross-cut: [[plan/transport-layer/map.md]]
- Related: [[plan/llm-connector/project.md]] owns cursor `?ai` / CloudAgents; gbot shares Run Agent Actor module helpers but is a separate function

## Out of scope (this Project)

- Slack as document or store of record
- MCP wrap of reply/query verbs
- Replacing CloudAgents job runner
- Persistent secrets-management strategy (Alan; wake URL + channel secret)
- Subsequent outbound wakes while a session is live (later ticket)
- Admiral webhook hub implementation (`ambit-inbound-hub`)

## Notes

- 2026-09-24 — Grill closed; locks in [[map.md]] Decisions. Next `/to-tickets`.
- 2026-09-23 — Charted from Nectar + Alan.
