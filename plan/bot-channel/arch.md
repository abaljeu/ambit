# bot-channel architecture

Spec: [[spec.md]]
Updated: 2026-09-25
Sequence: module-build

Sources: [[map.md]] Decisions (arch grill 2026-09-24; Alan accepted arch 2026-09-24 and locked `?test` gbot simulation; intermediate Finish-on-response-end lock 2026-09-24/25); form example [[plan/llm-connector/arch.md]]; Focus stream helpers from [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] (`done`) / `FocusXmlStream`. Checklist: `[x]` already true of the codebase shape; `[ ]` still to build for this Project. First slice is the Cursor-Cloud-like job. Destination keep-alive chat wire stays planned.

## 1. Story paths

1. **Run gbot wake**
   1. [ ] Browser Run Command text `?ai gbot` (extra tokens ignored) on the existing typed ActorStart path
   2. [ ] CoreActorPool admits Focus exclusivity; rejects second start while that `commandId` already has a live row
   3. [ ] CoreActorPool mints `sessionId` on the live row beside `commandId`, `focusId`, and Actor secret
   4. [ ] Run Agent Actor selects the **gbot** function (Actor name still `ai`)
   5. [ ] gbot packs Focus extract like cursor (`AiExtractPack`)
   6. [ ] WakeHttp POSTs ack-only wake with pack + `commandId` + `focusId` + `sessionId`; auth per Admiral hub / bot webhook contract
   7. [ ] Actor stays live after wake ack (does not Finish on wake ack)

2. **Inbound deliver**
   1. [ ] Bot POSTs `POST /ambit/actors/deliver` with header `X-Ambit-Inbound-Secret`
   2. [ ] InboundAuth checks secret from GrokbotConfig (`grokbot:InboundSecret`); reject unauthorized
   3. [ ] ActorsDeliverDoor decodes body `{ sessionId, text }` only
   4. [ ] CoreActorPool `deliver(sessionId, msg)` enqueues to the live Actor inbox; unknown / not live → 404
   5. [ ] Actors that do not read the inbox ignore delivered messages

