# bot-channel architecture

Spec: [[spec.md]]
Updated: 2026-09-25
Sequence: module-build

Sources: [map.md](map.md) Decisions (arch grill 2026-09-24; Alan lock 2026-09-24/25 remap; keep-open lock 2026-09-25); form example [plan/llm-connector/arch.md](../llm-connector/arch.md); Focus stream helpers from [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) (`done`) / `FocusXmlStream`. Checklist: `[x]` already true of the codebase shape; `[ ]` still to build. Oneshot library [24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) (`done`) plus Actor wiring [05 — Run Agent Actor Grok Bot oneshot](issues/05-run-agent-grokbot-oneshot.md) are shipped. Current increment is [08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md). Tickets [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md) stay the eventual fuller channel.

## 1. Story paths

**First-slice cut (implement now):** `?ai gbot` on existing [src/Server/RunAgentActor.fs](../../src/Server/RunAgentActor.fs) uses `GrokBotRunner` (wake + `streamUntilComplete` + cancel). Shared FocusXmlStream fold stays one path with cursor `AgentRunner`. Empty-text Done flushes the turn and does **not** Finish the Actor ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md)). Composition binds `GrokBotConfig` from `grokbot:WakeUrl` / `WakeSecret` / `InboundSecret`. No Server inbox loop, no WakeHttp Actor path, no second Actor name. Proofs use `GrokBotRunner.setFake` / `setFakeStream`.

**Eventual (do not implement now):** paths **Run gbot wake (eventual)**, **Inbound deliver (eventual)**, **Focus stream from inbox (eventual)**, and TestActor `?test gbot` simulation. Those stay [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md).

1. **Run gbot wake (eventual channel)**
   1. [ ] Browser Run Command text `?ai gbot` (extra tokens ignored) on the existing typed ActorStart path
   2. [ ] CoreActorPool admits Focus exclusivity; rejects second start while that `commandId` already has a live row
   3. [ ] CoreActorPool mints `sessionId` on the live row beside `commandId`, `focusId`, and Actor secret
   4. [ ] Run Agent Actor selects the **gbot** function (Actor name still `ai`)
   5. [ ] gbot packs Focus extract like cursor (`AiExtractPack`)
   6. [ ] WakeHttp POSTs ack-only wake with pack + `commandId` + `focusId` + `sessionId` + absolute `responseUrl`; auth is `Authorization: Bearer {WakeSecret}`
   7. [ ] Actor stays live after wake ack (does not Finish on wake ack)

2. **Inbound deliver (eventual channel)**
   1. [ ] Bot POSTs `POST /ambit/actors/deliver` with header `X-Ambit-Inbound-Secret`
   2. [ ] InboundAuth checks secret from GrokbotConfig (`grokbot:InboundSecret`); reject unauthorized
   3. [ ] ActorsDeliverDoor decodes body `{ sessionId, text }` only
   4. [ ] CoreActorPool `deliver(sessionId, msg)` enqueues to the live Actor inbox; unknown / not live → 404
   5. [ ] Actors that do not read the inbox ignore delivered messages

3. **Focus stream from inbox (eventual channel)**
   1. [ ] gbot Actor consumes inbox messages while live
   2. [ ] Each text chunk drives FocusXmlStream pending-buffer → ordinary Core Changes under Focus (reuse [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) helpers — no second Focus-write path). Those Changes may post Append (event-sourced-ops mailbox op; `commandName` Append; end of Children only; expands to Replace in History) when that op exists — preferred over a hand-built full-list Replace for end-append. [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) is `done` (Replace-based FocusXmlStream available).
   3. [ ] Browser Poll shows Focus Children grow

4. **Cancel drops the wire (eventual channel inbox)**
   1. [ ] Browser Cancel by Focus (existing door)
   2. [ ] Core drops the live row; `sessionId` invalidated
   3. [ ] No close-notify wake; later inbound gets 404
   4. [ ] Accepted Focus Children stay; new Run on same Command mints a fresh `sessionId`

5. **Oneshot Actor wiring (first slice)**
   1. [ ] Browser Run `?ai gbot` on the existing typed ActorStart path (Actor name still `ai`)
   2. [ ] First token `gbot` selects `GrokBotRunner`; other behaviors keep Cursor `AgentRunner`
   3. [ ] Pack extract via shared `AiExtractPack`; wake + `streamUntilComplete` + FocusXmlStream fold on `AssistantText`
   4. [ ] Empty-text `RunFinished` flushes Focus and keeps `streamUntilComplete` listening ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md)); Cancel mid-stream via `GrokBotRunner.cancel`
   5. [ ] Empty `WakeUrl` fails safely without writing secrets
   6. [x] Empty inbound `text` is oneshot Done mapping (`GrokBotRunner.deliver` → `RunFinished`) — keep the mapping for turn flush; do not Finish the Actor on it
   7. [x] Wake JSON includes absolute `responseUrl` for `/ambit/actors/deliver` ([06 — Wake response URL](issues/06-wake-response-url.md))
   8. [ ] Later destination (not this slice): subsequent outbound wakes; fuller-channel `kind: close` / close-notify

