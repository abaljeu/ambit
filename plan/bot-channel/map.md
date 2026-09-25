# bot-channel

Labels: wayfinder:map

## Destination

A person runs `?ai gbot` under a Focus. Ambit wakes a Grok Bot over a webhook (ack-only). The bot posts durable text back into Ambit; a live Actor applies it under Focus with the same incremental-append protocol as Cursor streaming. Files and docs stay on Origin/git.

The destination stays a keep-alive chat wire: live until Cancel/drop; multi-turn without Finish-on-every-reply; subsequent outbound wakes while the session is live; later `kind: close` / close-notify decisions for that fuller channel. The first slice is an easier Cursor-Cloud-like job so Done / chrome / Finish match `?ai` cursor. That slice does not cancel the destination.

## Goal

- **Wake (Ambit → bot):** `?ai gbot` POSTs an ack-only webhook with Command/Focus/session ids + Focus extract. Destination: keep-alive Run Agent path. First slice: one wake = one job for a single user message / extract (functionally like Cursor Cloud).
- **Reply (bot → Ambit):** authenticated inbound POST → `pool.deliver(sessionId, …)` → live Actor inbox → Actor writes Focus children (FocusXmlStream / [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]). Not a direct Graph write API for the bot.
- **Terminus:** First slice: when that response message concludes, the Actor Finishes (same class as CloudAgents RunFinished on `?ai` cursor). Next query is a new Run / new `sessionId`. Cancel still drops mid-stream. Destination later: no Finish-on-every-reply; the session stays live until Cancel/drop (or later close decisions).

## Non-goals

- Slack as the channel or store of record
- MCP as the first transport (optional later)
- Replacing cursor `?ai` / CloudAgents job runner
- Persistent secrets product design beyond first-slice User Secrets bind
- Memory policy (clean vs continue chat context) — deferred; do not design in this amend
- Implementing Admiral’s `ambit-inbound-hub`

Later tickets (destination; not canceled): subsequent outbound wakes while a session is live; keep-alive multi-turn without Finish-on-every-reply; `kind: close` / close-notify decisions for that fuller channel. Close-notify from Ambit stays deferred in the first slice. `kind: close` may be the first-slice “response concluded” inbound signal (Unsettled).

## Ids

| Id | Role |
| --- | --- |
| `commandId` | Graph conversation address (stable `?ai gbot` Command node) |
| `focusId` | Reply parent for this turn’s Focus writes |
| `sessionId` | Live wire — new Guid per Actor start; bot must echo on inbound |

Reject a second Actor start while that `commandId` already has a live row. After Cancel / Finish / drop, a new Run may mint a new `sessionId`. First slice: response concluded → Actor Finish → that `sessionId` dies; next query is a new Run / new `sessionId`. Later keep-alive: the same `sessionId` may span turns until Cancel/drop (or later close decisions).

## Wake path (Ambit → bot)

