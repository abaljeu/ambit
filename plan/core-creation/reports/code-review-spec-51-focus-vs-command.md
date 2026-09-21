# Spec review — 51 Focus vs Command on Run

Range: `git diff origin/staging...HEAD` (three-dot). HEAD `6570a866318ce02ecc80c29ef9096e75d67f9d82` (draft PR #81). origin/staging `b484d99ef9b3afd76dc706b740f4a5a35f5c9144`. 10 files, +370 / −33. Diff is non-empty.

Primary spec: [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md). Also [core-creation architecture](plan/core-creation/arch.md) **Browser Run** Interface item 3 Product Run, [llm-connector architecture](plan/llm-connector/arch.md) Locked **Focus vs Command on Run** (item 8), and [Plan or doc change — Focus vs Command on Run](plan-or-doc-change-51-focus-vs-command.md) `=` amendment.

Focused tests passed: `CommandRequestTests` 16, `TestActorHelloTests` 9 (question Focus proof and `?test hello` one-Node).

## (a) Missing or partial

None. Client Run sets `focusId` from selection Focus, `commandId` from Focus→Zoom owner-scan, `zoomId` to the Included Zoom root. No Command and not Amble → Run Error. Shared `tryStart` and Server TestActor prove distinct `commandId` / `focusId`, hello Owned Child under the question Focus, and question text kept. Non-goals (Amb replace vs stream, Actor live label TitleCase, Core door widen) are not in the code diff.

## (b) Behaviour not asked for

None in product code. `execAmbleRunOp` is the old Amble path when `tryStart` is Error. `oneNodeStart` stays as the hello helper.

## (c) Implemented but wrong

1. **`=` Command starts an Actor.** Locked **Focus vs Command on Run** (item 8): "Command is the nearest runnable owner-ancestor that selects the Actor." Same item: stop at the first runnable node — "do not skip a `=` line to reach a `?` above it." [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md) encode 2: set `commandId` from `?` or `=`. **Browser Run** Interface item 4: "otherwise AmbleRun". `isRunnableText` makes `tryStart` Ok for `x = 1`. Client then `SubmitCommand`. Amble on Focus never runs. Pool does not select an Actor from `=` text. Scan-stop is right. ActorStart for `=` is not.

## Counts

(a) 0 / (b) 0 / (c) 1. Worst Spec-axis issue: Run on an `=` Command starts an Actor instead of Amble Run.
