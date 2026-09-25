# Independent review — AI Actor stream

Pin: base `46f6edb4` (“cloud agent streaming” — [17 — CloudAgents Console stream](plan/llm-connector/issues/17-cloudagents-console-stream.md) tip) to tip `1c523991` on `cursor/ai-actor-stream-69b9`. Diff: `git diff 46f6edb4...1c523991`. Log: `git log 46f6edb4..1c523991 --oneline` — one commit: `1c523991 Stream Focus children from CloudAgents with pending-buffer addChild`. Do not use `origin/staging` three-dot; staging carries extra commits not on this pin.

Ticket: [18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md). Status stays `coded`. A report is not approval.

Axis reports: [Standards](code-review-standards-ai-actor-stream.md), [Spec](code-review-spec-ai-actor-stream.md).

## Scan

`python3 .agents/skills/code-review/scripts/standards-scan.py --diff 46f6edb4` from the tip checkout. The script printed only `--- measure-fs-size ---` bindings. Every printed function is under 40 lines. No LONG, TAB, MUTABLE, BARE_ID, BLANK_BLANK, or FILE lines. Count a printed line as a finding only when it breaks the cited limit in [fsharp-source.md](.agents/rules/fsharp-source.md). Surgical under-100-line preference is not a script fail.

## Standards

Range: `git diff 46f6edb4...1c523991` (`1c523991`). Scan printed no LONG / TAB / MUTABLE / BARE_ID / BLANK_BLANK / FILE lines. Printed bindings stay under 40 lines. Surgical under-100-line preference is not a script fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

### Hard violations

#### [RunAgentActor.fs](src/Server/RunAgentActor.fs) — grouped parameters

[fsharp-source.md](.agents/rules/fsharp-source.md): group related parameters into a named reused type. Reuse a type that already exists. `streamUntilDone` takes `config`, `agentId`, and `runId`, then `streamArgs` rebuilds [StreamArgs](src/CloudAgents/PublicTypes.fs). `complete` unpacks the same three after `start`.

#### [FocusXmlStream.fs](src/Shared/documents/FocusXmlStream.fs) — one-off tuple

Same rule: do not invent a one-off tuple. Reuse a type that already exists. `Step` is `{ draft; adds }`. `stepToken` takes `(draft: Draft, adds: PlannedAdd list)`.

#### [AgentRunnerFakeTests.fs](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs) — 40-line function

Same rule: 40 lines or less per function. The test file-size note does not drop that rule. Scan skips `tests/` for 40-line discovery. Member `partial fake stream waits until cancel` is lines 215–263 (49).

### Judgement-call smells

**Duplicated Code.** [AgentRunnerFake.fs](src/CloudAgents/AgentRunnerFake.fs) `waitForTerminal` repeats the `waitFakeStream` poll loop:

```
if pastDeadline started args.MaxWaitMs then
    Error AgentError.Timeout
elif isCancelled ids then
    cancelledRun ()
```

**Duplicated Code.** [RunAgentActor.fs](src/Server/RunAgentActor.fs) `rememberedChildren` and `nextChildren` share `Map.tryFind` / `parentId = focusId`. The Focus miss arms do not agree (`graph` children vs `[ edge ]`):

```
| None when parentId = focusId -> [ edge ]
| None -> oldChildren @ [ edge ]
```

**Mysterious Name.** `Draft.hold` does not say unparsed remainder.

### No hit

No `mutable` in production hunks. No Exceptions. No line over 100 characters. Public `start` / `push` / `apply` / `flush` require [FocusXmlStream](src/Shared/documents/FocusXmlStream.fs) (`RequireQualifiedAccess`). Draft Events keep `EventId.zero` ([core-api.md](.agents/rules/core-api.md)). New plan links wrap number and name ([refer-by-name.md](.agents/rules/refer-by-name.md)). Stage stays `done`.

Standards: 3 hard, 3 smells. Worst hard: `StreamArgs` exploded on `streamUntilDone`.

## Spec

No spec-axis findings versus [18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md) on pin `46f6edb4...1c523991`.

## Summary

Standards: 6 findings (3 hard, 3 smells); worst: `StreamArgs` exploded on `streamUntilDone` in [RunAgentActor.fs](src/Server/RunAgentActor.fs). Spec: 0 findings.
