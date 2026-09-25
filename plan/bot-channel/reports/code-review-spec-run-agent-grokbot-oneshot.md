# Spec — [05 — Run Agent Actor Grok Bot oneshot](plan/bot-channel/issues/05-run-agent-grokbot-oneshot.md)

Independent review of `origin/cursor/bot-channel-05-grokbot-actor-ccfd` vs `origin/staging` (`git diff origin/staging...HEAD`). Commits: remap tickets; Actor wiring; atomic `GrokBotFake` stream read.

Spec sources: [05 — Run Agent Actor Grok Bot oneshot](plan/bot-channel/issues/05-run-agent-grokbot-oneshot.md); Alan lock 2026-09-24/25 on [bot-channel map](plan/bot-channel/map.md) Decisions; first-slice cut on [bot-channel architecture](plan/bot-channel/arch.md). Eventual [01 — CoreActorPool sessionId + deliver + commandId exclusivity](plan/bot-channel/issues/01-coreactorpool-sessionid-deliver.md)–[03 — gbot Run Agent: wake + inbox → Focus stream](plan/bot-channel/issues/03-gbot-wake-inbox-focus.md) are non-goals. [04 — CloudAgents Grok Bot oneshot library](plan/bot-channel/issues/04-cloudagents-grokbot-oneshot.md) is `done` and points at [24 — CloudAgents Grok Bot oneshot stream](plan/llm-connector/issues/24-cloudagents-grokbot-oneshot.md).

## Alan lock

Same Actor face as Cursor: pack extract, wake/start, `streamUntilComplete` + FocusXmlStream fold on `AssistantText`, Finish on `RunFinished`, Cancel mid-stream. Divergent details stay in CloudAgents (`AgentRunner` vs `GrokBotRunner`). Do not build a Server inbox loop or WakeHttp Actor path. Do not invent a second Actor name. Do not implement the eventual channel tickets. This-slice `sessionId` is Actor-minted oneshot, not pool deliver. Composition binds `grokbot:WakeUrl` / `WakeSecret` / `InboundSecret`. Library stays settings-blind. Do not invent inbound Done-seam fields ([06 — Done seam for response concluded](plan/bot-channel/issues/06-done-seam-response-concluded.md) stays Unsettled).

## Focus

- Shared FocusXmlStream fold — **Pass.** `onStreamEvent` / `streamFold` is one `AssistantText` / `RunFinished` path. Cursor `streamUntilDone` and gbot `completeGrok` both call `runStream`.
- gbot vs cursor selection — **Pass.** `CommandRequest.behaviorFromText` then first `AiKeys.tokensFromText` token `gbot` selects `GrokBotRunner`; extra tokens ignored; other behaviors keep `AgentRunner`; registration stays `ActorName "ai"`.
- GrokBotSettings bind — **Pass.** `GrokBotSettings.fromConfig` reads the three `grokbot:*` keys at `RouteRegistration` composition. Library does not read `IConfiguration`.
- Fake proofs — **Pass.** `setFake` / `setFakeStream` cover Focus growth + Finish class; Cancel keeps streamed children; empty `WakeUrl` writes no secrets; `?ai` stays on `AgentRunner`.
- Cancel — **Pass.** Mid-stream abort is `GrokBotRunner.cancel`; stop is `ActorCancelled`; no close-notify wake.
- Empty `WakeUrl` — **Pass.** Wake error is `ActorFailed`; Graph node texts omit wake and inbound secrets.
- No Server inbox / WakeHttp Actor path — **Pass.** No CoreActorPool `deliver`, no `POST /ambit/actors/deliver`, no WakeHttp module. `sessionId` is `Guid.NewGuid()` in `completeGrok`.

## Findings

None. The diff matches [05 — Run Agent Actor Grok Bot oneshot](plan/bot-channel/issues/05-run-agent-grokbot-oneshot.md) and the Alan lock. The plan remap keeps the channel tickets eventual and points [04 — CloudAgents Grok Bot oneshot library](plan/bot-channel/issues/04-cloudagents-grokbot-oneshot.md) at [24 — CloudAgents Grok Bot oneshot stream](plan/llm-connector/issues/24-cloudagents-grokbot-oneshot.md). Atomic `GrokBotFake` stream read is in the CloudAgents sibling fake, in service of the Cancel/stream proofs.

Verdict: Good
