# bot-channel

Labels: wayfinder:map

## Destination

A person messages a Grok Bot from Ambit. The bot wakes on a webhook, thinks, and posts durable replies into the same Ambit thread. Files and docs stay on Origin/git; Ambit holds the conversation Graph.

## Goal

- **Wake (Ambit → bot):** an Ambit event-oriented path POSTs to a Grok Bot webhook routine (fast always-on; HTTP response is ack only).
- **Reply (bot → Ambit):** bots call an Ambit write/event API (thread/correlation-aware) so durable replies land in the Graph like in-thread messages. Ephemeral reasoning need not be written.
- **Shape:** messaging-channel semantics — thread id, author, text, timestamp, optional doc/attachment pointers — not a job ticket plus final report only.

## Non-goals

- Slack as the channel or store of record for this lane
- MCP as the first transport (optional later wrap)
- Replacing [[plan/llm-connector/project.md]] Run Agent
- Bot-side credential storage in Ambit plan docs (URL/key live in Admiral’s routine panel; Ambit config holds the copy Alan pastes)

## Wake path (Ambit → bot)

1. User (or Graph event) produces a message in an Ambit bot thread.
2. Ambit resolves bot route config (webhook URL + key from Server appsettings / equivalent; Alan pastes from Admiral).
3. Ambit POSTs a wake payload; response is ack only (no durable body required).
4. Admiral hub routine `ambit-inbound-hub` (webhook) receives and routes to the target bot.

### Proposed wake payload

```json
{
  "source": "ambit",
  "kind": "message",
  "sentAt": "ISO-8601",
  "correlationId": "thread-or-turn-id",
  "routeHint": "optional",
  "text": "...",
  "payload": {}
}
```

Fields may tighten in grill; `correlationId` must round-trip on reply.

## Reply API (bot → Ambit)

Bots need an authenticated Ambit write/event endpoint that:

- Accepts thread / `correlationId`, author (bot identity), text, timestamp
- Optionally accepts doc/attachment pointers (Origin paths or Ambit File Node refs)
- Appends durable message Nodes (or equivalent Changes) under the thread root
- Does not require writing ephemeral chain-of-thought

Routine prompts (bot-side, Nectar/Admiral) instruct: after webhook wake, post durable replies via this API.

## Threading

- One Ambit thread ↔ one conversation with a bot (or a stable correlation root)
- Replies nest or append under that thread using `correlationId`
- Human and bot messages share the same thread model

## Origin boundary

| Concern | Owner |
| --- | --- |
| Conversation text, thread structure | Ambit Graph (this Project) |
| Files, docs, shared repo artifacts | Origin / git (existing workspace mapping) |
| Bot always-on wake | Grok Bot webhook routine (Admiral hub) |
| Secrets for wake URL/key | Admiral panel → copied into Ambit config by Alan |

Do not store bot transcripts as the source of truth in Slack or only on the bot.

## First vertical slice

Smallest end-to-end proof:

1. Config: one bot webhook URL + key in Ambit Server settings (manual paste).
2. Ambit emits wake POST on a chosen Graph trigger (named Command or Actor — grill locks spelling).
3. Stub or real bot acks webhook and POSTs one durable reply via the new write API using the same `correlationId`.
4. Reply appears in the Ambit thread in Browser/Desktop via ordinary Poll/Changes.

Out of first slice: MCP, multi-bot routing UI, Slack side-channel, attachment upload UX, replacing `?ai`.

## Decisions so far

- 2026-09-23 — Ambit is the channel; Slack is out of scope as store of record.
- 2026-09-23 — Wake is webhook POST (ack-only); durable content returns via Ambit write API.
- 2026-09-23 — Messaging semantics (thread, author, text, time, optional pointers), not ticket+report.
- 2026-09-23 — MCP later; not first slice.
- 2026-09-23 — Do not wait on Nectar for credentials; Alan copies from Admiral `ambit-inbound-hub` panel.

## Implementation

None yet — Stage `chart`. Next: grill wake trigger spelling, write API surface, and Graph message Node shape; then `/to-tickets`.

## Not yet specified

- Exact Command / Actor / event that fires wake
- Auth scheme on the bot → Ambit write API
- Graph schema for message Nodes vs reuse of existing patterns
- Multi-bot `routeHint` resolution
- How attachment pointers are authorized and rendered

## Out of scope

- Slack document store; MCP-first transport; Run Agent replacement; implementing Admiral’s webhook hub
