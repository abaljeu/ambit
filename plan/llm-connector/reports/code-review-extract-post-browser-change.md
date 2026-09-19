# Code review: Extract postBrowserChange

Independent two-axis review of a one-file test-harness smell fix (not a product ticket). Range: three-dot `origin/staging...HEAD` (merge-base `da0214a6`; tip `33541cce`). Spec: extract private `postBrowserChange` in [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) so `seedAskTree`, `seedCommand`, and `seedBrowserCommand` share the Browser seed post envelope (`EventId.zero`, `Authority "Browser"`, empty `commandName`, `postGraphOnly` + `requireOk`) without changing seed Ops, return shapes, or the public harness face; `postOwnedChild` stays on `postEvents`. Axis drafts: [Standards axis](code-review-standards-extract-post-browser-change.md), [Spec axis](code-review-spec-extract-post-browser-change.md). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` printed `scan: none` (empty stdout). No ticket Status to change. A report is not approval.

## Verdict

**Good.** The diff extracts the shared Browser seed envelope into private `postBrowserChange` and reroutes the three named call sites. Seed Ops and returns stay. `postOwnedChild` is not in the range and still uses `CoreMailbox.postEvents`. Public names stay. Residual Event-record shape on `postOwnedChild` is a suppressed Duplicated Code judgement: that path posts through `postEvents`, and the spec left it alone.

## Standards

Range: three-dot `origin/staging...HEAD` (`33541cce`). Subject: [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). Mechanical scan: none (empty stdout).

### Hard violations

None.

The scan printed no FILE-growth hit. [fsharp-source.md](.agents/rules/fsharp-source.md) exempts tests from the 800/400 file split. The file shrank by extraction. Do not invent a FILE-growth finding.

`postBrowserChange` is 13 lines (198–210). Call sites stay under 40: `seedAskTree` 24, `seedCommand` 12, `seedBrowserCommand` 18. Added lines stay under 100 characters. No `mutable`. No new Exceptions. The helper is `private`; the public-name rule does not apply. The name is already more than one word.

[core-api.md](.agents/rules/core-api.md): the draft Event uses `EventId.zero`. Authority is Browser. Body is a Change of Ops.

[core-agent-behavior.md](.agents/rules/core-agent-behavior.md): the helper replaces three identical `postGraphOnly` seed posts. `postOwnedChild` still uses `CoreMailbox.postEvents` and stays untouched. That is a surgical edit.

No [markdown-writing.md](.agents/rules/markdown-writing.md), [planning-docs.md](.agents/rules/planning-docs.md), [refer-by-name.md](.agents/rules/refer-by-name.md), or [no-retrofit.md](.agents/rules/no-retrofit.md) hit. The range has no plan text.

### Smells (judgement, not hard)

**Duplicated Code** (residual; repo overrides). `postOwnedChild` still builds the same Browser Change Event:

```
{ id = EventId.zero
  submissionId = Guid.NewGuid()
  authority = Authority "Browser"
  commandName = ""
  body = EventBody.Change ... }
```

It posts through `postEvents`, not `postGraphOnly`. [SMELLS.md](.agents/skills/code-review/SMELLS.md) says the repo overrides. Surgical edit in [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) leaves that path alone. A shared post-door parameter would be Speculative Generality.

No Mysterious Name, Data Clumps, Parameter Explosion, or Middle Man on `postBrowserChange`. Three cohesive call sites; three parameters (`host`, `label`, `ops`) are not one type.

Hard 0. Judgement 1 (suppressed). Worst in-axis: none.

## Spec

Spec: one-file test-harness smell fix (Duplicated Code). Spec question: does the diff only extract the shared envelope without changing seed behavior / public harness face? Range: `origin/staging...HEAD` (`33541cce`). Subject: [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). No product ticket.

### (a) Missing or partial

None. Required extract is present: private `postBrowserChange` in [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs); call sites `seedAskTree`, `seedCommand`, and `seedBrowserCommand`; envelope `EventId.zero`, `Authority "Browser"`, empty `commandName`, `postGraphOnly` + `requireOk`. Spec: "Ops/return shapes unchanged." Each seed keeps its Op list and return value. Spec: "`postOwnedChild` stays on `postEvents`." That function is not in the diff and still calls `CoreMailbox.postEvents`. Public names `seedAskTree`, `seedCommand`, `postOwnedChild`, and `launchBrowserAsk` stay.

### (b) Behaviour the spec did not ask

None. The three-dot diff touches only [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). `seedCommand` names a local `ops` list so it can pass the same two Ops to the helper; that is the envelope extract, not extra seed behavior. `postOwnedChild` does not call `postBrowserChange`.

### (c) Implemented but wrong

None. Spec: "Envelope: `EventId.zero`, `Authority "Browser"`, empty `commandName`, `postGraphOnly` + `requireOk`." The helper builds that Event (`submissionId = Guid.NewGuid()`, `body = EventBody.Change ops`), posts with `CoreMailbox.postGraphOnly host testCaller event |> Async.StartAsTask`, then `requireOk label posted |> ignore`. Call-site labels stay `"seed"`, `"seed command"`, and `"seed Browser Command"`. `seedBrowserCommand` stays private. The extract answers the spec question: shared envelope only; seed behavior and public harness face unchanged.

## Summary

Standards: 0 hard + 1 suppressed smell; worst within axis: none. Spec: 0 findings; worst within axis: none. Verdict: Good. No ticket Status to change.
