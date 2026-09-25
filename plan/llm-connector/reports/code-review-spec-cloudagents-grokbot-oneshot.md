# Spec — [24 — CloudAgents Grok Bot oneshot stream](plan/llm-connector/issues/24-cloudagents-grokbot-oneshot.md)

Range: `git diff origin/staging...HEAD` (`a11d0ce7` on `cursor/grokbot-oneshot-stream-546b`). Status stays `coded`. A report is not approval.

Locks: [bot-channel spec](plan/bot-channel/spec.md) (wake ack-only, no close-notify, no Cursor-job replacement) and [bot-channel map](plan/bot-channel/map.md) (indicative wake payload; inbound path Unsettled).

## Findings

No spec-axis findings.

## No hit

1. Sibling face — [AgentRunner.fs](src/CloudAgents/AgentRunner.fs) and Cursor HTTP are not in the diff. New modules are `GrokBotRunner`, `GrokBotFake`, `Internal/GrokBotHttp`, `Internal/GrokBotAdapter`. The face is oneshot (`wake` then `streamUntilComplete` until `RunFinished`). It is not a keep-alive inbox on Cursor `AgentRunner`.
2. Wake ack-only — `GrokBotHttp.interpretWakeResponse` returns `Ok()` for 2xx and ignores the body. Tests send `"I am the bot reply"` on 200 and still pass.
3. Empty `WakeUrl` — `GrokBotAdapter.wake` returns `AuthenticationFailed` before `postWake`. The message is `AgentMessage.couldNotSend "Grok Bot" "missing wake URL"`. Tests assert the wake and inbound secrets are absent from that text.
4. Unsettled wake-auth — `GrokBotHttp.applyWakeAuth` attaches no header. Comments and tests name the missing Admiral hub contract and forbid `X-Ambit-Wake-Secret` / `X-Ambit-Inbound-Secret`.
5. Unsettled Done — live `streamUntilComplete` without a fake returns `InvalidResponse` (`stream Done seam Unsettled`). Fake `setFakeStream` emits `RunFinished`. No inbound deliver body fields are added.
6. Fake and tests — fake oneshot yields `AssistantText` then `RunFinished`. Cancel mid-stream yields `ApiError("cancelled", …)` and not `RunFinished`. Empty `WakeUrl` is covered. Collection `CloudAgents grokbot` is isolated from Cursor tests.
7. Public surface — `GrokBotConfig` / `GrokBotWakeArgs` / `GrokBotStreamArgs` are the typed objects. `InboundSecret` is on the config and unused (Server door out of scope), as the ticket asked.
8. Non-goals — no CoreActorPool, inbound Server door, or Run Agent Actor `gbot` wiring.

**Verdict:** Good

Items: (none)
