# Spec — [25 — Grok Bot wake auth Bearer](plan/llm-connector/issues/25-grokbot-wake-auth-bearer.md)

Range: `git diff origin/staging...HEAD` (`d7a2de1b`…`03b5f229`). Confirmed the 13-file wake-auth Bearer change (library header, grokbot proofs, plan lock). Status stays `coded`. A report is not approval.

## Findings

No spec-axis findings.

## No hit

- Bearer matches the locked hub contract; not an X-Ambit wake header — `GrokBotHttp.applyWakeAuth` in `src/CloudAgents/Internal/GrokBotHttp.fs` sets `Authorization: Bearer <WakeSecret>` via `AuthenticationHeaderValue`. `GrokBotHttp.postWake` applies that seam. [24 — CloudAgents Grok Bot oneshot stream](plan/llm-connector/issues/24-cloudagents-grokbot-oneshot.md), [bot-channel map](plan/bot-channel/map.md), [bot-channel spec](plan/bot-channel/spec.md), and [bot-channel architecture](plan/bot-channel/arch.md) record the same lock and keep inbound as `X-Ambit-Inbound-Secret`.
- Empty `WakeSecret` is safe; secrets are not in Graph or error text — whitespace leaves Authorization unset. `GrokBotAdapter.wake` in `src/CloudAgents/Internal/GrokBotAdapter.fs` fails closed (`missing wake secret`) without writing `WakeSecret` into the message. Wake JSON omits secrets (`wake JSON is the map payload and omits secrets` in `tests/CloudAgents.Tests/GrokBotOneshotTests.fs`).
- Inbound door not broken or rewritten to Bearer — `Api.inboundSecretHeader` / `Api.postActorsDeliver` in `src/Server/Api.fs` and `/ambit/actors/deliver` in `src/Server/RouteRegistration.fs` stay on `X-Ambit-Inbound-Secret` and are outside this diff.
- Tests prove `Authorization` Bearer with `WakeSecret` and forbid X-Ambit on wake — `applyWakeAuth sends Authorization Bearer` and `applyWakeAuth skips header when secret is blank` in `tests/CloudAgents.Tests/GrokBotOneshotTests.fs`.

**Verdict:** Good

Items: (none)
