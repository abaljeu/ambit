# 03 — gbot Run Agent: wake + inbox → Focus stream

**Status:** coded
Actual: 50m
**Blocked by:** [01 — CoreActorPool sessionId + deliver + commandId exclusivity](01-coreactorpool-sessionid-deliver.md), [02 — Inbound POST /ambit/actors/deliver + secret + GrokbotConfig](02-inbound-actors-deliver-door.md). [18 — AI Actor stream](../../llm-connector/issues/18-ai-actor-stream.md) is `done`.
**Type:** coding

**Eventual / deferred from the oneshot first slice.** Keep as the eventual fuller wake + inbox wire. First slice wires `GrokBotRunner` on the existing Run Agent Actor in [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md); library is [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md) / [24 — CloudAgents Grok Bot oneshot stream](../../llm-connector/issues/24-cloudagents-grokbot-oneshot.md). Do not build a separate Server inbox loop / WakeHttp Actor path in the oneshot slice. Do not delete this ticket.

## Context

With pool deliver and the inbound door in place, the person still needs a keep-alive `?ai gbot` path that wakes the bot and grows Focus as inbound texts arrive. That fuller channel is this ticket. The oneshot first slice already selects `gbot` inside the existing `ai` Run Agent Actor and shares FocusXmlStream with cursor via [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md). This ticket is the eventual inbox plumbing (wake via Server WakeHttp if still needed, consume inbox, Cancel/drop). Destination keep-alive (live until Cancel/drop; multi-turn without Finish-on-every-reply) stays later.

## Later (moved from old [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md))

TestActor `?test gbot` canned simulation (no hub, deliver + FocusXmlStream) stays later on this eventual channel — not as current [04 — CloudAgents Grok Bot oneshot library](04-cloudagents-grokbot-oneshot.md).

## What to build

Command behavior `gbot` already selects the oneshot Grok backend in [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md). This eventual ticket adds the inbox loop: stay live after wake ack, consume inbox texts into Focus Children via FocusXmlStream, optional Server WakeHttp if the library wake is not enough. Done seam stays [06 — Done seam for response concluded](06-done-seam-response-concluded.md). Focus writes may post mailbox Append (event-sourced-ops; `commandName` Append; end of Children only; expands to Replace in History) when that op exists — preferred over a hand-built full-list Replace for end-append. [18 — AI Actor stream](../../llm-connector/issues/18-ai-actor-stream.md) is `done` (Replace-based FocusXmlStream available). Cancel still drops mid-stream with no close-notify. Optional stub/proof bot may POST through the inbound door.

### 1. WakeHttp

Per [bot-channel architecture](../arch.md) module **WakeHttp**.

1. [x] 2.3.2.1 postWake ack-only — POST wake body; treat HTTP response as ack only (never as bot reply text) via existing `GrokBotRunner.wake`
2. [x] 2.3.2.2 Hub auth — outbound wake header `X-Ambit-Wake-Secret: <WakeSecret>`; empty secret fails closed
3. [x] 2.3.2.3 Empty WakeUrl — safe domain error; no Graph write of secrets

### 2. Run Agent Actor — gbot function

Per [bot-channel architecture](../arch.md) module **Run Agent Actor — gbot function** on [[src/Server/RunAgentActor.fs]].

1. [x] 2.6.2.1 Select gbot — first behavior token `gbot` selects gbot; further tokens ignored; other behaviors keep cursor path
2. [x] 2.6.2.2 Wake on start — pack via shared `AiExtractPack`; POST wake with `commandId`, `focusId`, pool `sessionId`; stay live after wake ack
3. [x] 2.6.2.3 Inbox → FocusXmlStream — door/`GrokBotRunner.deliver` feeds `streamUntilComplete`; empty `text` is oneshot Done; FocusXmlStream fold unchanged
4. [x] 2.6.2.5 Cancel/drop — mid-stream `GrokBotRunner.cancel` result is logged (not ignored); no close-notify wake
5. [x] 2.6.2.6 Actor-mediated only — no bot Graph write API

### 3. StubOrProofBot (optional)

Per [bot-channel architecture](../arch.md) module **StubOrProofBot**.

1. [x] 2.7.2.1 Proof POST — door harness POSTs `{ sessionId, text }` (and empty Done) through `Api.postActorsDeliver`; Actor proof uses the same deliver contract

### 4. Browser shape

1. [x] 2.1 unchanged — Browser Run / Cancel stays the existing ActorStart path; no second launch door

## See also

[arch.md](../arch.md) modules **WakeHttp**, **Run Agent Actor — gbot function**, story path **Focus stream from inbox**, [18 — AI Actor stream](../../llm-connector/issues/18-ai-actor-stream.md) (`done`), [event-sourced-ops](../../event-sourced-ops/project.md) mailbox Append, [spec.md](../spec.md) User Stories **Run gbot Command**, **Wake ack-only POST**, **Streaming incremental-append**, [05 — Run Agent Actor Grok Bot oneshot](05-run-agent-grokbot-oneshot.md), [map.md](../map.md) Decisions (wake auth + FocusXmlStream reuse; Append may-use)

## Comments

- 2026-09-25 — Live `streamRun` waits on inbound `deliver`; empty `text` is oneshot Done. Status `coded`.
- 2026-09-25 — Wake header locked: `X-Ambit-Wake-Secret`. Empty secret fails closed. Status stays `coded`.

## Time

- 2026-09-25 30m — live inbound stream + Actor sessionId + cancel log (from chat)
- 2026-09-25 20m — outbound `X-Ambit-Wake-Secret` + fail-closed empty secret (from chat)
