# Standards review: Browser Ask from what I see

Range `origin/staging...HEAD`. Scan stdout (one line): [AskCancelHarness](tests/Server.Tests/AskCancelHarness.fs) FILE 411→476, rule [fsharp-source](.agents/rules/fsharp-source.md). No function-size lines.

## 1. File size increase on a test harness (scan hit; not a violation)

The scan treats 411→476 as a file-size hit. The cited rule in [fsharp-source](.agents/rules/fsharp-source.md) says the 800/400 file-size split does not apply to tests; tests split to match source files. Extra surface check repeats that exemption.

## 2. Coded work listed as not yet specified (hard)

[map.md](plan/llm-connector/map.md) section Not yet specified now records [Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md) as Status `coded`, and it drops the separate vertical-proof line. [planning-docs](.agents/rules/planning-docs.md) requires one concern per section. That section is unspecified work; the changed lines are coded and done tickets.

## 3. Duplicated Code (judgement)

New `seedBrowserCommand` in [AskCancelHarness](tests/Server.Tests/AskCancelHarness.fs) repeats the Browser `EventBody.Change` plus `postGraphOnly` shape already in `seedCommand` and `seedAskTree`:

```
let event =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change ops }
```

Compose `seedCommand` then `postOwnedChild` for the visible-context node.

## 4. Middle Man (judgement)

```
let pollEventsSince host afterId =
    CoreMailbox.getEventsSince host afterId
    |> Async.StartAsTask
```

[BrowserAskProofTests](tests/Server.Tests/BrowserAskProofTests.fs) already calls `CoreMailbox.getEventId` the same way. Extra surface permits a small public face (`launchBrowserAsk`); this wrapper only forwards.

## 5. Mysterious Name (judgement)

`hasReplace` is true for any `EventBody.Change _` on the Poll list, not a Focus-child replace.

## Checks that passed

`seedBrowserCommand` and `startBrowserAsk` are `private`. Public names: `launchBrowserAsk`, `pollEventsSince`, `expectOneNodeStart`. No ticket numbers in module, file, or test names. Seed Events use `EventId.zero` ([core-api](.agents/rules/core-api.md)). [project.md](plan/llm-connector/project.md) Stage stays `build` ([project-stage](.agents/rules/project-stage.md)). Changed plan links are `[label](path)` with names ([markdown-writing](.agents/rules/markdown-writing.md), [refer-by-name](.agents/rules/refer-by-name.md)). One new proof file plus harness helpers is surgical ([core-agent-behavior](.agents/rules/core-agent-behavior.md)). Unedited labeled wikilinks on the ticket stay ([no-retrofit](.agents/rules/no-retrofit.md)).

Hard documented: 1. Judgement smells: 3. Scan exemption: 1.
