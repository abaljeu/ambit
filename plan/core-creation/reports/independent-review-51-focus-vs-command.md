# Independent review — [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md)

Reviewer did not write the implementation. Axis files [Standards axis: 51 Focus vs Command](code-review-standards-51-focus-vs-command.md) and [Spec review — 51 Focus vs Command on Run](code-review-spec-51-focus-vs-command.md) are parallel opinions. This report is not approval. Ticket Status stays `coded`. Do not land onto staging or ready.

**Range:** `origin/staging...HEAD` at `6570a866318ce02ecc80c29ef9096e75d67f9d82`. Base `origin/staging` `b484d99ef9b3afd76dc706b740f4a5a35f5c9144`. Command: `git diff origin/staging...HEAD`. Diff is non-empty (10 files, +370 / −33). Tip matches draft PR [51: Focus vs Command on product Run](https://github.com/abaljeu/ambit/pull/81) `headRefOid`.

**Spec:** [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md). [core-creation architecture](plan/core-creation/arch.md) **Browser Run** product item (distinct `focusId` / `commandId` / `zoomId`). [llm-connector architecture](plan/llm-connector/arch.md) Locked **Focus vs Command on Run**. [Plan or doc change — Focus vs Command on Run](plan-or-doc-change-51-focus-vs-command.md). Runnable: first Focus→Zoom owner with text starting with `?` or containing `=`; do not skip `=`.

**Commits** (`origin/staging..HEAD`):

- `6570a866` 51: encode distinct Focus, Command, and Zoom on Run.
- `3c5943a5` 51: Type coding.
- `748702b9` Amend: runnable Command is ? or contains =; scan stops.
- `bdceba60` plan-or-doc-change: Focus vs Command on product Run.

Focused tests (Spec axis): `CommandRequestTests` 16 passed; `TestActorHelloTests` 9 passed.

## Standards

Standards axis only. Range as pinned. Scan command: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (`python` is not on PATH).

### Mechanical scan

```
plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md:30  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md:44  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md:48  .agents/rules/refer-by-name.md  BARE_ID  Ticket 51
tests/Server.Tests/TestActorHelloTests.fs  .agents/rules/fsharp-source.md  FILE 481->538  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Client/Commands.fs::execAmbleRunOp: lines 62-85 (24 lines)
src/Shared/CommandRequest.fs::isRunnableText: lines 12-14 (3 lines)
src/Shared/CommandRequest.fs::noRunnableCommand: lines 45-47 (3 lines)
src/Shared/CommandRequest.fs::ownerPathToZoom: lines 48-65 (18 lines)
src/Shared/CommandRequest.fs::firstRunnable: lines 66-72 (7 lines)
src/Shared/CommandRequest.fs::commandOnOwnerPath: lines 74-81 (8 lines)
src/Shared/CommandRequest.fs::tryStart: lines 83-99 (17 lines)
```

### Hard violations

**Refer by name** ([.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md)): scan BARE_ID `Ticket 51` in [plan-or-doc-change-51-focus-vs-command.md](plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md) lines 30, 44, 48. Same file line 23 `[51](...)` and [llm-connector project.md](plan/llm-connector/project.md) `[51](...)` use the id with no ticket name.

**Labeled links** ([.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md)): [core-creation project.md](plan/core-creation/project.md) filed note uses Obsidian `[[issues/51-browser-run-focus-vs-command.md|51 Browser Run Focus vs Command]]`.

### Not-hit

FILE 481→538 on [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs): [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) 800/400 does not apply to tests. Tests match source: [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) for Shared; TestActor proof stays with hello tests.

Measured functions stay under 40 lines. No new 100-character F# lines. Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

[.agents/rules/core-api.md](.agents/rules/core-api.md): Browser passes VM `eventId`; it does not mint stored serials. [.agents/rules/planning-docs.md](.agents/rules/planning-docs.md), [.agents/rules/project-stage.md](.agents/rules/project-stage.md), [.agents/rules/no-retrofit.md](.agents/rules/no-retrofit.md): no hit.

### Judgement-call smells

**Duplicated Code / Feature Envy:** `ownerPathToZoom` copies [GraphQuery.enclosing](src/Shared/GraphQuery.fs) owner-walk. Quote:

```
match Map.tryFind current graph.ownerParentByChild with
| None -> None
| Some parentId ->
    collect (current :: acc) parentId (Set.add current visited)
```

Enclosing has no Zoom stop, so a path collect can stay.

**Parameter Explosion:** `tryStart` takes `graph`, `siteMap`, `zoomId`, `focusId`, `eventId` — `oneNodeStart` plus distinct Focus.

**Mysterious Name (mild):** issues-list title uses `?` ancestor, not the ticket name runnable ancestor.

Standards axis: 6 hard (3 scan BARE_ID, 2 nameless `[51]` links, 1 Obsidian labeled link). Judgement 3. Worst: refer-by-name BARE_ID on the plan-or-doc-change report.

**Verdict (Standards):** Approve with nits.

## Spec

Spec: ticket What to build plus **Browser Run** product item and Locked **Focus vs Command on Run**.

### (a) Missing or partial

None. Client Run sets `focusId` from selection Focus, `commandId` from Focus→Zoom owner-scan, `zoomId` to the Included Zoom root. No Command and not Amble → Run Error. Shared `tryStart` and Server TestActor prove distinct `commandId` / `focusId`, hello Owned Child under the question Focus, and question text kept. Non-goals (Amb replace vs stream, Actor live label TitleCase, Core door widen) are not in the code diff.

### (b) Behaviour not asked for

None in product code. `execAmbleRunOp` is the old Amble path when `tryStart` is Error. `oneNodeStart` stays as the hello helper.

### (c) Implemented but wrong

1. **`=` Command starts an Actor.** Locked **Focus vs Command on Run** (item 8): "Command is the nearest runnable owner-ancestor that selects the Actor." Same item: stop at the first runnable node — "do not skip a `=` line to reach a `?` above it." [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md) encode 2: set `commandId` from `?` or `=`. **Browser Run** Interface item 4: "otherwise AmbleRun". `isRunnableText` makes `tryStart` Ok for `x = 1`. Client then `SubmitCommand`. Amble on Focus never runs. Pool does not select an Actor from `=` text. Scan-stop is right. ActorStart for `=` is not.

Spec axis: (a) 0 / (b) 0 / (c) 1. Worst: Run on an `=` Command starts an Actor instead of Amble Run.

**Verdict (Spec):** Needs work.

## Summary

Standards: 6 hard (doc name/link nits), 3 judgement; worst: BARE_ID `Ticket 51` in [plan-or-doc-change-51-focus-vs-command.md](plan-or-doc-change-51-focus-vs-command.md). Spec: 0 missing, 0 creep, 1 wrong; worst: `=` Command encodes ActorStart so Amble Run does not run.

**For Alan:** Needs work on Spec (`=` vs Amble). Standards nits only. Status stays `coded`. Do not squash-land.
