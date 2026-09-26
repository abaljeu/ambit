# bot-channel

Labels: wayfinder:map

## Destination

A person runs `?ai gbot` under a Focus. Ambit wakes a Grok Bot over a webhook (ack-only). The bot posts durable text back into Ambit; a live Actor applies it under Focus with the same incremental-append protocol as Cursor streaming. Files and docs stay on Origin/git.

The destination stays a keep-alive chat wire: live until Cancel/drop; multi-turn without Finish-on-every-reply; subsequent outbound wakes while the session is live; later `kind: close` / close-notify decisions for that fuller channel. The first slice is an easier Cursor-Cloud-like job so Done / chrome / Finish match `?ai` cursor. That slice does not cancel the destination.

## Goal

- **Wake (Ambit → bot):** `?ai gbot` POSTs an ack-only webhook with Command/Focus/session ids + Focus extract. Destination: keep-alive Run Agent path. First slice: one wake = one job for a single user message / extract (functionally like Cursor Cloud).
- **Reply (bot → Ambit):** authenticated inbound POST → `pool.deliver(sessionId, …)` → live Actor inbox → Actor writes Focus children (FocusXmlStream / [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md)). Not a direct Graph write API for the bot.
- **Terminus:** Current increment ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md)): after the first reply stream, empty-text oneshot Done does **not** Finish the Actor. The same `sessionId` stays live and later deliver texts fold into Focus until Cancel or drop. Oneshot baseline ([04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md) / [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md)) still maps empty `text` to `RunFinished` for turn flush. Destination later: subsequent outbound wakes while live; `kind: close` / close-notify.

## Non-goals

- Slack as the channel or store of record
- MCP as the first transport (optional later)
- Replacing cursor `?ai` / CloudAgents job runner
- Persistent secrets product design beyond first-slice User Secrets bind
- Memory policy (clean vs continue chat context) — deferred; do not design in this amend
- Implementing Admiral’s `ambit-inbound-hub`

