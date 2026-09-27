# 12 — Contract old Load Fetch packages — independent code review

Range: `origin/staging...HEAD`.

## Standards

### Must-fix

1. **Remove the remaining singular package projection** — [ResidentProjection](../../../src/Shared/ResidentProjection.fs) still exports `packagesForTarget`, which returns an owning-Workspace subgraph through `workspaceSubgraphNodes`. [Load capture tests](../../../tests/Shared.Tests/LoadCaptureTests.fs) keep four callers and package-path assertions. Production Load now calls `wantAnswerForTargets`, so this is unused production surface for the retired path. This conflicts with the requested Standards check for no leftover dual-run or package path under `src/` and `tests/`, and with [core agent behavior](../../../.agents/rules/core-agent-behavior.md): “Consider removing code to achieve a goal.”

### Nice-to-have

1. **Use current event id wording in edited test names** — [Client history runtime tests](../../../tests/Shared.Tests/ClientHistoryRuntimeTests.fs) rename three `package-only` tests to `answer-only` but retain `Revision`. [Ambit context](../../../CONTEXT.md) retires `Revision` in favor of `event id`. The nearby production comment also says `revision`, but that text predates this change.

The mechanical scan found no binding or file size violation. `loadWantAnswer` is 17 lines and `wantAnswerForTargets` is 14 lines, below the 40-line limit in [F# source rules](../../../.agents/rules/fsharp-source.md).

## Spec

### Must-fix

1. **The legacy Load Fetch package API is not fully removed** — [12 — Contract old Load Fetch packages](../issues/12-contract-old-load-fetch-packages.md) says, “Remove the legacy Load Fetch package API,” and requires proof that “no legacy symbol or caller remains.” The Browser residency [map](../map.md) says there is “no dual-run or compatibility path.” `packagesForTarget` and its four tests still implement and prove the old complete owning-Workspace package projection. The named plural symbols are gone, and the current Events → `installWantAnswer` path is intact, but the broader contract proof checkmark is not honest while this singular helper and its callers remain.

### Nice-to-have

No findings.

## Summary

Standards: 1 must-fix and 1 nice-to-have; worst issue is the remaining `packagesForTarget` path. Spec: 1 must-fix; worst issue is the incomplete legacy API removal and resulting dishonest contract-proof checkmark.
