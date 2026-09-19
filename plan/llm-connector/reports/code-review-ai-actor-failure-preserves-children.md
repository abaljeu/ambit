# Code review — AI Actor half of [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md)

Independent two-axis review. Not approval. Ticket Status left `coded`. Do not treat this report as a `done` stamp.

**Verdict: Approve with nits.**

**Pin:** tip `640ad6d9` on `cursor/ai-actor-erase-proof-454b`. User named `origin/staging`; three-dot `origin/staging...HEAD` (merge-base `e9d59aa2`, equals current `origin/staging`). Subject: section **3. AI Actor does not erase data** and the Project rule **AI Actor does not erase data**.

**Commits:** `f2a9f706` Let CloudAgents setFake yield Failed; `392823c1` Prove Ask Failed preserves Focus Children; `640ad6d9` Mark AI Actor erase proof coded.

**Spec:** [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md) section **3. AI Actor does not erase data**. Arch Story path **Agent failure preserves children** hop 3, Locked **CloudAgents DLL interface**, **CloudAgents setFake**, and **Failure: no framework Changes, no erase** in [llm-connector architecture](plan/llm-connector/arch.md). Framework sections 1–2 already landed.

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` printed FILE-growth on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) 378→411. [fsharp-source](.agents/rules/fsharp-source.md) file-size cap does not apply to tests. measure-fs-size skips `tests/` for 40-line discovery.

**Focused tests (this review):** [AgentFailurePreserveTests](tests/Server.Tests/AgentFailurePreserveTests.fs) 1 passed; [AgentRunnerFakeTests](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs) 5 passed; [AgentAskTests](tests/Server.Tests/AgentAskTests.fs) 6 passed (`fakeReply` now wraps `Finished`).

Axis drafts: [Standards](code-review-standards-ai-actor-failure-preserves-children.md), [Spec](code-review-spec-ai-actor-failure-preserves-children.md). Axes stay separate below.

## Standards

Range: `git diff origin/staging...HEAD` (`e9d59aa2`..`640ad6d9`).

### Mechanical scan

```
tests/Server.Tests/AskCancelHarness.fs  .agents/rules/fsharp-source.md  FILE 378->411  already over 400 or new file over 400; change increased it
```

Scan exit 1. The 800/400 file-size rule in [fsharp-source](.agents/rules/fsharp-source.md) says it does not apply to tests. The printed hit is exempt. No new test member is over 40 lines.

### Findings

#### 1. Scan file size on AskCancelHarness (exempt)

Documented-standard hit from the scan, rule path [fsharp-source](.agents/rules/fsharp-source.md), FILE 378→411 on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs). The same rule text exempts tests. This is not a hard violation.

#### 2. Number-only 08 in ticket 09

Hard. [refer-by-name](.agents/rules/refer-by-name.md) requires number and name. New text in [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md) says `Amb replace landed on 08` and `Amb replace on 08`. Those mentions are number only.

#### 3. Duplicated Code: waitFailed

Judgement. [AgentRunnerFakeTests.fs](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs) adds `waitFailed` in the same shape as `waitFinished`:

```
| Ok(Failed msg) -> Some msg
| Ok Running when left > 0 ->
    Thread.Sleep 10
    spin (left - 10)
```

No hit on [core-api](.agents/rules/core-api.md) EventId serial or Core vs Adapter. No unused leftover from this change in [core-agent-behavior](.agents/rules/core-agent-behavior.md). [llm-connector project](plan/llm-connector/project.md) Stage stays `build`.

Hard 1, judgement 1, exempted scan 1. Worst in-axis: number-only 08 in ticket 09.

## Spec

Range: `git diff origin/staging...HEAD` (tip `640ad6d9`; fixed point `e9d59aa2`). Primary: [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md) section **3. AI Actor does not erase data** and the Project rule **AI Actor does not erase data**. Architecture in [llm-connector architecture](plan/llm-connector/arch.md): Story path **Agent failure preserves children**; Locked **CloudAgents DLL interface**, **CloudAgents setFake**, and **Failure: no framework Changes, no erase**.

### setFake lock

Old Locked **CloudAgents DLL interface** item 5: `setFake: (StartArgs -> AgentResult) option -> bool` (`Some` = deterministic fake, `None` = CursorAdapter). Module **CloudAgents** Interface item 2 used the same `AgentResult` signature. Locked **CloudAgents setFake** item 6 is unchanged: install/clear `option` handler on the DLL; success-path tests use `setFake (Some …)`; clear with `None`; do not put the fake switch on Ambit/Core.

This diff rewrites item 5 and Interface item 2 to `setFake: (StartArgs -> AgentStatus) option -> bool` and “Handler may yield `Finished` or `Failed`.”

Verdict: **both**. Intent holds: DLL `Some`/`None` switch; `start`/`poll`/`cancel`/`waitUntilComplete` stay; Core does not get the switch; a fake can yield `Failed`. Section **3. AI Actor does not erase data** item **2. Fail with `?ai`** requires “CloudAgents `setFake` yields Failed (not Finished).” `AgentResult` cannot express `Failed`. The same proof diff edits Locked item 5: unauthorized lock mutation. Leftover `AgentResult` remains on [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) item **4.2 `setFake` on the DLL** and Comments, and in [llm-connector project](plan/llm-connector/project.md) Notes: “Locked CloudAgents `setFake` on the DLL (`(StartArgs -> AgentResult) option -> bool`)”. `AgentStatus` also allows `Creating`, `Running`, and `Cancelled`; the rewritten lock names only `Finished` or `Failed`.

### (a) Missing or partial

None material. [AgentFailurePreserveTests](tests/Server.Tests/AgentFailurePreserveTests.fs) seeds two Focus Children, Runs `?ai` with `setFake` `Failed`, compares Child ids and text, keeps Change count at 1, asserts the provider string is not Node text, and drops via `liveFocusIds`. [RunAgentActor.fs](src/Server/RunAgentActor.fs) already maps `Failed` → `CompleteFailed` → `ActorFailed` and skips `postReplace`. The `setFake` type plus these tests is enough for this proof ticket.

### (b) Scope creep

1. **Unauthorized lock rewrite.** Ticket section **3. AI Actor does not erase data** item **2. Fail with `?ai`** asks for `setFake` to yield Failed. It does not ask to edit Locked **CloudAgents DLL interface**. See setFake lock.

### (c) Implemented but wrong

None. `pollFake` returns stored `AgentStatus` instead of wrapping `Finished`, so a handler can yield `Failed`.

One finding. Worst: unauthorized rewrite of Locked **CloudAgents DLL interface** item 5 (`AgentResult` → `AgentStatus`).

## Summary

Standards: 1 hard, 1 judgement, 1 exempted scan. Worst in-axis: number-only 08 in [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md).

Spec: 1 creep (lock rewrite; also faithful to Failed yield), 0 missing, 0 wrong. Worst in-axis: unauthorized rewrite of Locked **CloudAgents DLL interface** item 5 (`AgentResult` → `AgentStatus`).

**Recommendation: Approve with nits.** Section **3. AI Actor does not erase data** holds at this tip: `?ai` + `setFake` Failed leaves Focus Children (ids and text), posts no erase or response Change, writes no Agent-body Error text, drops via `liveFocusIds`. Nits are the number-only 08 mentions, duplicated `waitFailed`, leftover `AgentResult` on [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) and [llm-connector project](plan/llm-connector/project.md), and the in-place lock signature rewrite. Ticket Status left `coded`.
