# Standards — [08 — gbot keep-open listening](../issues/08-gbot-keep-open-listening.md)

Range: `git diff origin/staging...HEAD`

1. **Unused `acc`** — [core-agent-behavior](.agents/rules/core-agent-behavior.md) Surgical Changes: remove variables your change made unused. [GrokBotAdapter.fs](src/CloudAgents/Internal/GrokBotAdapter.fs) `stepEvent` / `streamRun` still thread `acc` and concatenate on each `AssistantText`. The only reader was empty-text `RunFinished` merging `acc` into `AgentResult.Text`. This change stops empty-text from completing, and non-empty Finish uses `result` only. No arm reads `acc`. [GrokBotFake.fs](src/CloudAgents/GrokBotFake.fs) `stepFakeEvent` already uses `(outcome, state)` without `acc`.

```
        | AssistantText chunk ->
            outcome, state, acc + chunk
        | RunFinished result when String.IsNullOrEmpty result.Text ->
            outcome, state, acc
        | RunFinished result ->
            Some(Ok result), state, acc
```

Findings: 1
