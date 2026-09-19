# Standards — AI Actor failure preserves children

Range: `git diff origin/staging...HEAD` (`e9d59aa274a80421254dfb0a0f6b526f62d9fe4e`..`640ad6d9bd462a554e9ffe8d4e227c56c38e6a61`).

## Mechanical scan

```
tests/Server.Tests/AskCancelHarness.fs  .agents/rules/fsharp-source.md  FILE 378->411  already over 400 or new file over 400; change increased it
```

Scan exit 1. The 800/400 file-size rule in [fsharp-source.md](.agents/rules/fsharp-source.md) says it does not apply to tests. The printed hit is exempt. No new test member is over 40 lines.

## Findings

### 1. Scan file size on AskCancelHarness (exempt)

Documented-standard hit from the scan, rule path [fsharp-source.md](.agents/rules/fsharp-source.md), FILE 378->411 on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). The same rule text exempts tests. This is not a hard violation.

### 2. Number-only 08 in ticket 09

Hard. [refer-by-name.md](.agents/rules/refer-by-name.md) requires number and name. New text in [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md) says `Amb replace landed on 08` and `Amb replace on 08`. Those mentions are number only.

### 3. Duplicated Code: waitFailed

Judgement. [AgentRunnerFakeTests.fs](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs) adds `waitFailed` in the same shape as `waitFinished`:

```
| Ok(Failed msg) -> Some msg
| Ok Running when left > 0 ->
    Thread.Sleep 10
    spin (left - 10)
```

No hit on [core-api.md](.agents/rules/core-api.md) EventId serial or Core vs Adapter. No unused leftover from this change in [core-agent-behavior.md](.agents/rules/core-agent-behavior.md). [project.md](plan/llm-connector/project.md) Stage stays `build`.

Hard 1, judgement 1, exempted scan 1. Worst in-axis: number-only 08 in ticket 09.
