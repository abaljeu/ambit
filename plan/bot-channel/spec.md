# bot-channel

Updated: 2026-09-25

Sources: [[map.md]] Decisions so far (grill locked 2026-09-24; arch locks 2026-09-24; intermediate Finish-on-response-end lock 2026-09-24/25). Epic chapter: [[plan/roadmap/epics/chapters/ambit-as-bot-dm-channel.md]]. Related Project: [[plan/llm-connector/project.md]] (cursor `?ai` / CloudAgents). Cross-cut: [[plan/transport-layer/map.md]].

## 1. Problem Statement

1. **Ambit as the bot DM surface** — A person wants to talk with a Grok Bot the way they talk in a direct-message channel, without leaving Ambit’s folding Graph UI or treating Slack as the store of record.
2. **Wake as a channel, not only a job ticket** — Today’s cursor `?ai` path completes an Agent job and Finishes. The destination bot conversation needs a keep-alive live wire: Ambit wakes the bot, the bot keeps posting text back over time, and Focus grows as replies arrive — not Finish-on-every-reply. The first slice is the easier Cursor-Cloud-like job (one wake, stream one reply, Actor Finish when that response concludes) so Done / chrome / Finish match `?ai` cursor. That first step does not abandon the keep-alive destination.
3. **Actor-mediated writes** — The bot must not gain a direct Graph write API. Replies must enter through a live Actor so Focus Children, ordinary Core Changes, Poll, and Cancel stay one model with the rest of Ambit.
4. **Stable conversation address** — One Command Node (`?ai gbot`) should stay the conversation address across the live session, while each Run mints a fresh live-wire id the bot echoes on every inbound post. A second start on the same Command while live must be rejected.
5. **Secrets stay outside Graph** — Wake URL, channel secret, and inbound shared secret must not live in the Graph; `sessionId` is minted per live Actor and is not a stored channel secret.

## 2. Solution

