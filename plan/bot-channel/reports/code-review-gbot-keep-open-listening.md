# Code review — gbot keep-open listening

PR: https://github.com/abaljeu/ambit/pull/119
Range: `origin/staging...HEAD` (three-dot; `origin/staging` = `4711d2aa`)
Ticket: [08 — gbot keep-open listening](../issues/08-gbot-keep-open-listening.md)
Verdict: Good
Ticket Status stays `coded`. This report is not approval.

Re-review after must-fix. Prior Spec finding (fake-stream Finish proof raced the door keep-open timeout) is gone. `ActorsDeliverDoorGrokTests` sits in Collection `GrokBot actor`. `GrokBotFake.streamFake` waits while `Stream` is set and events are not readable (`streamEventsReady`). Keep-open still holds: empty-text Done does not complete `streamUntilComplete` and does not Finish the Actor; a later deliver grows Focus; Cancel/drop is 404 / not live.

Axis reports: [Standards — gbot keep-open listening](code-review-standards-gbot-keep-open-listening.md), [Spec — gbot keep-open listening](code-review-spec-gbot-keep-open-listening.md).

Focused tests: `GrokBotOneshotTests` 13 passed; `AgentGrokBotStreamTests|ActorsDeliverDoorTests|CoreActorPoolDeliverTests` 15 passed.

## Standards

1. **Unused `acc`** — [core-agent-behavior](.agents/rules/core-agent-behavior.md) Surgical Changes: remove variables your change made unused. [GrokBotAdapter.fs](src/CloudAgents/Internal/GrokBotAdapter.fs) `stepEvent` / `streamRun` still thread `acc` and concatenate on each `AssistantText`. The only reader was empty-text `RunFinished` merging `acc` into `AgentResult.Text`. This change stops empty-text from completing, and non-empty Finish uses `result` only. No arm reads `acc`. [GrokBotFake.fs](src/CloudAgents/GrokBotFake.fs) `stepFakeEvent` already uses `(outcome, state)` without `acc`.

## Spec

None.

## Summary

Standards 1, Spec 0. Worst Standards issue: unused `acc` in `streamRun`. No Spec issue.
