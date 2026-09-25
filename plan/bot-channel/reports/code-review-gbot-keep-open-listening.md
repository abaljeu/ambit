# Code review — gbot keep-open listening

PR: https://github.com/abaljeu/ambit/pull/119
Range: `origin/staging...HEAD` (three-dot; `origin/staging` = `4711d2aa`)
Ticket: [08 — gbot keep-open listening](../issues/08-gbot-keep-open-listening.md)
Verdict: Needs fixes
Ticket Status stays `coded`. This report is not approval.

Axis reports: [Standards — gbot keep-open listening](code-review-standards-gbot-keep-open-listening.md), [Spec — gbot keep-open listening](code-review-spec-gbot-keep-open-listening.md).

## Standards

No findings.

## Spec

1. Fake-stream Finish proof fails when the Server suite runs the new door keep-open wait in parallel. Spec: "Explicit `RunFinished` with non-empty `Text` (fake-stream harness) may still terminate." Isolated, `fake Grok stream adds Focus children then Finishes` passes. Together with `good secret delivers chunk then empty Done`, Focus is `["ignored"]` (status-handler text) not `["Text", "two"]`. The door test calls `GrokBotRunner.setFake None` then `streamUntilComplete` with `MaxWaitMs = Some 200` on the live adapter (`withFlight` for the timeout). That wipes or blocks the process-global fake stream seam. `ActorsDeliverDoorTests` is not in Collection `GrokBot actor`.

## Summary

Standards 0, Spec 1. Worst Spec issue: fake-stream Finish proof races the door keep-open timeout.
