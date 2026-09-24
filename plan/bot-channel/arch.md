# bot-channel architecture

Spec: [[spec.md]]
Updated: 2026-09-24
Sequence: module-build

Sources: [[map.md]] Decisions (arch grill 2026-09-24); form example [[plan/llm-connector/arch.md]]; Focus stream helpers from [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] / `FocusXmlStream`. Checklist: `[x]` already true of the codebase shape; `[ ]` still to build for this Project.

## 1. Story paths

1. **Run gbot wake**
   1. [ ] Browser Run Command text `?ai gbot` (extra tokens ignored) on the existing typed ActorStart path
   2. [ ] CoreActorPool admits Focus exclusivity; rejects second start while that `commandId` already has a live row
   3. [ ] CoreActorPool mints `sessionId` on the live row beside `commandId`, `focusId`, and Actor secret
   4. [ ] Run Agent Actor selects the **gbot** function (Actor name still `ai`)
   5. [ ] gbot packs Focus extract like cursor (`AiExtractPack`)
   6. [ ] WakeHttp POSTs ack-only wake with pack + `commandId` + `focusId` + `sessionId`; auth per Admiral hub / bot webhook contract
   7. [ ] Actor stays live (does not Finish on wake ack)

2. **Inbound deliver**
   1. [ ] Bot POSTs `POST /ambit/actors/deliver` with header `X-Ambit-Inbound-Secret`
   2. [ ] InboundAuth checks secret from GrokbotConfig (`grokbot:InboundSecret`); reject unauthorized
   3. [ ] ActorsDeliverDoor decodes body `{ sessionId, text }` only
   4. [ ] CoreActorPool `deliver(sessionId, msg)` enqueues to the live Actor inbox; unknown / not live → 404
   5. [ ] Actors that do not read the inbox ignore delivered messages

3. **Focus stream from inbox**
   1. [ ] gbot Actor consumes inbox messages while live
   2. [ ] Each text chunk drives FocusXmlStream pending-buffer → ordinary Core Changes under Focus (reuse 18 helpers — no second Focus-write path)
   3. [ ] Browser Poll shows Focus Children grow

4. **Cancel drops the wire**
   1. [ ] Browser Cancel by Focus (existing door)
   2. [ ] Core drops the live row; `sessionId` invalidated
   3. [ ] No close-notify wake; later inbound gets 404
   4. [ ] Accepted Focus Children stay; new Run on same Command mints a fresh `sessionId`

5. **Secrets bind**
   1. [ ] GrokbotConfig binds `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret` from .NET User Secrets (localhost + Azure same path)
   2. [ ] Keys may be empty until Alan loads them; missing wake URL fails the wake call safely without writing secrets into Graph

6. **Stub or proof bot**
   1. [ ] Optional proof under tests/proofs POSTs `{ sessionId, text }` through the inbound door and observes Focus growth

Shared segments (paths 1–4):
1. [ ] CoreActorPool live registry with `sessionId` index + `deliver`
2. [ ] Run Agent Actor gbot function (wake + inbox loop)
3. [ ] FocusXmlStream / pending-buffer Focus writes from 18
4. [ ] Existing Cancel / drop / Focus exclusivity

