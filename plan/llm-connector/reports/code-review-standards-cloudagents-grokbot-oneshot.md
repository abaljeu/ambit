# Standards — [24 — CloudAgents Grok Bot oneshot stream](plan/llm-connector/issues/24-cloudagents-grokbot-oneshot.md)

Range: `git diff origin/staging...HEAD` (`a11d0ce7`). Scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`. The script printed only `--- measure-fs-size ---`. Every printed binding is under 40 lines. No LONG, TAB, MUTABLE, BARE_ID, BLANK_BLANK, or FILE lines. Surgical under-100-line preference is not a script fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). The file limit is 800 ([fsharp-source.md](.agents/rules/fsharp-source.md)). Scan skips `tests/` for 40-line discovery.

## Hard violations

### 1. Cancel test member is more than 40 lines

[fsharp-source.md](.agents/rules/fsharp-source.md): 40 lines or less per function. That rule applies to tests. Member `cancel mid-stream yields cancelled not Finish` in [GrokBotOneshotTests.fs](tests/CloudAgents.Tests/GrokBotOneshotTests.fs) is lines 183–230 (48). Split the member or extract helpers until the member is 40 lines or less.

## Judgement-call smells

None that this review will stand behind. [GrokBotFake.fs](src/CloudAgents/GrokBotFake.fs) copies the [AgentRunnerFake.fs](src/CloudAgents/AgentRunnerFake.fs) seam (`ref` cells, `withFlight`, `waitUntil`). That is the sibling face the ticket asked for, not a new abstraction.

## No hit

Public face is [GrokBotRunner.fs](src/CloudAgents/GrokBotRunner.fs) plus three records on [PublicTypes.fs](src/CloudAgents/PublicTypes.fs). HTTP and adapter stay in `Internal/`. `wake` / `cancel` require `GrokBotRunner` context (same one-word pattern as `AgentRunner.start` / `cancel`). Stream uses `GrokBotStreamArgs` (StreamArgs-style). `GrokBotRunner.cancel` takes `config` and `sessionId` as two arguments; `AgentRunner.cancel` is already exploded the same way. Empty-`WakeUrl` errors do not write secrets. Wake JSON omits secrets. `postWake` `try/with` matches [CursorHttp.fs](src/CloudAgents/Internal/CursorHttp.fs). New plan links wrap number and name. Stage stays `done`.

Counts: 1 hard, 0 smells. Worst hard: 48-line cancel test member.

**Verdict:** Needs fixes

- Split `cancel mid-stream yields cancelled not Finish` to 40 lines or less.
