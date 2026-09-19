# Re-review: Browser Ask from what I see

Independent two-axis re-review of [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md) after implement fixes. Range: three-dot `origin/staging...HEAD` (merge-base `10b35647`; tip `dfb4b7de`). Prior report: [code-review-browser-ask-vertical-proof](code-review-browser-ask-vertical-proof.md). Axis drafts: [Standards axis](code-review-standards-browser-ask-vertical-proof-rereview.md), [Spec axis](code-review-spec-browser-ask-vertical-proof-rereview.md). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` printed FILE-growth on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) 411→486 (size rule does not apply to tests). Focused test `BrowserAskProofTests` passed (1/1). **Status:** stays `coded`. A report is not approval.

## Prior findings

1. **Spec — Poll seed window.** `hasReplace` could pass on the seed Change; Poll did not prove Focus Children from the Agent reply. **Fixed.** `launchBrowserAsk` returns `pollAfter` / `graphAfterSeed` after seed and before `startActor`. The proof applies that Poll tail with `Ev.apply` and asserts owned children `[ "from-agent" ]`. `hasReplace` is gone.
2. **Standards — map section.** Coded ticket listed under **Not yet specified**. **Fixed.** [map.md](plan/llm-connector/map.md) **Implementation** lists coded/done tickets; **Not yet specified** is now none.

## Verdict

**Good.** Worst remaining on Standards is judgement Duplicated Code on the seed envelope. Spec has no findings.

## Standards

Range: three-dot `origin/staging...HEAD` (`dfb4b7de`). Scan printed one line:

```
tests/Server.Tests/AskCancelHarness.fs  .agents/rules/fsharp-source.md  FILE 411->486  already over 400 or new file over 400; change increased it
```

No function-size lines. Product code is unchanged.

### Prior map finding

**Fixed.** [map.md](plan/llm-connector/map.md) **Implementation** lists [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md) (`coded`) and the other coded/done tickets with names wrapping links. **Not yet specified** is now "None — arch grill closed..."

### Hard violations

None. Plan hunks use labeled `[name](path)` links. Stage stays `build`. Public harness face: `launchBrowserAsk`, `BrowserAskLaunch`, `pollEventsSince`, `expectOneNodeStart`. `seedBrowserCommand` and `startBrowserAsk` are `private`. No ticket numbers in module, file, or test names.

### Scan exemption (not a violation)

#### File size — [fsharp-source.md](.agents/rules/fsharp-source.md)

[AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) FILE 411→486. The 800/400 file-size split does not apply to tests.

### Judgement-call smells

**Duplicated Code.** `seedBrowserCommand` repeats the `seedAskTree` / `seedCommand` post envelope (`EventId.zero`, `Authority "Browser"`, `postGraphOnly`). Ops differ (Command owns the note; no Zoom). Surgical match of existing seed helpers.

**Middle Man.** `pollEventsSince` only forwards `CoreMailbox.getEventsSince`. Named as intended module face; suppress.

Hard documented: 0. Judgement smells: 2. Scan exemption: 1. Prior map finding: fixed. Worst remaining: Duplicated Code on the seed envelope.

## Spec

Spec: [13 — Vertical proof: Browser Ask from what I see](plan/llm-connector/issues/13-vertical-proof-browser-ask.md); architecture Story path **Agent ask from what I see**, Locked **Vertical proof timing**, **Agent Command spelling**, **CloudAgents setFake**, and **Live Actor chrome** in [llm-connector architecture](plan/llm-connector/arch.md). Range: `origin/staging...HEAD`.

### Prior Poll finding

**Fixed.** Ticket: "Poll / Graph shows the new Focus Children under Focus." `launchBrowserAsk` snapshots `pollAfter` / `graphAfterSeed` after the Browser seed Change and before `startActor`. `pollEventsSince` is `getEventsSince` after that id, so the seed is outside the Poll window. `applyPollChanges` folds only `EventBody.Change` through `Ev.apply` (ActorStart/Stop: `Ev.ops` is `None`). The proof asserts Focus owned children `[ "from-agent" ]` on that Poll graph, then the same texts on Graph `getState`. `hasReplace` is gone.

### Coverage

1. **Browser Authority** — Ticket allows "Browser or harness." Seed Event `authority` is Browser. Launch Caller is `testCaller` (Authority Test).
2. **CommandRequest.oneNodeStart** — `startBrowserAsk` builds `ActorStart` with that encode for `?ai`. `expectOneNodeStart` checks one-Node Zoom/Focus/Command.
3. **setFake Finished** — `withFake` + `fakeReply "from-agent"`; `None` in `finally`.
4. **Focus Children** — Poll-applied graph and Graph state both show `from-agent`. Prompt contains `visible-context`.
5. **ActorStarted then ActorFinished** — Poll tail, chrono `ActorStart` then `ActorStop ActorSucceeded` for Focus.
6. **liveFocusIds drop** — `expectActorSucceeded` → `expectLiveGone`.
7. **Non-goals** — No live-Actor chrome; no new Client encode; no mixed-format/nested-tag pack; no Cancel or Failed path.

### (a) Missing or partial

None.

### (b) Not asked for

None of product behavior.

### (c) Implemented but wrong

None.

Zero findings. Worst: none. Prior Poll finding is fixed.

## Summary

Standards: 0 hard, 2 smells, 1 scan exemption — worst remaining: Duplicated Code on the seed envelope. Spec: 0 findings — worst: none. Both prior Needs-work items are fixed.