6. **Secrets bind (first slice composition)**
   1. [ ] Composition binds `GrokBotConfig` from `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret` (.NET User Secrets / config)
   2. [ ] Library stays settings-blind; keys may be empty until Alan loads them; missing wake URL fails the wake call safely without writing secrets into Graph

7. **Stub or proof bot (eventual)**
   1. [ ] Optional proof under tests/proofs POSTs `{ sessionId, text }` through the inbound door and observes Focus growth

8. **Simulate gbot via TestActor (eventual / later)**
   1. [ ] Browser Run Command text `?test gbot` (optional extra tokens are extra canned texts) on the existing typed ActorStart path; Actor name `test`
   2. [ ] TestActor selects gbot-simulation behavior; `?test hello` unchanged
   3. [ ] Canned inbound `text` values enqueue via CoreActorPool `deliver` (no HTTP inbound door; no WakeHttp)
   4. [ ] Each canned text drives FocusXmlStream pending-buffer → ordinary Core Changes under Focus (reuse [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) helpers — same write path as gbot)
   5. [ ] Browser Poll shows Focus Children grow
   6. [ ] Actor Finishes after the canned stream (first-slice parity with Finish-on-response-end). This is not a keep-alive hub session.

Shared segments (first slice):
1. [x] FocusXmlStream / pending-buffer Focus writes from [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) (`done`)
2. [x] CloudAgents `GrokBotRunner` oneshot ([24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) `done`)
3. [x] Run Agent Actor gbot backend on the existing runner (wake + shared fold + Finish + Cancel)
4. [x] Existing Cancel / drop / Focus exclusivity
5. [x] Actor Finish when `RunFinished` arrives (cursor class of terminus)

Narrowest shared test seam:
1. [ ] CoreActorPool `deliver` + live-row `sessionId` / `commandId` exclusivity (no HTTP)
2. [ ] Inbound door → `deliver` with secret header (harness or test host)
3. [ ] gbot inbox → FocusXmlStream adds under Focus → Finish on response end (fake inbound; [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) is `done`)
4. [ ] TestActor `?test gbot` canned texts → `deliver` + FocusXmlStream → Finish after canned stream (no hub; Blocked-by [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md); [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) is `done`)

## 2. Module map

1. **Browser Run / Cancel**
   File: Client Run and Poll surfaces (existing Browser Command path). Shape unchanged for this Project.
   1. State
      1. [x] Session cookie / credential
      2. [x] Live Actor projection by Focus from Poll Events (core-creation chrome)
   2. Interface
      1. [x] Typed launch: included NodeIds, Zoom, Focus, Command, event id
      2. [x] Cancel by Focus NodeId
      3. [x] Poll consume Ev tail
   3. Uses
      1. [x] HTTP Adapter / RouteRegistration `/ambit/*`

2. **GrokbotConfig**
   File: new Server bind module (same pattern as [AiKeys](../../src/Server/AiKeys.fs) / `IConfiguration`).
   1. State
      1. [ ] Bound values: `WakeUrl`, `WakeSecret`, `InboundSecret` (strings; may be empty)
   2. Interface
      1. [ ] `fromConfig: IConfiguration -> GrokbotConfig` reading `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret` via .NET User Secrets (and ordinary config overlay)
      2. [ ] No Graph persistence of secrets
   3. Uses
      1. [ ] `IConfiguration` / User Secrets (composition)

3. **WakeHttp**
   File: new Server outbound helper used by the gbot function.
   1. State
      1. [ ] None durable
   2. Interface
      1. [ ] `postWake: GrokbotConfig * wakeBody -> Async<Result<unit, string>>` — HTTP POST; treat response as ack only (ignore reply body as bot text); wakeBody includes absolute `responseUrl` ([06 — Wake response URL](issues/06-wake-response-url.md))
      2. [ ] Auth header is `Authorization: Bearer {WakeSecret}` (Admiral hub panel; [25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md)). Empty or whitespace secret adds no header; caller fails closed. Do not invent `X-Ambit-Wake-Secret`.
      3. [ ] Empty `WakeUrl` → safe domain error (no Graph write of secrets)
   3. Uses
      1. [ ] `HttpClient` (or existing Server HTTP helper)
      2. [ ] GrokbotConfig

