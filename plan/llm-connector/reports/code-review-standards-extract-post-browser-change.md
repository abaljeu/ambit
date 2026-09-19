# Standards — Extract postBrowserChange

Range: `git diff origin/staging...HEAD` (`33541cce`). Subject: [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). Mechanical scan: none (empty stdout).

## Hard violations

None.

The scan printed no FILE-growth hit. [fsharp-source.md](.agents/rules/fsharp-source.md) exempts tests from the 800/400 file split. The file shrank by extraction. Do not invent a FILE-growth finding.

`postBrowserChange` is 13 lines (198–210). Call sites stay under 40: `seedAskTree` 24, `seedCommand` 12, `seedBrowserCommand` 18. Added lines stay under 100 characters. No `mutable`. No new Exceptions. The helper is `private`; the public-name rule does not apply. The name is already more than one word.

[core-api.md](.agents/rules/core-api.md): the draft Event uses `EventId.zero`. Authority is Browser. Body is a Change of Ops.

[core-agent-behavior.md](.agents/rules/core-agent-behavior.md): the helper replaces three identical `postGraphOnly` seed posts. `postOwnedChild` still uses `CoreMailbox.postEvents` and stays untouched. That is a surgical edit.

No [markdown-writing.md](.agents/rules/markdown-writing.md), [planning-docs.md](.agents/rules/planning-docs.md), [refer-by-name.md](.agents/rules/refer-by-name.md), or [no-retrofit.md](.agents/rules/no-retrofit.md) hit. The range has no plan text.

## Smells (judgement, not hard)

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
