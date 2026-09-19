# Standards review — 09 framework failure preserves children

Range: `99a13d9a...HEAD` (09 implement only). Mechanical scan (`--diff origin/staging` and `--diff 99a13d9a`): none. [TestActorCommandErrorTests](tests/Server.Tests/TestActorCommandErrorTests.fs) is 384 lines; [fsharp-source](.agents/rules/fsharp-source.md) file-size cap does not apply to tests. Stage `build` on [llm-connector](plan/llm-connector/project.md) matches [project status](doc/agents/project-status.md) First implement. Draft Events use `EventId.zero` per [core API](.agents/rules/core-api.md). No added long lines; no `mutable`; no new production Exceptions; 4-space indent.

## Documented standards (hard)

### [TestActorCommandErrorTests](tests/Server.Tests/TestActorCommandErrorTests.fs) — 40-line function

[fsharp-source](.agents/rules/fsharp-source.md): 40 lines or less per function. The test file-size note does not drop that rule. Scan skips `tests/` for 40-line discovery; this is from the file.

The Fact `TestActor non-hello preserves Focus Children and posts no Change` (lines 239–284) is 46 lines.

### [llm-connector project](plan/llm-connector/project.md) — refer by name

[refer-by-name](.agents/rules/refer-by-name.md): never refer by only the id; the name wraps the link.

New Notes line: `AI-Actor erase stays after [[issues/08-agent-ask-from-what-i-see.md|08]] and [[issues/12-replace-focus-children-from-reply.md|12]]`. Labels are ids only.

New Implementation tickets line: `AI-Actor erase deferred until 08 and 12`. Ids only; no names.

Pre-existing `[[path|label]]` wikilinks in plan/ are match-existing-style. [markdown-writing](.agents/rules/markdown-writing.md) labeled-link form is not a miss on those. New ticket and report links that wrap a name are fine.

## Baseline smells (judgement)

### Duplicated Code — seed helpers

`seedCommand` and new `seedCommandWithChildren` share mint, `EventId.zero` draft, `postGraphOnly`, and `requireOk`:

```
let event =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change ops }
let! posted =
    CoreMailbox.postGraphOnly host testCaller event
    |> Async.StartAsTask
requireOk "postChange" posted |> ignore
return commandId
```

### Duplicated Code — fail-and-wait block

The new Fact repeats the existing Theory start / wait / `ActorFailed` / live-row-gone shape:

```
let! start =
    CoreMailbox.startActor host testCaller request
    |> Async.StartAsTask
requireOk "startActor" start
let! stop = waitForActorStop host request.focusId 1000
match stop with
| Some ActorFailed -> ()
| other -> Assert.Fail($"expected ActorFailed, got {other}")
Assert.False(Set.contains request.focusId (pool.liveFocusIds ()))
```

## Not findings

No unused leftovers from this delta ([core-agent-behavior](.agents/rules/core-agent-behavior.md) Surgical). No consecutive blank lines in new Markdown. Arch hop 3 names [08 — Run Agent Actor calls CloudAgents](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) and [12 — Replace Focus Children from reply](plan/llm-connector/issues/12-replace-focus-children-from-reply.md). Surgical: test plus plan notes; no production edit.

## Counts

Hard: 3 (one 40-line function; two refer-by-name). Judgement smells: 2 (Duplicated Code). Worst hard: 46-line Fact.
