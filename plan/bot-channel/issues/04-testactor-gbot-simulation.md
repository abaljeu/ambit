# 04 — TestActor gbot simulation

**Status:** `defined`
**Blocked by:** [[01-coreactorpool-sessionid-deliver.md|01 — CoreActorPool sessionId + deliver + commandId exclusivity]], [[../../llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]
**Type:** coding

## Context

Builders need to prove inbound-shaped Focus growth without a live Grok Bot, Admiral hub, or wake HTTP. Today [[src/Server/TestActor.fs|TestActor]] (Actor name `test`) only handles `?test hello`: one Owned child then ActorStop. Alan locked an extra `?test` case that simulates gbot-shaped deliver + FocusXmlStream writes. This is not a second product path and does not replace `?ai gbot`.

## What to build

Command text `?test gbot` starts TestActor. Canned inbound texts enqueue through CoreActorPool `deliver` and grow Focus Children via FocusXmlStream pending-buffer helpers from 18. No WakeHttp, no hub, no inbound HTTP door. `?test hello` and unknown `?test` behaviors stay as they are. Optional StubOrProofBot HTTP proof stays on [[03-gbot-wake-inbox-focus.md|03 — gbot Run Agent: wake + inbox → Focus stream]].

### 1. TestActor

Extend existing TestActor per [[../arch.md|bot-channel architecture]] module **TestActor** (State / Interface / Uses there — do not fork a second map).

1. [ ] 2.8.2.2 Select gbot-sim — first behavior token `gbot` selects simulation; further tokens are extra canned inbound texts; with no extra tokens use a short fixed canned sequence
2. [ ] 2.8.2.3 Simulated vs real — no WakeHttp, no Admiral hub, no live Grok Bot, no inbound HTTP
3. [ ] 2.8.2.4 deliver + FocusXmlStream — enqueue canned texts via pool `deliver` (01); consume inbox through 18 pending-buffer helpers; ordinary Core Changes under Focus
4. [ ] 2.8.2.5 hello unchanged — `?test hello` still posts one Owned `hello`; unknown `?test` behaviors stay ActorFailed

### 2. Proof

1. [ ] 1.7 / 1.Narrowest.4 — Server or Browser Run of `?test gbot` grows Focus Children from the canned texts; Poll sees the growth; no hub

## See also

[[../arch.md|bot-channel architecture]] story path **Simulate gbot via TestActor**, module **TestActor**, [[../spec.md]] User Story **Deterministic gbot TestActor case**, [[../map.md]] Decisions (Alan accepted arch; locked `?test` gbot simulation)