Later tickets (destination; not canceled): subsequent outbound wakes while a session is live; keep-alive multi-turn without Finish-on-every-reply; `kind: close` / close-notify decisions for that fuller channel. Close-notify from Ambit stays deferred in the first slice. Oneshot Done is empty `text` on deliver.

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
5. POST wake (ack-only) with Focus extract **same pack as `?ai cursor`**, plus `commandId`, `focusId`, `sessionId`, and an absolute `responseUrl` for `POST /ambit/actors/deliver` ([06 — Wake response URL](issues/06-wake-response-url.md)). Wake auth is `Authorization: Bearer {WakeSecret}` (Admiral hub panel). Do not send `X-Ambit-*` on wake. Do not put `InboundSecret` on the wake body. Inbound stays `X-Ambit-Inbound-Secret`.
6. Do not Finish on wake ack. Current increment: do not Finish on empty-text Done after the first reply ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md)). Cancel still drops mid-stream. No idle timeout. Later destination: subsequent outbound wakes; `kind: close` / close-notify.

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
  "responseUrl": "https://collaborative-systems.org/ambit/actors/deliver",
  "payload": {}
}
```

`text` / pack body follows the cursor Focus-extract path on the first wake. `responseUrl` is the absolute deliver door so the hub can POST reply text ([06 — Wake response URL](issues/06-wake-response-url.md)). Later-turn outbound shape is a later ticket.

## Inbound path (bot → Ambit → Actor)

1. `POST /ambit/actors/deliver` with header **`X-Ambit-Inbound-Secret`** (TLS; easy auth is enough).
2. Body is exactly `sessionId` + `text` (no optional `commandId`).
3. Api → `pool.deliver(sessionId, msg)` — **generalized** for any live Actor; Actors that do not read the inbox ignore it.
4. No live session → 404. Close-notify from Ambit stays deferred (bot learns via 404). That deferral does not cancel later close-notify decisions for the keep-alive channel.
5. gbot Actor consumes inbox and writes under Focus via **Cursor streaming incremental-append protocol** (llm-connector [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) is `done` / FocusXmlStream helpers — no second Focus-write path). Current increment: empty `text` on deliver still maps to `RunFinished` for turn flush, but does not Finish the Actor or clear `sessionId` ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md)). Later non-empty deliver texts keep growing Focus. Cancel / drop still end the wire.

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

Oneshot = library [24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) (`done`) + Actor wiring [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md).

1. `?ai gbot` → existing Run Agent Actor (`ai`); first token `gbot` uses `GrokBotRunner` (wake + `streamUntilComplete` + cancel).
2. Shared FocusXmlStream fold on `AssistantText`; Finish on `RunFinished`; Cancel mid-stream. Cursor `AgentRunner` path unchanged.
3. Composition binds `GrokBotConfig` from `grokbot:WakeUrl` / `WakeSecret` / `InboundSecret`. Library stays settings-blind.
4. Proofs use `GrokBotRunner.setFake` / `setFakeStream`. Empty `WakeUrl` fails safely. Wake must carry absolute `responseUrl` ([06 — Wake response URL](issues/06-wake-response-url.md)).
5. Keep-open increment ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md)): empty-text Done does not Finish; same `sessionId` stays live for later deliver texts.

Eventual (do not implement now; do not delete): [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md) pool `sessionId`+deliver, [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](issues/02-inbound-actors-deliver-door.md) inbound door, [03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) fuller wake+inbox wire. Also later: TestActor `?test gbot` simulation; subsequent outbound wakes; close-notify; fuller-channel `kind: close`. Also out of first slice: MCP, Slack, attachment UX, HMAC, idle timeout, multi-bot routing UI, memory policy.

## Decisions so far

- 2026-09-25 — Alan lock: [08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md) — after the first reply, do not Finish the gbot Actor on empty-text oneshot Done. Same `sessionId` stays live; later `POST /ambit/actors/deliver` texts fold into Focus until Cancel or drop. Extends [04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md) / [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md). Do not expand [03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md). Do not touch wake/auth/`responseUrl` or proxy allowlist.
- 2026-09-25 — Alan lock: remap [06 — Wake response URL](issues/06-wake-response-url.md) to outbound wake `responseUrl` (absolute `POST /ambit/actors/deliver`) so oneshot `?ai gbot` replies work when Ambit runs on Azure. Do not put `InboundSecret` on the wake body. Do not expand [03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md). Prior research ticket **06 — Done seam for response concluded** is superseded. Empty `text` on deliver is the live oneshot Done terminus (retired from Unsettled; [08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md) keeps that mapping for turn flush and stops using it as Actor Finish). Definition of done: Azure `?ai gbot` → grokbot can POST deliver → Focus nodes via `/deliver` → CloudAgents grokbot handlers → Actor Changes. Hub POST is an ops dependency.
- 2026-09-25 — Alan confirmed from the hub panel: wake auth is `Authorization: Bearer {WakeSecret}`. Settled on [25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md). Do not invent `X-Ambit-Wake-Secret`. Inbound stays `X-Ambit-Inbound-Secret`.
- 2026-09-24/25 — Alan lock (remap + implement [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md)): first slice is oneshot library [24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) plus Actor wiring [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md). Tickets [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) stay the eventual channel target (pool sessionId+deliver, inbound door, fuller wake+inbox). Do not implement [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) now. [04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md) remaps to the CloudAgents oneshot library (`done`, pointer to [24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md)). Old TestActor `?test gbot` simulation was a mis-slot — later, under [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md). Then-ticket **06 — Done seam for response concluded** stayed Unsettled under the Grok adapter (superseded 2026-09-25; see [06 — Wake response URL](issues/06-wake-response-url.md)). Prefer file pointers as code spans. No ticket numbers in module filenames.
- 2026-09-24/25 — Intermediate lock (Alan): first slice is Cursor-Cloud-like. One wake = one job for a single user message / extract. Bot streams into Ambit (FocusXmlStream / [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md)). When that response message concludes, the Actor Finishes (same class of terminus as CloudAgents RunFinished on `?ai` cursor). Do not keep a long-lived live wire for more queries in this slice; next query is a new Run / new `sessionId`. Cancel still drops mid-stream. Memory policy (clean vs continue chat context) is out of scope — do not design here. This **amends** prior first-slice close locks that said live until Cancel/drop only, no bot `kind: close` / no Done, Actor stays live after wake ack forever. Those remain the **destination / later tickets**: keep-alive chat wire until Cancel/drop; multi-turn without Finish-on-every-reply; subsequent outbound wakes while live; `kind: close` / close-notify decisions for that fuller channel. Close-notify from Ambit stays deferred. Exact wire signal for response-concluded was Unsettled in this lock; settled 2026-09-25 as empty `text` on deliver (see lock above). Product behavior is Finish-on-response-end like cursor. [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) is `done` on staging; FocusXmlStream reuse is unblocked.
- 2026-09-24 — Streaming Actors including gbot **may** use mailbox Change op **Append** (from [event-sourced-ops](../event-sourced-ops/project.md): expands to Replace in History; `commandName` Append; end of Children only). Preferred over a hand-built full-list Replace for end-append when that op exists. First-slice tickets are not blocked on Append if llm-connector [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) ships Replace-based FocusXmlStream.
- 2026-09-24 — Alan accepted [[arch.md]] as shared understanding. Locked: `?test` gains a deterministic gbot-simulation case on existing TestActor (Actor name `test`). Canned texts grow Focus the way inbound deliver + FocusXmlStream would, so Server/Browser proofs do not need the Admiral hub or a live Grok Bot. Optional StubOrProofBot HTTP proof stays; `?test` is the primary deterministic seam. Do not invent a second product command or Actor name. Amended 2026-09-24/25: the simulation Finishes after the canned stream (first-slice parity).
- 2026-09-24 — Arch grill locks (Alan confirmed shared understanding for first slice):
  - **Secrets:** .NET User Secrets for localhost and Azure alike; provisional keys `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret`; values may be empty until Alan loads them.
  - **Close (amended 2026-09-24/25):** first slice = Finish-on-response-end (see lock above). Prior wording “Cancel/drop only; no bot `kind: close`” is no longer the first-slice close policy. Destination later still plans keep-alive until Cancel/drop and later `kind: close` / close-notify decisions.
  - **Inbound:** `POST /ambit/actors/deliver` + header `X-Ambit-Inbound-Secret`; body exactly `sessionId` + `text` (no optional `commandId`).
  - **Wake auth:** `Authorization: Bearer {WakeSecret}` (Alan confirmed from the hub panel 2026-09-25; [25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md)). Do not invent an Ambit wake header name.
  - **Focus writes:** reuse pending-buffer / FocusXmlStream helpers from llm-connector [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) (`done` on staging) — no second Focus-write path. Tickets that listed Blocked-by [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) are unblocked on that edge.
  - **Three-ticket cut (module-build):** (1) pool `sessionId` + deliver + `commandId` exclusivity; (2) inbound door + secret + config bind; (3) gbot Actor wake + inbox → Focus. TestActor `?test gbot` simulation is a later lock on the same arch — not a fourth product module (see eventual [03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md)). Not current [04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md).
- 2026-09-24 — Grill locked (Alan confirmed shared understanding):
  - Command `?ai gbot`; gbot = separate function inside Run Agent Actor with shared cursor helpers.
  - `commandId` / `focusId` / `sessionId` split; deliver by `sessionId`; reject second start while `commandId` live.
  - Wake #1 = Focus extract like cursor; subsequent outbound postponed.
  - Inbound = shared-secret header → generalized `pool.deliver`; Actor-mediated Focus streaming writes.
  - Live until Cancel / drop (destination; first-slice terminus amended 2026-09-24/25 to Finish-on-response-end); no close-notify (404 later; close-notify remains a later fuller-channel decision).
  - Wake URL + channel secret → persistent secrets strategy (settled as User Secrets for first slice).
- 2026-09-23 — Ambit is the channel; Slack out as store of record; MCP later; Admiral hub already exists.

## Implementation

Architecture: [arch.md](arch.md). Sequence `module-build`. Tickets:

1. [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md) — Status `defined` (eventual / deferred)
2. [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](issues/02-inbound-actors-deliver-door.md) — Status `defined` (eventual / deferred)
3. [03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) — Status `defined` (eventual / deferred; Blocked-by [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md) and [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](issues/02-inbound-actors-deliver-door.md))
4. [04 — CloudAgents Grok Bot oneshot library](issues/04-cloudagents-grokbot-oneshot.md) — Status `done` (pointer to [24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md))
5. [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md) — Status `done`
6. [06 — Wake response URL](issues/06-wake-response-url.md) — Status `done` (absolute `responseUrl` on outbound wake)
7. [25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md) — Status `coded`. Settles wake auth as `Authorization: Bearer {WakeSecret}`.
8. [08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md) — Status `done`. After first reply, empty-text Done does not Finish; same `sessionId` stays live.

## Not yet specified

- Multi-bot `routeHint` / options on `?ai gbot`
- Attachment pointers
- Azure Key Vault vs User Secrets packaging details if any (see [[arch.md]] Unsettled)
- Memory policy (clean vs continue chat context)

## Out of scope

- Slack document store; MCP-first transport; Cursor job-runner replacement; Admiral hub impl; persistent secrets product design beyond User Secrets bind; memory policy this amend
- Not out of scope (later / destination): subsequent outbound wakes; close-notify; fuller-channel `kind: close`. Keep-open after first reply is [08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md).
