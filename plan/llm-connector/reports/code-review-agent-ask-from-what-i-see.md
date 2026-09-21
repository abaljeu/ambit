# Code review: Agent ask from what I see

Independent two-axis review of [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md). Range: three-dot `origin/staging...HEAD` (merge-base `08831db1`; tip `3c0e14cf`). Spec: that ticket plus [llm-connector architecture](plan/llm-connector/arch.md) modules **Run Agent Actor**, **Document (Amb pack + Reference Paste)**, **CloudAgents**, **CoreMailbox / CoreMsg / CoreActorPool**, Story path **Agent ask from what I see**, Locked **CloudAgents DLL interface** and **CloudAgents setFake**. Subject: CloudAgents `setFake`; [RunAgentActor.fs](src/Server/RunAgentActor.fs) pack → complete → Focus replace; [FocusChildrenReplace.fs](src/Shared/documents/FocusChildrenReplace.fs); Core without CloudAgents; fake Ask tests. Axis drafts: [Standards axis](code-review-standards-agent-ask-from-what-i-see.md), [Spec axis](code-review-spec-agent-ask-from-what-i-see.md). Mechanical scan: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` printed only measure-fs-size (all bindings ≤38 lines; no LONG / TAB / MUTABLE-keyword / BARE_ID / FILE-growth). **Status:** stays `coded`. A report is not approval.

## Verdict

**Approve with nits.** Ticket success path is present: `?ai` launches the Run Agent Actor; Amb `SuppliedExtract` pack; `setFake` on the CloudAgents DLL; Focus-child replace through ordinary Core Change; fake Ask proof; `src/Server/Core` does not reference CloudAgents. Worst Standards hit is process-local `ref` Fake state on the DLL (documented no-mutable rule; the scan matches the `mutable` keyword only). Worst Spec hit is Complete/cancel mapping marked delivered while `RunAgentActor.complete` never fits a cancel token (cancel story stays on [10 — Cancel by Focus](plan/llm-connector/issues/10-cancel-by-focus.md)). Concurrent merge uses ordinary Change post and has no Agent-specific stale rule; the new test does not read Focus Children after both Changes.

## Standards

Range: three-dot `origin/staging...HEAD` (`3c0e14cf`). Scan printed no LONG / TAB / MUTABLE-keyword / BARE_ID / FILE-growth lines. `waitLive` is 38 lines (not a size hit). [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) does not open CloudAgents. [RunAgentActor.fs](src/Server/RunAgentActor.fs) and [Gambol.Server.fsproj](src/Server/Gambol.Server.fsproj) may. Stage `build` matches First implement in [project-status](doc/agents/project-status.md). Draft Events use `EventId.zero` per [core-api.md](.agents/rules/core-api.md).

### Hard violations

#### [AgentRunner.fs](src/CloudAgents/AgentRunner.fs) — mutable Fake

[fsharp-source.md](.agents/rules/fsharp-source.md): Do not use mutable. The scan matches the `mutable` keyword only. Private `Fake` stores process state in `ref` cells and writes with `:=` on the CloudAgents DLL (`handler`, `results`, `inFlight`).

#### [AgentAskTests.fs](tests/Server.Tests/AgentAskTests.fs) — 40-line function

Same rule: 40 lines or less per function. The test file-size note does not drop that rule. Scan skips `tests/` for 40-line discovery. Member `Ask extract carries Graph.focus from launch` is lines 250–296 (47).

#### [AgentRunner.fs](src/CloudAgents/AgentRunner.fs) — grouped parameters

Same rule: group related parameters into a named reused type. `StartArgs` exists. Private `startFake f config prompt repos options` still takes the four fields and calls `toStartArgs`.

#### [spec.md](plan/llm-connector/spec.md) — refer by name

[refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id; the name wraps the link. New Delivered clauses label `[[issues/08-agent-ask-from-what-i-see.md|08]]` and `[[issues/11-simple-extract-format.md|11]]`.

#### [12 — Replace Focus Children from reply](plan/llm-connector/issues/12-replace-focus-children-from-reply.md)

Same rule. New comment: `do not implement 12 separately`.

### Judgement-call smells

**Duplicated Code.** `pollFake` and `waitFake` share the lookup:

```
match Fake.tryGet (agentId, runId) with
| Some result -> ...
| None -> Error(ApiError("not-found", "fake agent result missing"))
```

**Duplicated Code.** Three Ask success Facts repeat `withFake` / `withHost` / `waitForActorStop` / `ActorSucceeded`.

**Middle Man.** `waitUntilComplete` only dispatches Fake vs `waitLive`.

**Shotgun Surgery.** One Ask path edits CloudAgents, Server composition, Core exclusivity, Shared replace, and plan checkboxes. Expected for this ticket.

### No hit

No Core→CloudAgents reference. No [environment.md](.agents/rules/environment.md) edit (`CURSOR_API_KEY` is app config). No [no-retrofit.md](.agents/rules/no-retrofit.md) rewrite of old tickets beyond Status and checkboxes. New [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) project note uses `[label](path)`. No consecutive blank lines. `withFocus` plus exclusivity on [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) traces to Ask ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical).

Counts: 5 hard, 4 smells. Worst hard: mutable `ref` Fake on the CloudAgents DLL.

## Spec

Spec: [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md); architecture modules **Run Agent Actor**, **Document**, **CloudAgents**, **CoreMailbox / CoreMsg / CoreActorPool** and Locked **CloudAgents DLL interface** / **CloudAgents setFake** in [llm-connector architecture](plan/llm-connector/arch.md). Range: `origin/staging...HEAD`. Consume only: [11 — Pack extract with Amb (supplied-fragment walk)](plan/llm-connector/issues/11-simple-extract-format.md).

### (a) Missing or partial

1. **Concurrent merge proof is partial.** [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) §5.2: "a credentialed Change under Focus while the Actor runs reconciles with ordinary merge only (Story path **Credentialed Change while Agent runs** open hop)." The diff adds no Agent-specific stale rule (`getState` then `ChildListWire.replace`; harness `postEvents`). The new test only asserts that post is `Ok` and the Actor reaches `ActorSucceeded`. It does not read Focus Children after both Changes.

### (b) Scope creep

None material. `setFake`, `?ai` → `RunAgentActor`, `AmbWriteWalk.SuppliedExtract` pack, `FocusChildrenReplace`, ordinary Core Change, and fake Ask tests match the ticket. Server composition references CloudAgents; `src/Server/Core` does not. [12 — Replace Focus Children from reply](plan/llm-connector/issues/12-replace-focus-children-from-reply.md) stays open; that is not extra code.

### (c) Implemented but looks wrong

1. **Cancel-token complete is marked delivered.** [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) §2.2: "Complete — fit system prompt + document + cancel into existing CloudAgents `start` / `poll` / `cancel`; map outcomes to Completed | Failed | Cancelled." [llm-connector architecture](plan/llm-connector/arch.md) step 6: "Run Agent Actor → CloudAgents complete (system prompt + document + cancel token)." Module **CloudAgents** interface 3 and Locked **CloudAgents DLL interface** mark the same fit `[x]`. `RunAgentActor.complete` concatenates prompt and pack, calls `start` and `waitUntilComplete`, and maps every error to `ActorFailed`. It does not call `cancel` or emit `Cancelled`. [10 — Cancel by Focus](plan/llm-connector/issues/10-cancel-by-focus.md) still owns the Cancel story; this is 08's Complete line claimed done.

Success path matches: DLL `setFake`, Amb extract-walk pack, Focus-child replace, fake Ask through ordinary Core Change, Core without CloudAgents. Two findings. Worst: Complete/cancel mapping marked `[x]` while `RunAgentActor.complete` never fits a cancel token.

## Summary

Standards: 5 hard + 4 smells; worst within axis: mutable `ref` Fake on the CloudAgents DLL. Spec: 2 findings; worst within axis: Complete/cancel mapping marked `[x]` while `RunAgentActor.complete` never fits a cancel token. Verdict: Approve with nits. Status stays `coded`.
