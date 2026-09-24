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
- Persistent secrets strategy for wake URL + channel secret (separate effort Alan owns)
- Subsequent outbound wakes while a session is live (later ticket)
- Close-notify wake to the bot (bot learns via 404)
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
4. Resolve wake URL + secret from Alan’s persistent secrets strategy (temporary inject OK until that ships).
5. POST wake (ack-only) with Focus extract **same pack as `?ai cursor`**, plus `commandId`, `focusId`, `sessionId`.
6. Actor stays live until Cancel, explicit bot close, or drop — no idle timeout in first slice.

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

1. `POST` inbound door (path TBD) with **shared-secret header** (TLS; easy auth is enough — inbound only asks the Actor to insert text).
2. Body includes `sessionId` + text (and optional fields).
3. Api → `pool.deliver(sessionId, msg)` — **generalized** for any live Actor; Actors that do not read the inbox ignore it.
4. Optional defense: reject if body `commandId` disagrees with the live row.
5. No live session → 404. No close-notify from Ambit on Cancel/Finish.
6. gbot Actor consumes inbox and writes under Focus via **Cursor streaming incremental-append protocol** (llm-connector ticket 18 shape).

## Origin / secrets boundary

| Concern | Owner |
| --- | --- |
| Conversation Graph / Focus writes | Ambit Actor (this Project) |
| Files / docs | Origin / git |
| Always-on bot wake | Grok Bot webhook (Admiral hub) |
| Wake URL + channel secret | Alan’s persistent secrets strategy |
| Inbound shared secret | Ambit Server config (same secrets strategy when ready) |
| `sessionId` | Minted per live Actor — not a stored channel secret |

## First vertical slice

1. `?ai gbot` → gbot function in Run Agent Actor module; live row + `sessionId`.
2. Generalized `pool.deliver` + inbound POST with shared-secret header.
3. Wake POST ack-only with cursor-like Focus extract + ids (secret source temporary or secrets strategy).
4. Bot (stub or real) posts text with `sessionId`; Actor incremental-appends under Focus.
5. Cancel drops session; second start while live rejected.

Out of first slice: later outbound turns, close-notify, MCP, Slack, attachment UX, HMAC, idle timeout, multi-bot routing UI.

## Decisions so far

- 2026-09-24 — Grill locked (Alan confirmed shared understanding):
  - Command `?ai gbot`; gbot = separate function inside Run Agent Actor with shared cursor helpers.
  - `commandId` / `focusId` / `sessionId` split; deliver by `sessionId`; reject second start while `commandId` live.
  - Wake #1 = Focus extract like cursor; subsequent outbound postponed.
  - Inbound = shared-secret header → generalized `pool.deliver`; Actor-mediated Focus streaming writes.
  - Live until Cancel / close / drop; no close-notify (404 later).
  - Wake URL + channel secret → persistent secrets strategy (not designed here).
- 2026-09-23 — Ambit is the channel; Slack out as store of record; MCP later; Admiral hub already exists.

## Implementation

None yet — Stage `chart`. Grill closed. Next: `/to-tickets` for the first vertical slice.

## Not yet specified

- Exact inbound route path and payload schema field names
- Temporary vs secrets-strategy wiring for first-slice secrets
- Whether bot `kind: close` is in first slice or Cancel-only
- Multi-bot `routeHint` / options on `?ai gbot`
- Attachment pointers

## Out of scope

- Slack document store; MCP-first transport; Cursor job-runner replacement; Admiral hub impl; persistent secrets product design
