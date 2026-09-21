# Spec axis: AI Actor does not erase data

Range: `git diff origin/staging...HEAD` (tip `640ad6d9`; fixed point `e9d59aa2`). Primary: [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md) section **3. AI Actor does not erase data** and the Project rule **AI Actor does not erase data**. Architecture in [llm-connector architecture](plan/llm-connector/arch.md): Story path **Agent failure preserves children**; Locked **CloudAgents DLL interface**, **CloudAgents setFake**, and **Failure: no framework Changes, no erase**.

## setFake lock

Old Locked **CloudAgents DLL interface** item 5: `setFake: (StartArgs -> AgentResult) option -> bool` (`Some` = deterministic fake, `None` = CursorAdapter). Module **CloudAgents** Interface item 2 used the same `AgentResult` signature. Locked **CloudAgents setFake** item 6 is unchanged: install/clear `option` handler on the DLL; success-path tests use `setFake (Some …)`; clear with `None`; do not put the fake switch on Ambit/Core.

This diff rewrites item 5 and Interface item 2 to `setFake: (StartArgs -> AgentStatus) option -> bool` and “Handler may yield `Finished` or `Failed`.”

Verdict: **both**. Intent holds: DLL `Some`/`None` switch; `start`/`poll`/`cancel`/`waitUntilComplete` stay; Core does not get the switch; a fake can yield `Failed`. Section **3. AI Actor does not erase data** item **2. Fail with `?ai`** requires “CloudAgents `setFake` yields Failed (not Finished).” `AgentResult` cannot express `Failed`. The same proof diff edits Locked item 5: unauthorized lock mutation. Leftover `AgentResult` remains on [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) item **4.2 `setFake` on the DLL** and Comments, and in [llm-connector project](plan/llm-connector/project.md) Notes: “Locked CloudAgents `setFake` on the DLL (`(StartArgs -> AgentResult) option -> bool`)”. `AgentStatus` also allows `Creating`, `Running`, and `Cancelled`; the rewritten lock names only `Finished` or `Failed`.

## (a) Missing or partial

None material. [AgentFailurePreserveTests](tests/Server.Tests/AgentFailurePreserveTests.fs) seeds two Focus Children, Runs `?ai` with `setFake` `Failed`, compares Child ids and text, keeps Change count at 1, asserts the provider string is not Node text, and drops via `liveFocusIds`. [RunAgentActor.fs](src/Server/RunAgentActor.fs) already maps `Failed` → `CompleteFailed` → `ActorFailed` and skips `postReplace`. The `setFake` type plus these tests is enough for this proof ticket.

## (b) Scope creep

1. **Unauthorized lock rewrite.** Ticket section **3. AI Actor does not erase data** item **2. Fail with `?ai`** asks for `setFake` to yield Failed. It does not ask to edit Locked **CloudAgents DLL interface**. See setFake lock.

## (c) Implemented but wrong

None. `pollFake` returns stored `AgentStatus` instead of wrapping `Finished`, so a handler can yield `Failed`.

One finding. Worst: unauthorized rewrite of Locked **CloudAgents DLL interface** item 5 (`AgentResult` → `AgentStatus`).
