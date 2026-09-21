# Standards re-review — Browser Ask vertical proof

Range: `git diff origin/staging...HEAD` (`cad5a1f1`, `dfb4b7de`). Sources: [fsharp-source.md](.agents/rules/fsharp-source.md), [core-api.md](.agents/rules/core-api.md), [core-agent-behavior.md](.agents/rules/core-agent-behavior.md), [markdown-writing.md](.agents/rules/markdown-writing.md), [refer-by-name.md](.agents/rules/refer-by-name.md), [planning-docs.md](.agents/rules/planning-docs.md), [project-stage.md](.agents/rules/project-stage.md), [no-retrofit.md](.agents/rules/no-retrofit.md), [project-values.md](.agents/rules/project-values.md), [environment.md](.agents/rules/environment.md). Smells are judgement only.

## Prior finding

**1. Map listed coded tickets under Not yet specified** — **fixed.** [map.md](plan/llm-connector/map.md) **Implementation** lists [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md) (`coded`) and the other coded/done tickets with names wrapping links. **Not yet specified** is now "None — arch grill closed..."

## Mechanical scan

Stdout: [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) FILE 411→486 citing [fsharp-source.md](.agents/rules/fsharp-source.md). **Not a hard violation.** That rule does not apply to tests; extra surface check repeats file-size 400/800 does not apply to tests. No function-size or long-line hits.

## Hard violations

None. Plan hunks in [map.md](plan/llm-connector/map.md), [project.md](plan/llm-connector/project.md), and [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md) use labeled `[name](path)` links, no consecutive blank lines, no branch-as-delivery status, Stage stays `build`.

## Extra surface

Public face added on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs): `launchBrowserAsk`, `BrowserAskLaunch`, `pollEventsSince`, `expectOneNodeStart`. Helpers `seedBrowserCommand` and `startBrowserAsk` are `private`. [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs) module, file, and test names have no ticket numbers.

## Judgement smells

**2. Duplicated Code** — [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) `seedBrowserCommand` repeats the `seedAskTree` / `seedCommand` post envelope:

```
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change ops }
        let! posted =
            CoreMailbox.postGraphOnly host testCaller event
```

Ops differ (Command owns the note; no Zoom). Matches existing seed helpers ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) surgical). Not hard.

**3. Middle Man** — same file:

```
let pollEventsSince host afterId =
    CoreMailbox.getEventsSince host afterId
    |> Async.StartAsTask
```

Thin wrap. Extra surface check names `pollEventsSince` as intended module face; suppress.

## Summary

Hard: 0. Prior map finding: fixed. Worst remaining: judgement **2. Duplicated Code** on the seed envelope.