Narrowest shared test seam:
1. [ ] CoreActorPool `deliver` + live-row `sessionId` / `commandId` exclusivity (no HTTP)
2. [ ] Inbound door → `deliver` with secret header (harness or test host)
3. [ ] gbot inbox → FocusXmlStream adds under Focus (fake inbound; Blocked-by 18)

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
   File: new Server bind module (same pattern as [[src/Server/AiKeys.fs|AiKeys]] / `IConfiguration`).
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
      1. [ ] `postWake: GrokbotConfig * wakeBody -> Async<Result<unit, string>>` — HTTP POST; treat response as ack only (ignore reply body as bot text)
      2. [ ] Auth header and field layout match the Admiral hub / bot webhook contract (Ambit adapter); do not invent an Ambit-only wake header name
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
      2. [ ] gbot: in-memory wake-sent flag; consumes inbox until Cancel/drop
   2. Interface
      1. [ ] Parse Command behavior: first token `gbot` selects gbot function; further tokens ignored in first slice; other behaviors keep cursor path
      2. [ ] On start: pack Focus extract (`AiExtractPack` shared with cursor); POST wake via WakeHttp with three ids; stay live
      3. [ ] Loop: take inbox texts → FocusXmlStream pending-buffer → post ordinary Core Changes under Focus (helpers from 18 — no second write stack)
      4. [ ] On Cancel token / drop: stop loop; no close-notify wake; framework drop invalidates `sessionId`
      5. [ ] Never expose a bot Graph write API; never Finish solely because wake acked
   3. Uses
      1. [ ] AiExtractPack (shared)
      2. [ ] WakeHttp + GrokbotConfig
      3. [ ] FocusXmlStream / pending-buffer from [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]
      4. [ ] CoreMailbox postEvents / actorStop
      5. [ ] CoreActorPool inbox (via Actor input or injected take)

7. **StubOrProofBot** (optional)
   File: under `tests/` or `proofs/` (not production Server).
   1. State
      1. [ ] None durable
   2. Interface
      1. [ ] POST `{ sessionId, text }` to `/ambit/actors/deliver` with `X-Ambit-Inbound-Secret`
      2. [ ] Enough to prove Focus Children grow end-to-end
   3. Uses
      1. [ ] Inbound door; test host or live Server

## 3. Seams

1. [x] **Browser ↔ RouteRegistration** — existing typed launch / Cancel / Poll (`/ambit/*`)
2. [ ] **ActorsDeliverDoor ↔ CoreActorPool.deliver** — sole inbound bot→Actor door; secret checked before deliver
3. [ ] **GrokbotConfig ↔ User Secrets** — `grokbot:*` bind; WakeHttp and InboundAuth read the same config
4. [ ] **WakeHttp ↔ Admiral hub / bot webhook** — outbound ack-only; auth per hub contract (Ambit adapter)
5. [ ] **CoreActorPool ↔ Run Agent Actor** — start/schedule/drop plus inbox deliver; gbot opts in to inbox
6. [ ] **gbot function ↔ FocusXmlStream** — Interface on **Run Agent Actor — gbot function**; reuses 18 helpers
7. [x] **CoreActorPool ↔ Focus exclusivity** — existing admit; this Project adds `commandId` exclusivity beside it

## 4. Alternative considered

1. **gbot-named inbound door** (`POST /ambit/gbot/deliver` or similar) — Rejected: deliver is generalized for any live Actor; path stays `/ambit/actors/deliver` so the door is Actor-pool shaped, not bot-branded.
2. **CloudAgents stretch for bot replies** — Rejected for first slice: bot conversation is a live webhook wire with inbox deliver, not a CloudAgents job ticket + final report. Cursor CloudAgents path stays owned by llm-connector; gbot shares extract/Focus helpers only.
3. **Direct Graph write API for the bot** — Rejected: would bypass Actor-mediated Changes, Poll, Cancel, and History; map Non-goal and spec Problem 3.
4. **Invent Ambit-only wake auth header** — Rejected: wake must match the existing Admiral hub / bot webhook contract so Alan’s hub panel credentials work; confirm exact header name at wire time (Unsettled).
5. **Bot `kind: close` in first slice** — Deferred: Cancel/drop only; bot learns via 404.

## 5. Unsettled

1. **Hub wake header exact name** — Confirm at wire time against the Admiral hub / bot webhook contract; Ambit adapter follows that name (do not invent).
2. **Azure Key Vault vs User Secrets packaging** — First slice binds via User Secrets for localhost and Azure alike; any Key Vault packaging detail beyond that bind is deferred if needed.