1. Browser Run on `?ai gbot` (options ignored for now) → `ActorStart` as today.
2. Pool selects Run Agent Actor; **gbot** is a separate function in that module with shared helpers vs cursor.
3. Live row stores `commandId`, `focusId`, `sessionId` (and Actor secret). Index deliver by **`sessionId`**.
4. Resolve wake URL + secret from .NET User Secrets (`grokbot:WakeUrl`, `grokbot:WakeSecret`; may be empty until Alan loads them).
5. POST wake (ack-only) with Focus extract **same pack as `?ai cursor`**, plus `commandId`, `focusId`, `sessionId`. Wake auth matches the Admiral hub / bot webhook contract (Ambit adapter) — do not invent an Ambit wake header name.
6. Do not Finish on wake ack. First slice: Actor Finishes when the response message concludes (Cursor Cloud class of terminus). Cancel still drops mid-stream. No idle timeout. Later destination: Actor stays live until Cancel/drop without Finish-on-every-reply; `kind: close` / close-notify remain later decisions for that fuller channel.

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
4. No live session → 404. Close-notify from Ambit stays deferred (bot learns via 404). That deferral does not cancel later close-notify decisions for the keep-alive channel.
5. gbot Actor consumes inbox and writes under Focus via **Cursor streaming incremental-append protocol** (llm-connector [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done` / FocusXmlStream helpers — no second Focus-write path). First slice: when that response concludes, the Actor Finishes. Exact wire signal for “response concluded” is Unsettled (`kind: close` may be that bot→Ambit signal, or harness Done / explicit inbound kind / empty sentinel). Product behavior is Finish-on-response-end like cursor.

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
3. Wake POST ack-only with cursor-like Focus extract + ids (User Secrets bind). One wake = one job for a single user message / extract.
4. Bot (stub or real) posts `{ sessionId, text }`; Actor incremental-appends under Focus via FocusXmlStream from [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] (`done`).
5. When that response concludes, the Actor Finishes (same class as cursor RunFinished). Cancel still drops mid-stream. Next query = new Run / new `sessionId`. Second start while live rejected.

Out of first slice (later / destination — not deleted): keep-alive live wire until Cancel/drop; multi-turn without Finish-on-every-reply; subsequent outbound wakes while live; close-notify; fuller-channel `kind: close` decisions (except possibly as the Unsettled Done seam). Also out of first slice: MCP, Slack, attachment UX, HMAC, idle timeout, multi-bot routing UI, memory policy.

## Decisions so far

- 2026-09-24/25 — Intermediate lock (Alan): first slice is Cursor-Cloud-like. One wake = one job for a single user message / extract. Bot streams into Ambit (FocusXmlStream / [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]). When that response message concludes, the Actor Finishes (same class of terminus as CloudAgents RunFinished on `?ai` cursor). Do not keep a long-lived live wire for more queries in this slice; next query is a new Run / new `sessionId`. Cancel still drops mid-stream. Memory policy (clean vs continue chat context) is out of scope — do not design here. This **amends** prior first-slice close locks that said live until Cancel/drop only, no bot `kind: close` / no Done, Actor stays live after wake ack forever. Those remain the **destination / later tickets**: keep-alive chat wire until Cancel/drop; multi-turn without Finish-on-every-reply; subsequent outbound wakes while live; `kind: close` / close-notify decisions for that fuller channel. Close-notify from Ambit stays deferred. Exact wire signal for response-concluded is Unsettled (`kind: close` may be that inbound signal, or harness Done / explicit inbound kind / empty sentinel). Product behavior is Finish-on-response-end like cursor. [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done` on staging; FocusXmlStream reuse is unblocked.
- 2026-09-24 — Streaming Actors including gbot **may** use mailbox Change op **Append** (from [[plan/event-sourced-ops/project.md|event-sourced-ops]]: expands to Replace in History; `commandName` Append; end of Children only). Preferred over a hand-built full-list Replace for end-append when that op exists. First-slice tickets are not blocked on Append if llm-connector [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] ships Replace-based FocusXmlStream.
- 2026-09-24 — Alan accepted [[arch.md]] as shared understanding. Locked: `?test` gains a deterministic gbot-simulation case on existing TestActor (Actor name `test`). Canned texts grow Focus the way inbound deliver + FocusXmlStream would, so Server/Browser proofs do not need the Admiral hub or a live Grok Bot. Optional StubOrProofBot HTTP proof stays; `?test` is the primary deterministic seam. Do not invent a second product command or Actor name. Amended 2026-09-24/25: the simulation Finishes after the canned stream (first-slice parity).
- 2026-09-24 — Arch grill locks (Alan confirmed shared understanding for first slice):
  - **Secrets:** .NET User Secrets for localhost and Azure alike; provisional keys `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret`; values may be empty until Alan loads them.
  - **Close (amended 2026-09-24/25):** first slice = Finish-on-response-end (see lock above). Prior wording “Cancel/drop only; no bot `kind: close`” is no longer the first-slice close policy. Destination later still plans keep-alive until Cancel/drop and later `kind: close` / close-notify decisions.
  - **Inbound:** `POST /ambit/actors/deliver` + header `X-Ambit-Inbound-Secret`; body exactly `sessionId` + `text` (no optional `commandId`).
  - **Wake auth:** match Admiral hub / bot webhook contract (Ambit adapter); do not invent an Ambit wake header name.
  - **Focus writes:** reuse pending-buffer / FocusXmlStream helpers from llm-connector [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] (`done` on staging) — no second Focus-write path. Tickets that listed Blocked-by 18 are unblocked on that edge.
  - **Three-ticket cut (module-build):** (1) pool `sessionId` + deliver + `commandId` exclusivity; (2) inbound door + secret + config bind; (3) gbot Actor wake + inbox → Focus. Ticket 04 (TestActor gbot simulation) is a later lock on the same arch — not a fourth product module.
- 2026-09-24 — Grill locked (Alan confirmed shared understanding):
  - Command `?ai gbot`; gbot = separate function inside Run Agent Actor with shared cursor helpers.
  - `commandId` / `focusId` / `sessionId` split; deliver by `sessionId`; reject second start while `commandId` live.
  - Wake #1 = Focus extract like cursor; subsequent outbound postponed.
  - Inbound = shared-secret header → generalized `pool.deliver`; Actor-mediated Focus streaming writes.
  - Live until Cancel / drop (destination; first-slice terminus amended 2026-09-24/25 to Finish-on-response-end); no close-notify (404 later; close-notify remains a later fuller-channel decision).
  - Wake URL + channel secret → persistent secrets strategy (settled as User Secrets for first slice).
- 2026-09-23 — Ambit is the channel; Slack out as store of record; MCP later; Admiral hub already exists.

## Implementation

Architecture: [[arch.md]]. Sequence `module-build`. Tickets:

1. [[issues/01-coreactorpool-sessionid-deliver.md|01 — CoreActorPool sessionId + deliver + commandId exclusivity]] — Status `defined` (frontier)
2. [[issues/02-inbound-actors-deliver-door.md|02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig]] — Status `defined` (frontier)
3. [[issues/03-gbot-wake-inbox-focus.md|03 — gbot Run Agent: wake + inbox → Focus stream]] — Status `blocked` (by 01 and 02; [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done`)
4. [[issues/04-testactor-gbot-simulation.md|04 — TestActor gbot simulation]] — Status `defined` (Blocked-by 01; 18 is `done`)

## Not yet specified

- Multi-bot `routeHint` / options on `?ai gbot`
- Attachment pointers
- Hub wake header exact name (confirm at wire time — see [[arch.md]] Unsettled)
- Azure Key Vault vs User Secrets packaging details if any (see [[arch.md]] Unsettled)
- Exact Done seam for “response concluded” (bot `kind: close` vs harness Done / explicit inbound kind / empty sentinel — see [[arch.md]] Unsettled)
- Memory policy (clean vs continue chat context)

## Out of scope

- Slack document store; MCP-first transport; Cursor job-runner replacement; Admiral hub impl; persistent secrets product design beyond User Secrets bind; memory policy this amend
- Not out of scope (later / destination): keep-alive live wire; subsequent outbound wakes; close-notify; fuller-channel `kind: close`
