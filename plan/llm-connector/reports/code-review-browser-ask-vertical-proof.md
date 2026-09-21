# Code review: Browser Ask from what I see

Independent two-axis review of [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md). Range: three-dot `origin/staging...HEAD` (merge-base `10b35647`; tip `cad5a1f1`). Spec: that ticket plus [llm-connector architecture](plan/llm-connector/arch.md) Story path **Agent ask from what I see**, Locked **Vertical proof timing**, **Agent Command spelling**, **CloudAgents setFake**, and **Live Actor chrome**. Subject: Browser-shaped `?ai` harness via `CommandRequest.oneNodeStart`, `setFake` Finished, Poll / Graph Focus Children, ActorStarted then ActorFinished, `liveFocusIds` drop. Axis drafts: [Standards axis](code-review-standards-browser-ask-vertical-proof.md), [Spec axis](code-review-spec-browser-ask-vertical-proof.md). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` printed FILE-growth on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) 411→476 (size rule does not apply to tests). **Status:** stays `coded`. A report is not approval.

## Verdict

**Needs work.** The in-scope success path is present: `launchBrowserAsk` builds `ActorStart` with `CommandRequest.oneNodeStart`, CloudAgents `setFake` returns Finished, Graph Focus Children become the fake reply, ActorStarted then ActorFinished appear, and `expectActorSucceeded` drops the live Focus id. Non-goals stay out (no Client encode, no live-Actor chrome, no pack format, no Cancel or Failed). Worst Standards hit is [map.md](plan/llm-connector/map.md) **Not yet specified** listing this coded ticket (and other delivered tickets) in the unspecified-work section. Worst Spec hit is Poll Focus Children: `hasReplace` can pass on the seed Change that `launchBrowserAsk` posts after the test snapshots `getEventId`.

## Standards

Range: three-dot `origin/staging...HEAD` (`cad5a1f1`). Scan printed one line:

```
tests/Server.Tests/AskCancelHarness.fs  .agents/rules/fsharp-source.md  FILE 411->476  already over 400 or new file over 400; change increased it
```

No function-size lines. Product code is unchanged. New public harness names are `launchBrowserAsk`, `pollEventsSince`, and `expectOneNodeStart`. `seedBrowserCommand` and `startBrowserAsk` are `private`. No ticket numbers in module, file, or test names.

### Hard violations

#### 1. Coded work listed as not yet specified — [planning-docs.md](.agents/rules/planning-docs.md) (one concern per section)

[map.md](plan/llm-connector/map.md) section **Not yet specified** now records [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md) as Status `coded`, and it drops the separate vertical-proof line. That section is unspecified work; the changed lines are coded and done tickets.

### Scan exemption (not a violation)

#### File size — [fsharp-source.md](.agents/rules/fsharp-source.md)

[AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) FILE 411→476. The cited rule says the 800/400 file-size split does not apply to tests; tests split to match source files.

### Judgement-call smells

**Duplicated Code.** New `seedBrowserCommand` in [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) repeats the Browser `EventBody.Change` plus `postGraphOnly` shape already in `seedCommand` and `seedAskTree`.

**Middle Man.** `pollEventsSince` only forwards `CoreMailbox.getEventsSince` through `Async.StartAsTask`. [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs) already calls `CoreMailbox.getEventId` the same way.

**Mysterious Name.** `hasReplace` is true for any `EventBody.Change _` on the Poll list, not a Focus-child replace.

Checks that passed: seed Events use `EventId.zero` ([core-api.md](.agents/rules/core-api.md)); [project.md](plan/llm-connector/project.md) Stage stays `build`; changed plan links are `[label](path)` with names; one new proof file plus harness helpers is surgical; unedited labeled wikilinks on the ticket stay ([no-retrofit.md](.agents/rules/no-retrofit.md)).

Hard documented: 1. Judgement smells: 3. Scan exemption: 1. Worst hard: coded ticket listed under **Not yet specified**.

## Spec

Spec: [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md); architecture Story path **Agent ask from what I see**, Locked **Vertical proof timing**, **Agent Command spelling**, **CloudAgents setFake**, and **Live Actor chrome** in [llm-connector architecture](plan/llm-connector/arch.md). Range: `origin/staging...HEAD`.

Covered without a finding: `CommandRequest.oneNodeStart` in [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs); CloudAgents `setFake` Finished via `withFake` / `fakeReply`; Graph Focus Children via `expectOwnedTexts`; ActorStarted then ActorFinished via `lifecycleIndexes` on `ActorStart` / `ActorStop ActorSucceeded`; `liveFocusIds` drop inside `expectActorSucceeded`. Launch uses `testCaller` (`Authority "Test"`). Seed Change uses `Authority "Browser"`. Spec: "Browser or harness submits `ActorStart`". Non-goals not added: Live-Actor chrome; new Client `ActorStart` encode; mixed-format owning-codec / nested-tag pack; Cancel or Failed paths.

### (a) Missing or partial

#### 1. Poll Focus Children

Spec: "Poll / Graph shows the new Focus Children under Focus." Graph is proven in [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs) with `expectOwnedTexts` → `getState`. Poll does not read Focus Children. `hasReplace` only requires some `EventBody.Change`.

### (b) Not asked for

None in product behavior. The diff is harness helpers, [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs), and plan Status notes.

### (c) Implemented but wrong

#### 2. Poll Change may be the seed

Spec: "after success, Poll / Graph shows the new Focus Children under Focus." [BrowserAskProofTests.fs](tests/Server.Tests/BrowserAskProofTests.fs) snapshots `getEventId`, then `launchBrowserAsk` posts a seed Change before `startActor`. `pollEventsSince` includes that seed. `hasReplace` can pass if the Run Agent never posts a replace Change. Graph children still fail closed via `expectOwnedTexts`.

Two findings. Worst: Poll Change may be the seed.

## Summary

Standards: 1 hard, 3 smells, 1 scan exemption — worst: [map.md](plan/llm-connector/map.md) **Not yet specified** lists coded work. Spec: 2 findings — worst: `hasReplace` can pass on the seed Change.

## Re-review (`dfb4b7de`)

Both prior Needs-work items are **fixed**. Full write-up: [code-review-browser-ask-vertical-proof-rereview](code-review-browser-ask-vertical-proof-rereview.md). Verdict on that tip: **Good**. Ticket Status stays `coded`.