1. **Command `?ai gbot`** — The person focuses under a Command Node whose text is `?ai gbot` (options ignored in the first slice) and Runs. Actor name stays `ai` from `?ai`. Inside the Run Agent Actor module, `gbot` selects a separate function that shares helpers with the cursor path rather than replacing CloudAgents.
2. **Three ids** — `commandId` is the Graph conversation address (the Command Node). `focusId` is the reply parent for this turn’s Focus writes. `sessionId` is a new Guid per Actor start — the live wire the bot must echo on inbound. Deliver’s primary key is `sessionId`. Reject a second Actor start while that `commandId` already has a live row. After Cancel, Finish, or drop, a new Run may mint a new `sessionId`.
3. **Wake (Ambit → bot)** — On start, Ambit POSTs an ack-only webhook. The first wake carries the same Focus extract pack as `?ai cursor`, plus `commandId`, `focusId`, and `sessionId`. Wake URL and channel secret come from .NET User Secrets (`grokbot:WakeUrl`, `grokbot:WakeSecret`). Wake auth matches the Admiral hub / bot webhook contract (Ambit adapter). HTTP response is ack only — not the bot’s reply body.
4. **Inbound (bot → Ambit → Actor)** — The bot POSTs to `POST /ambit/actors/deliver` with header `X-Ambit-Inbound-Secret` (TLS; easy auth is enough). Body is exactly `sessionId` + `text`. Api calls generalized `pool.deliver(sessionId, …)` for any live Actor; Actors that do not read the inbox ignore it. No live session → 404. No close-notify wake from Ambit on Cancel/Finish (bot learns via 404).
5. **Focus streaming writes** — The gbot Actor consumes its inbox and writes under Focus with the Cursor streaming incremental-append protocol (llm-connector [[../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done` / FocusXmlStream helpers — no second Focus-write path). First slice: when that response message concludes, the Actor Finishes (same class as CloudAgents RunFinished on `?ai` cursor). Do not Finish on wake ack. Cancel still drops mid-stream. No idle timeout. Next query is a new Run / new `sessionId`. Destination later: the Actor stays live until Cancel/drop without Finish-on-every-reply; subsequent outbound wakes while the session is live stay later tickets. Exact wire signal for “response concluded” is Unsettled (`kind: close` may be that inbound signal). Close-notify from Ambit stays deferred.
6. **Origin boundary** — Conversation Graph / Focus writes stay with the Ambit Actor. Files and docs stay on Origin/git. Always-on bot wake stays on the Grok Bot webhook (Admiral hub already exists; this Project does not implement it).

## 3. User Stories

1. **Run gbot Command** — As a person, I want Command text `?ai gbot` under Focus to start the AI Actor’s gbot function (Actor name still `ai`), so that I open a bot conversation without inventing a second Actor registration name.
2. **Shared helpers with cursor** — As the Run Agent Actor module, I want gbot and cursor to share extract and Focus-write helpers while remaining separate functions, so that bot wake does not fork a second pack/write stack.
3. **Ignore options for now** — As a person, I want extra tokens after `gbot` ignored in the first slice, so that Run still starts without a multi-bot options UI.
4. **Mint sessionId on start** — As CoreActorPool, I want each gbot Actor start to mint a new `sessionId` Guid on the live row (alongside `commandId`, `focusId`, and Actor secret), so that the bot has a live wire to echo.
5. **Deliver by sessionId** — As the inbound door, I want `pool.deliver` keyed by `sessionId`, so that any live Actor can receive inbox messages without the bot knowing Credential secrets.
6. **commandId exclusivity** — As Core, I want a second Actor start rejected while that `commandId` already has a live row, so that one Command Node cannot host two concurrent bot wires.
7. **Reuse Focus exclusivity where it still applies** — As Core, I want existing Focus live rules to keep cooperating with gbot starts, so that two Actors do not race the same Focus reply parent.
8. **Wake ack-only POST** — As Ambit, I want the wake HTTP response to be ack only, so that the bot’s reply is never smuggled through the wake response body.
9. **Wake Focus extract like cursor** — As the gbot function, I want the first wake payload’s text/pack to follow the same Focus extract path as `?ai cursor`, so that the bot sees the same Zoom-rooted working set the person is looking at.
10. **Wake carries three ids** — As the bot, I want the wake body to include `commandId`, `focusId`, and `sessionId`, so that I can address the conversation and echo the live wire on inbound.
11. **Secrets from User Secrets** — As composition, I want wake URL, channel secret, and inbound secret bound from .NET User Secrets (`grokbot:*`), so that secrets do not land in Graph Nodes and localhost + Azure share one bind path.
12. **Inbound shared-secret header** — As the inbound door, I want header `X-Ambit-Inbound-Secret` over TLS, so that easy auth is enough when the door only asks a live Actor to insert text.
13. **Generalized pool.deliver** — As CoreActorPool, I want `deliver(sessionId, msg)` available to any live Actor that opts in, so that bot inbound is not a one-off gbot-only mailbox hack.
14. **Actor-mediated Focus writes** — As a person, I want bot text applied under Focus by the live Actor via ordinary Core Changes, so that Poll, merge, History, and Cancel stay one model — not a bot Graph write API.
15. **Streaming incremental-append** — As a person, I want inbound bot text to grow Focus Children with the Cursor streaming incremental-append protocol (llm-connector ticket 18 / FocusXmlStream), so that I see durable replies appear as the bot posts rather than one late replace.
16. **Keep-alive later (destination)** — As a person, I want a later slice where the Actor stays live across turns until I Cancel or the row drops, so that a multi-turn bot session is not Finish-on-every-reply and is not cut by an idle timeout.
17. **Cancel drops the wire** — As a person, I want Cancel by Focus to drop the live row and invalidate `sessionId` mid-stream, so that later bot posts get 404 and Focus Children already accepted stay.
18. **No close-notify** — As Ambit, I want no outbound close-notify wake on Cancel/Finish, so that the bot discovers the closed wire via 404 on its next inbound.
19. **Second start after drop** — As a person, I want a new Run after Cancel/Finish/drop to mint a fresh `sessionId` on the same Command, so that I can reopen the conversation without renaming the Command Node.
20. **404 when not live** — As the bot, I want HTTP 404 when `sessionId` is unknown or no longer live, so that I know the wire is gone without a special close event.
21. **Stub or real bot proof** — As a builder, I want a stub or real bot to POST text with `sessionId` through the inbound door and see Focus Children grow, so that the first vertical slice is proven end-to-end.
22. **Browser Run unchanged shape** — As a person, I want Browser Run on `?ai gbot` to use the same ActorStart path as today (typed launch membership, Zoom, Focus, Command), so that gbot does not invent a second launch door.
23. **Files stay on Origin** — As a person, I want files and docs to remain on Origin/git while bot cognition stays in the Graph under Focus, so that Ambit is the channel and git stays the file backend.
24. **Inbound secret in Server config** — As composition, I want the inbound shared secret in Ambit Server User Secrets (`grokbot:InboundSecret`), so that channel auth is not a Graph field.
25. **Deterministic gbot TestActor case** — As a builder, I want Command text `?test gbot` to grow Focus Children from canned texts the way inbound deliver plus FocusXmlStream would, then Finish after that canned stream, so that Server and Browser proofs do not need the Admiral hub or a live Grok Bot and Done / chrome match first-slice gbot.
26. **Finish on response end (first slice)** — As a person, I want the Actor to Finish when the bot’s response message concludes (same class as cursor RunFinished), so that Done / chrome match `?ai` cursor. Next query is a new Run / new `sessionId`.

## 4. Out of Scope

1. **Slack as store of record** — This Project does not make Slack the document store or channel of record for bot conversation.
2. **MCP-first transport** — Wrapping reply/query verbs in MCP is optional later; not the first transport.
3. **Replace cursor CloudAgents path** — This Project does not replace `?ai` / CloudAgents job runner owned by [[plan/llm-connector/project.md]].
4. **Persistent secrets product design** — Beyond first-slice .NET User Secrets bind (`grokbot:*`); Azure Key Vault packaging details stay unsettled if needed later.
5. **Subsequent outbound wakes** — Later-turn Ambit→bot wakes while a session is live are later tickets (destination keep-alive). Not canceled.
6. **Close-notify wake** — No Ambit→bot notify on Cancel/Finish in the first slice; bot learns via 404. Later close-notify decisions for the keep-alive channel remain planned.
7. **Fuller-channel `kind: close`** — Keep-alive close decisions remain later tickets. First-slice terminus is Finish-on-response-end; `kind: close` may be that Unsettled inbound Done signal (see [[map.md]] Decisions 2026-09-24/25).
8. **Memory policy** — Clean vs continue chat context is deferred; do not design in this amend.
9. **Admiral hub implementation** — Implementing `ambit-inbound-hub` is out; the hub already exists for always-on wake.
10. **Attachment UX** — Attachment pointers and file-send UX are deferred.
11. **HMAC / hard inbound crypto** — Beyond shared-secret header over TLS; HMAC upgrades later.
12. **Idle timeout** — No first-slice idle timeout on the live Actor.
13. **Multi-bot routing UI** — `routeHint` / options on `?ai gbot` for choosing among bots is deferred.
14. **Optional inbound `commandId`** — Body is `sessionId` + `text` only; no optional commandId defense field in the first slice.

## 5. Further Notes

1. **Grill locks (2026-09-24)** — Command `?ai gbot`; gbot = separate function in Run Agent Actor with shared cursor helpers; `commandId` / `focusId` / `sessionId` split; deliver by `sessionId`; reject second start while `commandId` live; wake #1 = Focus extract like cursor; inbound = `POST /ambit/actors/deliver` + `X-Ambit-Inbound-Secret` → generalized `pool.deliver`; body `sessionId` + `text` only; Actor-mediated Focus streaming writes via FocusXmlStream from [[../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]; secrets = User Secrets `grokbot:WakeUrl` / `WakeSecret` / `InboundSecret`. Close wording from this grill (live until Cancel/drop; no bot `kind: close`) is the destination / later tickets — amended for first slice on 2026-09-24/25.
2. **Intermediate lock (2026-09-24/25)** — First slice = Finish-on-response-end like Cursor Cloud (one wake, stream one reply, Actor Finish when that response concludes; next query new Run / new `sessionId`; Cancel still drops mid-stream). Keep-alive chat wire remains Goal / later tickets. Memory policy deferred. Exact Done seam Unsettled. [[../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done`.
3. **Indicative wake payload** — `source`, `kind: message`, `sentAt`, `commandId`, `focusId`, `sessionId`, `text`, `payload` as in [[map.md]]; wake auth header name follows the hub contract (confirm at wire time).
4. **Unsettled for wire / packaging / Done seam** — Hub wake header exact name (confirm when wiring Ambit adapter); Azure Key Vault vs User Secrets packaging details if any; exact signal for “response concluded” (`kind: close` vs harness Done / explicit inbound kind / empty sentinel). Settled: inbound path, body shape, User Secrets key names; first-slice product terminus = Actor Finish on response end.
5. **Spoken name** — Run Agent / AI Actor for the Ambit side; Grok Bot for the webhook counterpart. Do not call the Ambit Actor an Agent ([[CONTEXT.md]]).
6. **Architecture** — [[arch.md]]; Sequence `module-build`; coding tickets under [[map.md]] Implementation (01–03 product modules; 04 TestActor `?test gbot` simulation).
7. **TestActor lock (2026-09-24)** — Alan accepted the architecture. `?test` gains a gbot-simulation behavior on existing TestActor (Actor name `test`). Optional HTTP StubOrProofBot stays; `?test gbot` is the primary deterministic seam. Not a second product path. Simulation Finishes after the canned stream (first-slice parity).
