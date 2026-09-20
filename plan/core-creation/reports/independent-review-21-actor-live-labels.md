# Independent review — Actor live labels

Reviewer did not write the implementation. Ticket [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Status stays `coded`. This report is not approval to land.

**Range:** `origin/staging...HEAD` (three-dot). Merge-base `0c716ef393688ae2755e60bcb468622f9e34fd26`. HEAD `d280520aeea34870442613e05606851b11e349d4` matches draft pull request [21: TitleCase live Actor result labels from Command](https://github.com/abaljeu/ambit/pull/80). Command: `git diff origin/staging...HEAD`. Diff is non-empty (9 files, +452 / −42).

**Spec:** [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Start/Stop result items; [core-creation architecture](plan/core-creation/arch.md) module **Actor live labels** and Seam **6a**; [Plan or doc change — Actor live labels](plan-or-doc-change-21-actor-live-labels.md). Runnable amendment: first owner-ancestor Focus→zoom whose text starts with `?` or contains `=`; do not skip `=` lines; TitleCase actor token for `?…`; generic when missing; `=` forms as architecture **Display label**.

**Commits** (`origin/staging..HEAD`):

- `d280520a` 21: Architecture Actor live labels first-runnable rule
- `7719b56d` 21: Stop Actor live scan at first runnable ? or =
- `1b84f451` 21: Mark architecture Actor live labels delivered
- `9b25c784` plan-or-doc-change: Actor live labels in architecture.
- `b88c51bd` 21: Record TitleCase lastCmdResult start-result checklist
- `4fc3062a` 21: Label live Actor results from Command TitleCase
- `3b91d2d0` 21: TitleCase command name for live result labels.
- `c6076c82` Mark 21 coded (failed review); cancel 50 into 21.
- `cc48a3cc` File core-creation 50: Actor live labels from Command node.

**Focused tests (green):** [ActorLiveTests](tests/Shared.Tests/ActorLiveTests.fs) — 12 passed (`dotnet test tests/Shared.Tests/Gambol.Shared.Tests.fsproj --filter FullyQualifiedName~ActorLive`).

Axis files: [Standards axis](code-review-standards-21-actor-live-labels.md), [Spec axis](code-review-spec-21-actor-live-labels.md). Axes stay separate below.

## Mechanical scan

`python` is not on PATH. Command used: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`

```
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:3  .agents/rules/refer-by-name.md  BARE_ID  Ticket 50
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:5  .agents/rules/refer-by-name.md  BARE_ID  #79
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:11  .agents/rules/refer-by-name.md  BARE_ID  ticket 21
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:11  .agents/rules/refer-by-name.md  BARE_ID  ticket 50
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:18  .agents/rules/refer-by-name.md  BARE_ID  ticket 14
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:35  .agents/rules/refer-by-name.md  BARE_ID  Ticket 21
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:36  .agents/rules/refer-by-name.md  BARE_ID  Ticket 50
plan/core-creation/reports/plan-or-doc-change-21-actor-live-labels.md:46  .agents/rules/refer-by-name.md  BARE_ID  #79
--- measure-fs-size ---
src/Shared/ActorLive.fs::titleCaseName: lines 67-72 (6 lines)
src/Shared/ActorLive.fs::isRunnableText: lines 73-75 (3 lines)
src/Shared/ActorLive.fs::firstToken: lines 76-81 (6 lines)
src/Shared/ActorLive.fs::equalsNameLabel: lines 82-90 (9 lines)
src/Shared/ActorLive.fs::labelFromText: lines 91-99 (9 lines)
src/Shared/ActorLive.fs::runnableTextOnPath: lines 101-113 (13 lines)
src/Shared/ActorLive.fs::displayLabel: lines 114-121 (8 lines)
src/Shared/ActorLive.fs::startResult: lines 122-131 (10 lines)
src/Shared/ActorLive.fs::resultOf: lines 132-152 (21 lines)
src/Shared/ActorLive.fs::lastCmdResult: lines 153-164 (12 lines)
```

## Standards

Range: `origin/staging...HEAD` (merge-base `0c716ef3`, HEAD `d280520a`). Code: [`ActorLive.fs`](src/Shared/ActorLive.fs), [`UpdateActorLive.fs`](src/Client/UpdateActorLive.fs), [`ActorLiveTests.fs`](tests/Shared.Tests/ActorLiveTests.fs), [`AgentAuthErrorTests.fs`](tests/Server.Tests/AgentAuthErrorTests.fs). Plan markdown in the same range.

### Hard violations

**[Refer by name](.agents/rules/refer-by-name.md)** — mechanical scan `BARE_ID` hits in [plan-or-doc-change — Actor live labels](plan-or-doc-change-21-actor-live-labels.md): line 3 `Ticket 50`; line 5 `#79`; line 11 `ticket 21` and `ticket 50`; line 18 `ticket 14`; line 35 `Ticket 21`; line 36 `Ticket 50`; line 46 `#79`. Same rule on other new plan text: [Core creation](../project.md) notes and Issues list say “folded into 21”; [50 — Actor live result labels from Command node](../issues/50-actor-live-labels-from-command.md) has “(14 stays)”, “llm-connector 17/18”, “+ 21 rework”, link label `[21]`, and “21 done”.

**[Markdown writing](.agents/rules/markdown-writing.md)** — labeled links must be `[label](path)`, not Obsidian `[[path|label]]`. New [Core creation](../project.md) notes use `[[issues/…|21]]` and `[[issues/…|50]]` with number-only labels. Changed See also on [21 — Client shows live Actor](../issues/21-client-shows-lock-present.md) uses `[[../arch.md|core-creation architecture]]`. [50 — Actor live result labels from Command node](../issues/50-actor-live-labels-from-command.md) says “Client” for the Browser project ([CONTEXT.md](CONTEXT.md) **Browser**).

**[Planning docs](.agents/rules/planning-docs.md)** — plan text must not discuss branches as delivery. [plan-or-doc-change — Actor live labels](plan-or-doc-change-21-actor-live-labels.md) names workplace branch `cursor/actor-live-cmd-result-label` and “draft PR #79”.

**[F# source](.agents/rules/fsharp-source.md)** — group related parameters into a named reused type. `graph`, `focusId`, and `zoomRoot` travel together through `runnableTextOnPath`, `displayLabel`, and `startResult`; `lastCmdResult` and `resultOf` lengthen the list with `graph` plus `zoomRoot` instead of one scan-context type.

### Judgement (smells)

**Duplicated Code** — `firstToken` in [`ActorLive.fs`](src/Shared/ActorLive.fs) repeats private `firstToken` in [`CommandRequest.fs`](src/Shared/CommandRequest.fs). Test fixtures `graphWithCommand`, `graphWithCommandChild`, and `graphWithCommandMidChild` repeat the same `Graph.replace` / `createNodes` shape.

**Mysterious Name** — private `resultOf` does not say it maps one Event to a `CmdLastResult`; the stop path binds `chip` while start uses `label`.

### Clean

Function sizes are under 40 lines (`resultOf` is 21). No added line over 100 characters. [`ActorLive.fs`](src/Shared/ActorLive.fs) is 164 lines (under 800). No tabs. The owner walk uses `GraphQuery.enclosing` ([F# source](.agents/rules/fsharp-source.md) GraphQuery-first). [`UpdateActorLive.fs`](src/Client/UpdateActorLive.fs) is a one-line call-site change. Tests’ `failwith` matches existing Shared test style. [Core API](.agents/rules/core-api.md) is not in this diff. [No retrofit](.agents/rules/no-retrofit.md): [14 — Provider-named AI errors](../../llm-connector/issues/14-provider-named-ai-errors.md) stays done.

## Spec

Range: `git diff origin/staging...HEAD` (HEAD `d280520aeea34870442613e05606851b11e349d4`). Specs: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Sequence + Start/Stop; [core-creation architecture](plan/core-creation/arch.md) **Actor live labels**; [Plan or doc change — Actor live labels](plan-or-doc-change-21-actor-live-labels.md). [50 — Actor live result labels from Command node](plan/core-creation/issues/50-actor-live-labels-from-command.md) is historical; architecture and 21 — Client shows live Actor win. This axis does not score Standards.

### 1. Missing or partial

None on this rework. Architecture **Runnable node**: “from Focus, scan owner-parents toward zoom root (inclusive of both); first node whose text starts with literal `?` or contains `=`. Do not skip an `=` node to reach a `?` above.” [ActorLive.fs](src/Shared/ActorLive.fs) `runnableTextOnPath` walks the owner chain with `GraphQuery.enclosing`, stops at the first `?` or `=` or at zoom, and does not skip an `=` node. **Display label**: “`?` form → TitleCase of the actor name token (same token rule as CommandRequest actor select). `=` form → TitleCase of that line’s name token if present, else generic. No runnable on the path → generic (not a product name).” The code uses `CommandRequest.actorNameFromText` then first-letter TitleCase; `count=1+2` → Count; `=1+2` → generic. Uses: “ActorStart / ActorStop Focus (and ActorStart zoomId when present); Client zoom root on stop.” Start uses `start.focusId` and `start.zoomId`; stop uses the stop Focus and [UpdateActorLive.fs](src/Client/UpdateActorLive.fs) `model.zoomRoot`. 21 — Client shows live Actor Start: “Client `lastCmdResult` … **“Run: <Label> started.”**” lands as `Detail (Some "Run", $"{label} started.")`. Stop: “same Label rule … success detail if useful; on `ActorFailed` / `ActorCancelled`, an Error … Conveyance only.” Succeeded/Failed/Cancelled still set Detail/Error with that Label as chip; provider text is unchanged. Projection, chrome, and boot are not in this diff.

### 2. Scope creep

None in product code. The `=` runnable bound and TitleCase helpers are architecture **Runnable node** and **Display label**. Filing [50 — Actor live result labels from Command node](plan/core-creation/issues/50-actor-live-labels-from-command.md) then cancelling it is not product behaviour.

### 3. Looks implemented but wrong

None against architecture and 21 — Client shows live Actor. `?test` → Test. `?ai` → Ai, which is TitleCase of the CommandRequest token `ai`, not a fixed product chip (**Command chip**: “not a fixed product name”). Tests cover “equals ancestor stops scan and does not use `?` above.”

### 4. Summary

(a) 0, (b) 0, (c) 0. Worst in Spec: none.

## Summary

Standards: 4 hard-violation groups plus 2 smell judgements; worst is [refer-by-name](.agents/rules/refer-by-name.md) bare ids in write-once plan text (not product code). Spec: 0 findings; worst: none.

## Verdict

**Approve with nits.** Product code matches architecture **Actor live labels** and [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Start/Stop conveyance. No must-fix blocker. Plan-text nits and the scan-context parameter type are optional follow-ups; the plan-or-doc-change report is write-once so those bare ids stay. Do not squash-land onto staging from this report. Status stays `coded`.
