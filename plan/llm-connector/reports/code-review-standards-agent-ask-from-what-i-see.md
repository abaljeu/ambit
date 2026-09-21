# Standards — Agent ask from what I see

Range: three-dot `origin/staging...HEAD` (`3c0e14cf`). Scan printed no LONG / TAB / MUTABLE-keyword / BARE_ID / FILE-growth lines. `waitLive` is 38 lines (not a size hit). [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) does not open CloudAgents. [RunAgentActor.fs](src/Server/RunAgentActor.fs) and [Gambol.Server.fsproj](src/Server/Gambol.Server.fsproj) may. Stage `build` matches First implement in [project-status](doc/agents/project-status.md). Draft Events use `EventId.zero` per [core-api.md](.agents/rules/core-api.md).

## Hard violations

### [AgentRunner.fs](src/CloudAgents/AgentRunner.fs) — mutable Fake

[fsharp-source.md](.agents/rules/fsharp-source.md): Do not use mutable. The scan matches the `mutable` keyword only. Private `Fake` stores process state in `ref` cells and writes with `:=` on the CloudAgents DLL (`handler`, `results`, `inFlight`).

### [AgentAskTests.fs](tests/Server.Tests/AgentAskTests.fs) — 40-line function

Same rule: 40 lines or less per function. The test file-size note does not drop that rule. Scan skips `tests/` for 40-line discovery. Member `Ask extract carries Graph.focus from launch` is lines 250–296 (47).

### [AgentRunner.fs](src/CloudAgents/AgentRunner.fs) — grouped parameters

Same rule: group related parameters into a named reused type. `StartArgs` exists. Private `startFake f config prompt repos options` still takes the four fields and calls `toStartArgs`.

### [spec.md](plan/llm-connector/spec.md) — refer by name

[refer-by-name.md](.agents/rules/refer-by-name.md): never refer by only the id; the name wraps the link. New Delivered clauses label `[[issues/08-agent-ask-from-what-i-see.md|08]]` and `[[issues/11-simple-extract-format.md|11]]`.

### [12 — Replace Focus Children from reply](plan/llm-connector/issues/12-replace-focus-children-from-reply.md)

Same rule. New comment: `do not implement 12 separately`.

## Judgement-call smells

**Duplicated Code.** `pollFake` and `waitFake` share the lookup:

```
match Fake.tryGet (agentId, runId) with
| Some result -> ...
| None -> Error(ApiError("not-found", "fake agent result missing"))
```

**Duplicated Code.** Three Ask success Facts repeat `withFake` / `withHost` / `waitForActorStop` / `ActorSucceeded`.

**Middle Man.** `waitUntilComplete` only dispatches Fake vs `waitLive`.

**Shotgun Surgery.** One Ask path edits CloudAgents, Server composition, Core exclusivity, Shared replace, and plan checkboxes. Expected for this ticket.

## No hit

No Core→CloudAgents reference. No [environment.md](.agents/rules/environment.md) edit (`CURSOR_API_KEY` is app config). No [no-retrofit.md](.agents/rules/no-retrofit.md) rewrite of old tickets beyond Status and checkboxes. New [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) project note uses `[label](path)`. No consecutive blank lines. `withFocus` plus exclusivity on [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) traces to Ask ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical).

Counts: 5 hard, 4 smells. Worst hard: mutable `ref` Fake on the CloudAgents DLL.