4. **InboundAuth + ActorsDeliverDoor**
   File: [[src/Server/RouteRegistration.fs]] (new `POST /ambit/actors/deliver` beside existing `/ambit/*` doors).
   1. State
      1. [ ] None beyond request decode
   2. Interface
      1. [ ] Require header `X-Ambit-Inbound-Secret` equal to configured `InboundSecret` (reject when missing/wrong; empty configured secret fails closed)
      2. [ ] Decode JSON body with exactly `sessionId` + `text` (no optional `commandId`)
      3. [ ] Call `pool.deliver(sessionId, text)`; map not-live / unknown → HTTP 404; success → ack
      4. [ ] Door does not write Graph; does not Finish Actors
   3. Uses
      1. [ ] GrokbotConfig
      2. [ ] CoreActorPool `deliver` (via CoreMailbox / CoreRuntime door as composition already exposes pool)

5. **CoreActorPool**
   File: [[src/Server/Core/CoreActorPool.fs]] (extend live row + interface).
   1. State
      1. [x] Live registry keyed by Actor `Credential` secret; Focus exclusivity
      2. [ ] Live row also holds `commandId`, `sessionId` (Guid string), and an inbox queue of inbound texts
      3. [ ] Secondary index: `sessionId` → live Credential (for deliver)
   2. Interface
      1. [x] `startActor` / `schedule` / `drop` / `finish` / Focus helpers (existing)
      2. [ ] On start: mint `sessionId`; store `commandId` from ActorStart; reject if any live row already has that `commandId`
      3. [ ] Keep existing Focus exclusivity (`focus already has a live Actor`)
      4. [ ] `deliver: sessionId * text -> Result<unit, string>` — enqueue to that live Actor’s inbox; Error when unknown / not live
      5. [ ] Inbox readable by the scheduled Actor body (opt-in consume); Actors that never read ignore messages
      6. [ ] Drop / Cancel / Finish clears `sessionId` index and inbox
   3. Uses
      1. [x] PersistHandlers / EventLog via CoreMailbox
      2. [x] Registered Actor definitions (composition)

6. **Run Agent Actor — gbot function**
   File: [[src/Server/RunAgentActor.fs]] (branch inside existing `ai` ActorFn; do not register a second Actor name).
   1. State
      1. [x] Cursor path job memory (CloudAgents) — unchanged
      2. [ ] First slice: oneshot `GrokBotRunner` wake + stream on the same Actor; destination later consumes inbox until Cancel/drop
   2. Interface
      1. [ ] Parse Command behavior: first token `gbot` selects `GrokBotRunner`; further tokens ignored in first slice; other behaviors keep Cursor `AgentRunner`
      2. [ ] On start: pack Focus extract (`AiExtractPack` shared with cursor); `GrokBotRunner.wake` with three ids plus absolute `responseUrl`; `streamUntilComplete` + shared FocusXmlStream fold
      3. [ ] Current increment: empty-text `RunFinished` flushes and does not Finish ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md)). Wake JSON includes absolute `responseUrl` ([06 — Wake response URL](issues/06-wake-response-url.md)). Same `sessionId` stays live for later deliver texts
      4. [ ] On Cancel token / drop: `GrokBotRunner.cancel` mid-stream; no close-notify wake
      5. [ ] Never expose a bot Graph write API; never Finish solely because wake acked
      6. [ ] Later destination (not this slice): inbox loop via [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md)
   3. Uses
      1. [ ] AiExtractPack (shared)
      2. [ ] `GrokBotRunner` + composition-bound `GrokBotConfig`
      3. [ ] FocusXmlStream / pending-buffer from [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md)
      4. [ ] CoreMailbox postEvents / actorStop

7. **StubOrProofBot** (optional)
   File: under `tests/` or `proofs/` (not production Server).
   1. State
      1. [ ] None durable
   2. Interface
      1. [ ] POST `{ sessionId, text }` to `/ambit/actors/deliver` with `X-Ambit-Inbound-Secret`
      2. [ ] Enough to prove Focus Children grow end-to-end
   3. Uses
      1. [ ] Inbound door; test host or live Server

