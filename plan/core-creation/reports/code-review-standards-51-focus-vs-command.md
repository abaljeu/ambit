# Standards axis: [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md)

Range `git diff origin/staging...HEAD`. HEAD `6570a866` matches draft PR 81. `origin/staging` `b484d99e`. Diff 10 files, +370 / −33. Non-empty.

## Mechanical scan

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

## Hard violations

**Refer by name** ([.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md)): scan BARE_ID `Ticket 51` in [plan-or-doc-change-51-focus-vs-command.md](plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md) lines 30, 44, 48. Same file line 23 `[51](...)` and [llm-connector project.md](plan/llm-connector/project.md) `[51](...)` use the id with no ticket name.

**Labeled links** ([.agents/rules/markdown-writing.md](.agents/rules/markdown-writing.md)): [core-creation project.md](plan/core-creation/project.md) filed note uses Obsidian `[[issues/51-browser-run-focus-vs-command.md|51 Browser Run Focus vs Command]]`.

## Not-hit

FILE 481→538 on [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs): [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) 800/400 does not apply to tests. Tests match source: [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) for Shared; TestActor proof stays with hello tests.

Measured functions stay under 40 lines. No new 100-character F# lines. Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

[.agents/rules/core-api.md](.agents/rules/core-api.md): Browser passes VM `eventId`; it does not mint stored serials. [.agents/rules/planning-docs.md](.agents/rules/planning-docs.md), [.agents/rules/project-stage.md](.agents/rules/project-stage.md), [.agents/rules/no-retrofit.md](.agents/rules/no-retrofit.md): no hit.

## Judgement-call smells

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

## Counts

Hard 6 (3 scan BARE_ID, 2 nameless `[51]` links, 1 Obsidian labeled link). Judgement 3. Worst: refer-by-name BARE_ID on the plan-or-doc-change report.
