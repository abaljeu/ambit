# bot-channel

Labels: wayfinder:map

## Destination

A person runs `?ai gbot` under a Focus. Ambit wakes a Grok Bot over a webhook (ack-only). The bot posts durable text back into Ambit; a live Actor applies it under Focus with the same incremental-append protocol as Cursor streaming. Files and docs stay on Origin/git.

## Goal

- **Wake (Ambit → bot):** `?ai gbot` starts a long-lived Run Agent path; POST webhook; HTTP response is ack only.
- **Reply (bot → Ambit):** authenticated inbound POST → `pool.deliver(sessionId, …)` → live Actor inbox → Actor writes Focus children (streaming protocol). Not a direct Graph write API for the bot.
- **Shape:** Command/Focus/session ids + Focus extract on first wake; not a job ticket plus final report only.

## Non-goals

- Slack as the channel or store of record
- MCP as the first transport (optional later)
- Replacing cursor `?ai` / CloudAgents job runner
- Persistent secrets product design beyond first-slice User Secrets bind
- Subsequent outbound wakes while a session is live (later ticket)
- Close-notify wake to the bot (bot learns via 404)
- Bot `kind: close` in the first slice (Cancel/drop only)
- Implementing Admiral’s `ambit-inbound-hub`

## Ids

| Id | Role |
| --- | --- |
| `commandId` | Graph conversation address (stable `?ai gbot` Command node) |
| `focusId` | Reply parent for this turn’s Focus writes |
| `sessionId` | Live wire — new Guid per Actor start; bot must echo on inbound |

Reject a second Actor start while that `commandId` already has a live row. After Cancel / Finish / drop, a new Run may mint a new `sessionId`.

## Wake path (Ambit → bot)

1. Browser Run on `?ai gbot` (options ignored for now) → `ActorStart` as today.
2. Pool selects Run Agent Actor; **gbot** is a separate function in that module with shared helpers vs cursor.
3. Live row stores `commandId`, `focusId`, `sessionId` (and Actor secret). Index deliver by **`sessionId`**.
4. Resolve wake URL + secret from .NET User Secrets (`grokbot:WakeUrl`, `grokbot:WakeSecret`; may be empty until Alan loads them).
5. POST wake (ack-only) with Focus extract **same pack as `?ai cursor`**, plus `commandId`, `focusId`, `sessionId`. Wake auth matches the Admiral hub / bot webhook contract (Ambit adapter) — do not invent an Ambit wake header name.
6. Actor stays live until Cancel or drop — no idle timeout; no bot `kind: close` in the first slice.

### Wake payload (indicative)

```json
{
  "source": "ambit",
  "kind": "message",
  "sentAt": "ISO-8601",
  "commandId": "...",
  "focusId": "...",
  "sessionId": "...",
  "text": "...",
  "payload": {}
}
```

`text` / pack body follows the cursor Focus-extract path on the first wake. Later-turn outbound shape is a later ticket.

## Inbound path (bot → Ambit → Actor)

1. `POST /ambit/actors/deliver` with header **`X-Ambit-Inbound-Secret`** (TLS; easy auth is enough).
2. Body is exactly `sessionId` + `text` (no optional `commandId`).
3. Api → `pool.deliver(sessionId, msg)` — **generalized** for any live Actor; Actors that do not read the inbox ignore it.
4. No live session → 404. No close-notify from Ambit on Cancel/Finish.
5. gbot Actor consumes inbox and writes under Focus via **Cursor streaming incremental-append protocol** (llm-connector [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] / FocusXmlStream helpers — no second Focus-write path).

## Origin / secrets boundary

| Concern | Owner |
| --- | --- |
| Conversation Graph / Focus writes | Ambit Actor (this Project) |
| Files / docs | Origin / git |
| Always-on bot wake | Grok Bot webhook (Admiral hub) |
| Wake URL + channel secret | .NET User Secrets `grokbot:WakeUrl`, `grokbot:WakeSecret` |
| Inbound shared secret | .NET User Secrets `grokbot:InboundSecret` (same bind path localhost + Azure) |
| `sessionId` | Minted per live Actor — not a stored channel secret |

## First vertical slice

1. `?ai gbot` → gbot function in Run Agent Actor module; live row + `sessionId`.
2. Generalized `pool.deliver` + inbound `POST /ambit/actors/deliver` with `X-Ambit-Inbound-Secret`.
3. Wake POST ack-only with cursor-like Focus extract + ids (User Secrets bind).
4. Bot (stub or real) posts `{ sessionId, text }`; Actor incremental-appends under Focus via FocusXmlStream from 18.
5. Cancel drops session; second start while live rejected.

Out of first slice: later outbound turns, close-notify, bot `kind: close`, MCP, Slack, attachment UX, HMAC, idle timeout, multi-bot routing UI.

## Decisions so far

- 2026-09-24 — Arch grill locks (Alan confirmed shared understanding for first slice):
  - **Secrets:** .NET User Secrets for localhost and Azure alike; provisional keys `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret`; values may be empty until Alan loads them.
  - **Close:** Cancel/drop only; no bot `kind: close` in the first slice.
  - **Inbound:** `POST /ambit/actors/deliver` + header `X-Ambit-Inbound-Secret`; body exactly `sessionId` + `text` (no optional `commandId`).
  - **Wake auth:** match Admiral hub / bot webhook contract (Ambit adapter); do not invent an Ambit wake header name.
  - **Focus writes:** coding tickets Blocked-by llm-connector [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]; reuse pending-buffer / FocusXmlStream helpers — no second Focus-write path.
  - **Three-ticket cut (module-build):** (1) pool `sessionId` + deliver + `commandId` exclusivity; (2) inbound door + secret + config bind; (3) gbot Actor wake + inbox → Focus.
- 2026-09-24 — Grill locked (Alan confirmed shared understanding):
  - Command `?ai gbot`; gbot = separate function inside Run Agent Actor with shared cursor helpers.
  - `commandId` / `focusId` / `sessionId` split; deliver by `sessionId`; reject second start while `commandId` live.
  - Wake #1 = Focus extract like cursor; subsequent outbound postponed.
  - Inbound = shared-secret header → generalized `pool.deliver`; Actor-mediated Focus streaming writes.
  - Live until Cancel / drop; no close-notify (404 later).
  - Wake URL + channel secret → persistent secrets strategy (settled as User Secrets for first slice).
- 2026-09-23 — Ambit is the channel; Slack out as store of record; MCP later; Admiral hub already exists.

## Implementation

Architecture: [[arch.md]]. Sequence `module-build`. Tickets:

1. [[issues/01-coreactorpool-sessionid-deliver.md|01 — CoreActorPool sessionId + deliver + commandId exclusivity]] — Status `defined` (frontier)
2. [[issues/02-inbound-actors-deliver-door.md|02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig]] — Status `defined` (frontier)
3. [[issues/03-gbot-wake-inbox-focus.md|03 — gbot Run Agent: wake + inbox → Focus stream]] — Status `blocked` (by 01, 02, and llm-connector 18)

## Not yet specified

- Multi-bot `routeHint` / options on `?ai gbot`
- Attachment pointers
- Hub wake header exact name (confirm at wire time — see [[arch.md]] Unsettled)
- Azure Key Vault vs User Secrets packaging details if any (see [[arch.md]] Unsettled)

## Out of scope

- Slack document store; MCP-first transport; Cursor job-runner replacement; Admiral hub impl; persistent secrets product design beyond User Secrets bind; bot `kind: close` first slice; subsequent outbound wakes; close-notify