8. **TestActor**
   File: [[src/Server/TestActor.fs]] (existing; Actor name `test`). Not a second product Actor and not a Shared module.
   1. State
      1. [x] None durable (hello path)
      2. [ ] gbot-sim: canned inbound texts consumed through the live-row inbox until applied, then Finish
   2. Interface
      1. [x] Interpret `?test hello` → post one Owned child `hello` then ActorStop
      2. [ ] First behavior token `gbot` selects simulation; further tokens are extra canned texts; with no extra tokens use a short fixed canned sequence
      3. [ ] Simulated: no WakeHttp, no Admiral hub, no live Grok Bot, no inbound HTTP
      4. [ ] Real: enqueue canned texts via CoreActorPool `deliver` ([01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)); consume inbox through FocusXmlStream pending-buffer ([18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md)); ordinary Core Changes under Focus
      5. [ ] Finish after the canned stream (first-slice parity). This is not a keep-alive hub session
      6. [ ] Unknown `?test` behaviors stay ActorFailed; hello path unchanged
   3. Uses
      1. [x] CoreMailbox postEvents / actorStop
      2. [ ] CoreActorPool `deliver` + inbox (via [01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md))
      3. [ ] FocusXmlStream / pending-buffer from [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md)

## 3. Seams

1. [x] **Browser ↔ RouteRegistration** — existing typed launch / Cancel / Poll (`/ambit/*`)
2. [ ] **ActorsDeliverDoor ↔ CoreActorPool.deliver** — sole inbound bot→Actor door; secret checked before deliver
3. [ ] **GrokBotConfig ↔ User Secrets** — `grokbot:*` bind at composition; library stays settings-blind. Eventual inbound door reads the same config later
4. [x] **GrokBotRunner ↔ Admiral hub / bot webhook** — outbound ack-only in CloudAgents ([24 — CloudAgents Grok Bot oneshot stream](../llm-connector/issues/24-cloudagents-grokbot-oneshot.md) `done`); wake auth `Authorization: Bearer {WakeSecret}` ([25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md)). Absolute `responseUrl` on that JSON is [06 — Wake response URL](issues/06-wake-response-url.md).
5. [ ] **Run Agent Actor ↔ GrokBotRunner** — first token `gbot` uses wake + shared FocusXmlStream fold; empty-text `RunFinished` flushes and keeps listening ([08 — gbot keep-open listening](issues/08-gbot-keep-open-listening.md))
6. [ ] **gbot function ↔ FocusXmlStream** — Interface on **Run Agent Actor — gbot function**; reuses [18 — AI Actor stream](../llm-connector/issues/18-ai-actor-stream.md) helpers (`done`); one fold for both backends
7. [x] **CoreActorPool ↔ Focus exclusivity** — existing admit; this Project adds `commandId` exclusivity beside it
8. [ ] **TestActor ↔ deliver + FocusXmlStream** — Interface on **TestActor**; primary deterministic seam (no hub)

## 4. Alternative considered

1. **gbot-named inbound door** (`POST /ambit/gbot/deliver` or similar) — Rejected: deliver is generalized for any live Actor; path stays `/ambit/actors/deliver` so the door is Actor-pool shaped, not bot-branded.
2. **CloudAgents stretch of Cursor `AgentRunner`** — Rejected: destination keep-alive stays a webhook + inbox wire ([01 — CoreActorPool sessionId + deliver + commandId exclusivity](issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](issues/03-gbot-wake-inbox-focus.md)). First slice uses sibling `GrokBotRunner` (not stretched Cursor `AgentRunner`) wired on the existing Run Agent Actor. Cursor path stays owned by llm-connector; gbot shares extract/Focus helpers only.
3. **Direct Graph write API for the bot** — Rejected: would bypass Actor-mediated Changes, Poll, Cancel, and History; map Non-goal and spec Problem 3.
4. **Invent Ambit-only wake auth header** — Rejected: wake must match the existing Admiral hub / bot webhook contract so Alan’s hub panel credentials work. Locked 2026-09-25 as `Authorization: Bearer {WakeSecret}` ([25 — Grok Bot wake auth Bearer](../llm-connector/issues/25-grokbot-wake-auth-bearer.md)). Do not send `X-Ambit-Wake-Secret`.
5. **Bot `kind: close` as keep-alive close** — Deferred as the fuller-channel close decision (destination still planned). First-slice terminus is response concluded → Actor Finish. Oneshot Done is empty `text` on deliver (settled). `kind: close` stays later. Cancel/drop still ends the wire mid-stream; bot learns via 404. Close-notify from Ambit stays deferred.
6. **New command or second Actor for gbot proof** — Rejected: extend existing TestActor / `?test` behavior token `gbot`; do not invent a second product path.

## 5. Unsettled

1. **Azure Key Vault vs User Secrets packaging** — First slice binds via User Secrets for localhost and Azure alike; any Key Vault packaging detail beyond that bind is deferred if needed.

Settled 2026-09-25: oneshot Done is empty `text` on deliver (`GrokBotRunner.deliver` → `RunFinished`). Prior research ticket **06 — Done seam for response concluded** is superseded. Next lock is [06 — Wake response URL](issues/06-wake-response-url.md) (absolute `responseUrl` on outbound wake).
