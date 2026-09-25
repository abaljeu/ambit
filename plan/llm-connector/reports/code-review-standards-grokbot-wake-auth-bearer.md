# Standards — [25 — Grok Bot wake auth Bearer](plan/llm-connector/issues/25-grokbot-wake-auth-bearer.md)

Range: `git diff origin/staging...HEAD` (`d7a2de1b`…`03b5f229`). Mechanical scan: none (no LONG/TAB/MUTABLE/BARE_ID/BLANK_BLANK/FILE/over-40). Surgical under-100-line preference is not a fail. File limit 800 (`.agents/rules/fsharp-source.md`).

## Hard violations

1. **Branch names as delivery status** — `.agents/rules/planning-docs.md`: plan text records what is implemented by ticket, section, or Point, not by git branch names; do not discuss `dev`, `ready`, `master`, or other branches as delivery status in plan or arch docs. `plan/llm-connector/issues/25-grokbot-wake-auth-bearer.md` Context says “Ready later attached `X-Ambit-Wake-Secret`.” Non-goal 3 says “Land / squash onto ready or staging without Alan accept.” Name the invented header by ticket, not by Ready.
2. **File pointers wrap function names** — review checklist: prefer file pointers as code spans, not labeled file links wrapping function names (same lock in [bot-channel map](plan/bot-channel/map.md) Decisions). Ticket What to build 2.1–2.2 uses `[GrokBotHttp.applyWakeAuth](../../../src/CloudAgents/Internal/GrokBotHttp.fs)` and `[GrokBotAdapter.wake](../../../src/CloudAgents/Internal/GrokBotAdapter.fs)`. Write `applyWakeAuth` in `src/CloudAgents/Internal/GrokBotHttp.fs`.

## Judgement-call smells

None. Shotgun Surgery on map / spec / arch / [24 — CloudAgents Grok Bot oneshot stream](plan/llm-connector/issues/24-cloudagents-grokbot-oneshot.md) is the requested Bearer lock; not stood behind.

## No hit

- Name-and-link: added issue refs use `[label](path)`.
- Small public surface: HTTP stays Internal in `src/CloudAgents/Internal/GrokBotHttp.fs`; `wakeSecretHeader` removed.
- Tests: 40-line rule applies; counted every member in `tests/CloudAgents.Tests/GrokBotOneshotTests.fs`. Longest member 29 lines (`fake oneshot emits AssistantText then RunFinished`). Changed members 13 and 9 lines. File 309 lines (limit 800).
- No ticket numbers in module or report filenames.
- `applyWakeAuth` is two words; no `mutable` keyword (Authorization setter is the `HttpRequestMessage` API); added F# lines under 100 chars; 4-space indent.
- `.agents/rules/core-api.md`: no Core API change.

Counts: 2 hard, 0 smells.

**Verdict:** Needs fixes

1. In `plan/llm-connector/issues/25-grokbot-wake-auth-bearer.md`, drop Ready / ready / staging as delivery status.
2. In the same ticket, point at files with code spans, not function-name links.