3. **Focus stream from inbox**
   1. [ ] gbot Actor consumes inbox messages while live
   2. [ ] Each text chunk drives FocusXmlStream pending-buffer → ordinary Core Changes under Focus (reuse 18 helpers — no second Focus-write path). Those Changes may post Append (event-sourced-ops mailbox op; `commandName` Append; end of Children only; expands to Replace in History) when that op exists — preferred over a hand-built full-list Replace for end-append. [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done` (Replace-based FocusXmlStream available).
   3. [ ] Browser Poll shows Focus Children grow

4. **Cancel drops the wire**
   1. [ ] Browser Cancel by Focus (existing door)
   2. [ ] Core drops the live row; `sessionId` invalidated
   3. [ ] No close-notify wake; later inbound gets 404
   4. [ ] Accepted Focus Children stay; new Run on same Command mints a fresh `sessionId`

5. **Finish on response end (first slice)**
   1. [ ] The bot’s response message concludes (Done seam Unsettled — `kind: close` may be the inbound signal, or harness Done / explicit inbound kind / empty sentinel)
   2. [ ] Actor Finishes (same class of terminus as CloudAgents RunFinished / ActorFinished on `?ai` cursor)
   3. [ ] Live row and `sessionId` drop; chrome matches cursor Done
   4. [ ] No close-notify from Ambit; later inbound gets 404
   5. [ ] Next query is a new Run / new `sessionId` (do not keep a long-lived live wire for more queries in this slice)
   6. [ ] Later destination (not this slice): keep-alive until Cancel/drop; multi-turn without Finish-on-every-reply; subsequent outbound wakes while live; fuller-channel `kind: close` / close-notify decisions

6. **Secrets bind**
   1. [ ] GrokbotConfig binds `grokbot:WakeUrl`, `grokbot:WakeSecret`, `grokbot:InboundSecret` from .NET User Secrets (localhost + Azure same path)
   2. [ ] Keys may be empty until Alan loads them; missing wake URL fails the wake call safely without writing secrets into Graph

7. **Stub or proof bot**
   1. [ ] Optional proof under tests/proofs POSTs `{ sessionId, text }` through the inbound door and observes Focus growth

8. **Simulate gbot via TestActor**
   1. [ ] Browser Run Command text `?test gbot` (optional extra tokens are extra canned texts) on the existing typed ActorStart path; Actor name `test`
   2. [ ] TestActor selects gbot-simulation behavior; `?test hello` unchanged
   3. [ ] Canned inbound `text` values enqueue via CoreActorPool `deliver` (no HTTP inbound door; no WakeHttp)
   4. [ ] Each canned text drives FocusXmlStream pending-buffer → ordinary Core Changes under Focus (reuse 18 helpers — same write path as gbot)
   5. [ ] Browser Poll shows Focus Children grow
   6. [ ] Actor Finishes after the canned stream (first-slice parity with Finish-on-response-end). This is not a keep-alive hub session.

Shared segments (paths 1–5):
1. [ ] CoreActorPool live registry with `sessionId` index + `deliver`
2. [ ] Run Agent Actor gbot function (wake + inbox + first-slice Finish-on-response-end)
3. [ ] FocusXmlStream / pending-buffer Focus writes from 18 (`done`)
4. [ ] Existing Cancel / drop / Focus exclusivity
5. [ ] Actor Finish when the response concludes (cursor class of terminus)

Narrowest shared test seam:
1. [ ] CoreActorPool `deliver` + live-row `sessionId` / `commandId` exclusivity (no HTTP)
2. [ ] Inbound door → `deliver` with secret header (harness or test host)
3. [ ] gbot inbox → FocusXmlStream adds under Focus → Finish on response end (fake inbound; 18 is `done`)
4. [ ] TestActor `?test gbot` canned texts → `deliver` + FocusXmlStream → Finish after canned stream (no hub; Blocked-by 01; 18 is `done`)

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
      2. [ ] gbot: in-memory wake-sent flag; first slice consumes inbox until the response concludes then Finishes; destination later consumes until Cancel/drop
   2. Interface
      1. [ ] Parse Command behavior: first token `gbot` selects gbot function; further tokens ignored in first slice; other behaviors keep cursor path
      2. [ ] On start: pack Focus extract (`AiExtractPack` shared with cursor); POST wake via WakeHttp with three ids; stay live after wake ack (do not Finish on wake ack)
      3. [ ] Loop: take inbox texts → FocusXmlStream pending-buffer → post ordinary Core Changes under Focus (helpers from 18 — no second write stack). May post Append when that mailbox op exists (preferred for end-append); first slice may keep 18 Replace-based writes
      4. [ ] First slice terminus: when the response message concludes, Finish (same class as CloudAgents RunFinished on `?ai` cursor). Exact Done seam Unsettled (`kind: close` may be the inbound signal, or harness Done / explicit inbound kind / empty sentinel). Next query is a new Run / new `sessionId`
      5. [ ] On Cancel token / drop: stop loop mid-stream; no close-notify wake; framework drop invalidates `sessionId`
      6. [ ] Never expose a bot Graph write API; never Finish solely because wake acked
      7. [ ] Later destination (not this slice): keep the inbox loop live across turns until Cancel/drop; no Finish-on-every-reply; subsequent outbound wakes and fuller-channel close decisions stay later tickets
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

8. **TestActor**
   File: [[src/Server/TestActor.fs]] (existing; Actor name `test`). Not a second product Actor and not a Shared module.
   1. State
      1. [x] None durable (hello path)
      2. [ ] gbot-sim: canned inbound texts consumed through the live-row inbox until applied, then Finish
   2. Interface
      1. [x] Interpret `?test hello` → post one Owned child `hello` then ActorStop
      2. [ ] First behavior token `gbot` selects simulation; further tokens are extra canned texts; with no extra tokens use a short fixed canned sequence
      3. [ ] Simulated: no WakeHttp, no Admiral hub, no live Grok Bot, no inbound HTTP
      4. [ ] Real: enqueue canned texts via CoreActorPool `deliver` (01); consume inbox through FocusXmlStream pending-buffer (18); ordinary Core Changes under Focus
      5. [ ] Finish after the canned stream (first-slice parity). This is not a keep-alive hub session
      6. [ ] Unknown `?test` behaviors stay ActorFailed; hello path unchanged
   3. Uses
      1. [x] CoreMailbox postEvents / actorStop
      2. [ ] CoreActorPool `deliver` + inbox (via 01)
      3. [ ] FocusXmlStream / pending-buffer from [[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]

## 3. Seams

1. [x] **Browser ↔ RouteRegistration** — existing typed launch / Cancel / Poll (`/ambit/*`)
2. [ ] **ActorsDeliverDoor ↔ CoreActorPool.deliver** — sole inbound bot→Actor door; secret checked before deliver
3. [ ] **GrokbotConfig ↔ User Secrets** — `grokbot:*` bind; WakeHttp and InboundAuth read the same config
4. [ ] **WakeHttp ↔ Admiral hub / bot webhook** — outbound ack-only; auth per hub contract (Ambit adapter)
5. [ ] **CoreActorPool ↔ Run Agent Actor** — start/schedule/drop/finish plus inbox deliver; gbot opts in to inbox; first slice Finishes on response end
6. [ ] **gbot function ↔ FocusXmlStream** — Interface on **Run Agent Actor — gbot function**; reuses 18 helpers (`done`)
7. [x] **CoreActorPool ↔ Focus exclusivity** — existing admit; this Project adds `commandId` exclusivity beside it
8. [ ] **TestActor ↔ deliver + FocusXmlStream** — Interface on **TestActor**; primary deterministic seam (no hub)

## 4. Alternative considered

1. **gbot-named inbound door** (`POST /ambit/gbot/deliver` or similar) — Rejected: deliver is generalized for any live Actor; path stays `/ambit/actors/deliver` so the door is Actor-pool shaped, not bot-branded.
2. **CloudAgents stretch for bot replies** — Rejected as the product path: destination bot conversation is a keep-alive webhook wire with inbox deliver, not a CloudAgents job ticket. First slice is **functionally** like a Cursor Cloud job (one wake, stream one reply, Actor Finish when that response concludes) so Done / chrome match `?ai` cursor — still via wake + inbound deliver + FocusXmlStream, not by stretching CloudAgents. Cursor CloudAgents path stays owned by llm-connector; gbot shares extract/Focus helpers only.
3. **Direct Graph write API for the bot** — Rejected: would bypass Actor-mediated Changes, Poll, Cancel, and History; map Non-goal and spec Problem 3.
4. **Invent Ambit-only wake auth header** — Rejected: wake must match the existing Admiral hub / bot webhook contract so Alan’s hub panel credentials work; confirm exact header name at wire time (Unsettled).
5. **Bot `kind: close` as keep-alive close** — Deferred as the fuller-channel close decision (destination still planned). First-slice terminus is response concluded → Actor Finish. `kind: close` may be that Unsettled inbound Done signal; it is not deleted. Cancel/drop still ends the wire mid-stream; bot learns via 404. Close-notify from Ambit stays deferred.
6. **New command or second Actor for gbot proof** — Rejected: extend existing TestActor / `?test` behavior token `gbot`; do not invent a second product path.

## 5. Unsettled

1. **Hub wake header exact name** — Confirm at wire time against the Admiral hub / bot webhook contract; Ambit adapter follows that name (do not invent).
2. **Azure Key Vault vs User Secrets packaging** — First slice binds via User Secrets for localhost and Azure alike; any Key Vault packaging detail beyond that bind is deferred if needed.
3. **Done seam for “response concluded”** — Product behavior is locked: Actor Finish when the response message ends (cursor class of terminus). Exact wire signal is not locked: bot `kind: close`, harness Done, explicit inbound kind, or empty sentinel. Prefer `kind: close` as the bot→Ambit signal if the hub already has it; otherwise pick the thinnest seam at coding time and record it here. If the chosen signal needs a body field beyond `{ sessionId, text }`, amend the inbound-body lock then — do not invent that field in this slice until the seam is picked.
