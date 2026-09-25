# bot-channel

Stage: slice
Summary: Ambit is a Slack-like direct-message channel to Grok Bots — `?ai gbot` wake webhook, live Actor inbox deliver, Focus streaming writes — with Ambit itself as the channel. First slice is Cursor-Cloud-like (Finish-on-response-end); destination keep-alive wire stays planned.
Updated: 2026-09-25

## Objective

Make Ambit the durable messaging surface for talking to Grok Bots. Cognition stays in Ambit’s folding UI. Git/Origin stays the shared backend for files and docs. Wake a bot with an ack-only webhook POST; bots deliver text into a live Actor via `pool.deliver(sessionId)`; the Actor writes Focus with the Cursor streaming protocol (FocusXmlStream from llm-connector [[../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]], `done`). First slice Finishes when that response concludes (cursor class of terminus). Destination remains a keep-alive chat wire.

## Epic home

- [[plan/roadmap/epics/operate-connected-channels.md]] — Chapter [[plan/roadmap/epics/chapters/ambit-as-bot-dm-channel.md]]
- Cross-cut: [[plan/transport-layer/map.md]]
- Related: [[plan/llm-connector/project.md]] owns cursor `?ai` / CloudAgents; gbot shares Run Agent Actor module helpers but is a separate function

## Out of scope (this Project)

- Slack as document or store of record
- MCP wrap of reply/query verbs
- Replacing CloudAgents job runner
- Persistent secrets product design beyond first-slice User Secrets `grokbot:*`
- Subsequent outbound wakes while a session is live (later ticket; destination keep-alive)
- Fuller-channel `kind: close` / close-notify (later; first slice may use `kind: close` only as the Unsettled Done seam)
- Memory policy (clean vs continue chat context)
- Admiral webhook hub implementation (`ambit-inbound-hub`)

## Notes

- 2026-09-25 — Filed first-slice variant tickets: [[issues/05-gbot-finish-on-response-end.md|05 — gbot Finish on response end]] (Blocked-by 03 and 04; gbot and `?test gbot` Finish when the streamed reply concludes) and [[issues/06-done-seam-response-concluded.md|06 — Done seam for response concluded]] (`defined` research; Options for the Unsettled wire signal). No third pool ticket — [[issues/01-coreactorpool-sessionid-deliver.md|01 — CoreActorPool sessionId + deliver + commandId exclusivity]] already clears `sessionId` on Finish. 03/04 stay channel plumbing. Frontier unchanged for coding: 01 and 02 (`defined`); 06 is also unblocked research. Stage stays `slice`.
- 2026-09-25 — Alan locked an intermediate first slice: gbot wake/webhook is functionally like Cursor Cloud (one wake = one job; stream via FocusXmlStream; Actor Finishes when that response concludes; next query new Run / new `sessionId`; Cancel still drops mid-stream). Destination keep-alive chat wire, multi-turn without Finish-on-every-reply, subsequent outbound wakes, and later `kind: close` / close-notify stay planned — not deleted. Memory policy deferred. [[../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done` on staging; tickets 03 and 04 no longer list 18 as a blocker. Stage stays `slice`.
- 2026-09-24 — Alan locked: streaming Actors including gbot may use mailbox Append when that op exists (preferred for end-append); first-slice tickets are not blocked on Append if 18 ships Replace-based FocusXmlStream. Stage stays `slice`.
- 2026-09-24 — Alan accepted [[arch.md]] as shared understanding and locked a deterministic `?test gbot` case on TestActor (ticket [[issues/04-testactor-gbot-simulation.md|04 — TestActor gbot simulation]]). Stage stays `slice`. Frontier then: 01 and 02 (`defined`); 03 `blocked` by 01, 02, and llm-connector 18; 04 `defined` (Blocked-by 01 and 18). Superseded on the 18 edge 2026-09-25 when 18 became `done`.
- 2026-09-24 — Arch published ([[arch.md]], Sequence `module-build`); tickets 01–03 filed; Stage `slice`. Frontier then: 01 and 02 (`defined`); 03 `blocked` by 01, 02, and llm-connector 18.
- 2026-09-24 — Spec published ([[spec.md]]); Stage was `spec`. Grill locks in [[map.md]].
- 2026-09-24 — Grill closed; locks in [[map.md]] Decisions.
- 2026-09-23 — Charted from Nectar + Alan.
