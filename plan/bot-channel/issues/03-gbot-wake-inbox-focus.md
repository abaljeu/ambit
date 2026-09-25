# 03 — gbot Run Agent: wake + inbox → Focus stream

**Status:** `blocked`
**Blocked by:** [[01-coreactorpool-sessionid-deliver.md|01 — CoreActorPool sessionId + deliver + commandId exclusivity]], [[02-inbound-actors-deliver-door.md|02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig]] — [[../../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done` on staging (FocusXmlStream available)
**Type:** coding

## Context

With pool deliver and the inbound door in place, the person still needs `?ai gbot` to wake the bot and grow Focus as inbound texts arrive. The gbot path is a separate function inside the existing `ai` Run Agent Actor: share extract helpers with cursor, POST an ack-only wake, then consume the inbox through FocusXmlStream pending-buffer helpers from llm-connector [[../../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] (`done`) — not a second Focus-write stack and not a CloudAgents job ticket. First slice is Cursor-Cloud-like: one wake = one job; stream the reply; Actor Finishes when that response concludes. Destination keep-alive (live until Cancel/drop; multi-turn without Finish-on-every-reply) stays later.

## What to build

Command behavior `gbot` selects the gbot function (Actor name still `ai`). On start: pack Focus extract like cursor, wake via WakeHttp (hub auth contract), stay live after wake ack (do not Finish on wake ack). Consume inbox texts into Focus Children via FocusXmlStream. When that response message concludes, the Actor Finishes (same class as CloudAgents RunFinished on `?ai` cursor). Exact Done seam is Unsettled (`kind: close` may be the inbound signal, or harness Done / explicit inbound kind / empty sentinel). Next query is a new Run / new `sessionId`. Focus writes may post mailbox Append (event-sourced-ops; `commandName` Append; end of Children only; expands to Replace in History) when that op exists — preferred over a hand-built full-list Replace for end-append. [[../../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] is `done` (Replace-based FocusXmlStream available). Cancel still drops mid-stream with no close-notify. Optional stub/proof bot may POST through the inbound door.

### 1. WakeHttp

Per [[arch.md|bot-channel architecture]] module **WakeHttp**.

1. [ ] 2.3.2.1 postWake ack-only — POST wake body; treat HTTP response as ack only (never as bot reply text)
2. [ ] 2.3.2.2 Hub auth — auth header and field layout match Admiral hub / bot webhook contract (Ambit adapter); do not invent an Ambit-only wake header name
3. [ ] 2.3.2.3 Empty WakeUrl — safe domain error; no Graph write of secrets

### 2. Run Agent Actor — gbot function

Per [[arch.md|bot-channel architecture]] module **Run Agent Actor — gbot function** on [[src/Server/RunAgentActor.fs]].

1. [ ] 2.6.2.1 Select gbot — first behavior token `gbot` selects gbot; further tokens ignored; other behaviors keep cursor path
2. [ ] 2.6.2.2 Wake on start — pack via shared `AiExtractPack`; POST wake with `commandId`, `focusId`, `sessionId`; stay live after wake ack (do not Finish on wake ack)
3. [ ] 2.6.2.3 Inbox → FocusXmlStream — consume inbox texts; pending-buffer → ordinary Core Changes under Focus using 18 helpers only. May post Append when that mailbox op exists (preferred for end-append)
4. [ ] 2.6.2.4 Finish on response end — when the response message concludes, Finish (cursor class of terminus). Next query is a new Run / new `sessionId`. Done seam Unsettled (see [[../arch.md]] Unsettled **Done seam for “response concluded”**)
5. [ ] 2.6.2.5 Cancel/drop — stop loop mid-stream on Cancel token / drop; no close-notify wake
6. [ ] 2.6.2.6 Actor-mediated only — no bot Graph write API

### 3. StubOrProofBot (optional)

Per [[arch.md|bot-channel architecture]] module **StubOrProofBot**.

1. [ ] 2.7.2.1 Proof POST — stub or harness POSTs `{ sessionId, text }` with `X-Ambit-Inbound-Secret` and observes Focus Children grow

### 4. Browser shape

1. [ ] 2.1 unchanged — Browser Run / Cancel stays the existing ActorStart path; no second launch door

## See also

[[../arch.md|bot-channel architecture]] modules **WakeHttp**, **Run Agent Actor — gbot function**, story paths **Focus stream from inbox** and **Finish on response end (first slice)**, [[../../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]] (`done`), [[../../event-sourced-ops/project.md|event-sourced-ops]] mailbox Append, [[../spec.md]] User Stories **Run gbot Command**, **Wake ack-only POST**, **Streaming incremental-append**, **Finish on response end (first slice)**, [[../map.md]] Decisions (2026-09-24/25 Finish-on-response-end; wake auth + FocusXmlStream reuse; Append may-use)
