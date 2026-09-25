# Standards — [05 — Run Agent Actor Grok Bot oneshot](plan/bot-channel/issues/05-run-agent-grokbot-oneshot.md)

Independent review of `origin/cursor/bot-channel-05-grokbot-actor-ccfd` vs `origin/staging` (`git diff origin/staging...HEAD`). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`.

Measure-fs-size bindings are all ≤40 lines. [RunAgentActor.fs](src/Server/RunAgentActor.fs) grew 343→420 (file limit 800 in [fsharp-source](.agents/rules/fsharp-source.md)). Those scan lines are not findings.

## F# conventions (pass)

Small public faces: `GrokBotSettings.fromConfig`; `actorFn` stays the existing `ai` registration. Wake/stream use typed `GrokBotWakeArgs` / `GrokBotStreamArgs`. No secrets in source; `unusedGrokConfig` and `secret-must-not-leak` are test fixtures. GrokBotFake `ref`/`lock` is the existing sibling fake seam; the hunk takes one lock around the stream read and does not add `mutable`. Draft Events keep `EventId.zero`.

## Documented-standard violations (hard)

1. **Bare issue ids** — [refer-by-name](.agents/rules/refer-by-name.md): never refer by only the id; the name wraps the link. Scan BARE_ID lines:
   - [bot-channel map](plan/bot-channel/map.md) line 106: `Ticket 04 (TestActor gbot simulation)` — id without a labeled name-link. [04 — CloudAgents Grok Bot oneshot library](plan/bot-channel/issues/04-cloudagents-grokbot-oneshot.md) is the library remap, not TestActor.
   - [bot-channel spec](plan/bot-channel/spec.md) line 40: `llm-connector ticket 18 / FocusXmlStream` — no `[18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md)`.
   - Same remap prose uses bare `01–03`, `04`, `05`, `24` in [bot-channel spec](plan/bot-channel/spec.md) Sources and Further Notes item 8, and in [bot-channel project](plan/bot-channel/project.md) Notes (2026-09-25).

2. **Obsidian labeled wikilinks** — [markdown-writing](.agents/rules/markdown-writing.md): labeled links must be `[label](path)`; do not write `[[path|label]]`. Added hunks use the forbidden form, for example `[[plan/llm-connector/issues/18-ai-actor-stream.md|18 — AI Actor stream]]` in [bot-channel map](plan/bot-channel/map.md), [bot-channel spec](plan/bot-channel/spec.md), and [bot-channel architecture](plan/bot-channel/arch.md).

3. **Unused import** — [core-agent-behavior](.agents/rules/core-agent-behavior.md): remove imports your change made unused. New [GrokBotSettings.fs](src/Server/GrokBotSettings.fs) has `open Gambol.Shared` with no Shared names (`GrokBotConfig` is CloudAgents; `Option.ofObj` is FSharp.Core).

## Smells (judgement call)

None stood. `runComplete` adding `grok` beside existing `keys` / `repos` matches the exploded composition style and is not a new mega-record.

Verdict: Needs fixes — name-and-link every issue id in the remap prose; replace `[[path|label]]` with `[label](path)`; drop unused `open Gambol.Shared`.
