# Standards — [16 — AiRepos from appsettings](plan/llm-connector/issues/16-airepos-from-appsettings.md)

Range: three-dot `origin/staging...HEAD` (`97e7f180`). Scan exit 1. Printed FILE 478→480 on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) with rule [fsharp-source.md](.agents/rules/fsharp-source.md). Measure-fs-size bindings stay under 40 lines. [RouteRegistration.fs](src/Server/RouteRegistration.fs) line count does not grow.

## Hard violations

### [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) — FILE 478→480

[fsharp-source.md](.agents/rules/fsharp-source.md): already over 400 or new file over 400; change increased it. The same rule: the file-size split does not apply to tests. No production split duty.

### [RunAgentActor.fs](src/Server/RunAgentActor.fs) — grouped parameters

Same rule: group related parameters into a named reused type. When a parameter belongs with existing ones, extend that type. Reuse a type that already exists. `complete` now takes `apiKey`, `repos`, and `document`, then calls `AgentRunner.start config prompt repos askOptions`. `StartArgs` already holds those four fields. `actorFn` adds `repos` beside `keys` with no named type. The pair also travels through `runBody`, `commandArgs`, [AiRepos.fs](src/Server/AiRepos.fs) `AiAskArgs.fromText`, and `withHostKeysRepos`.

## Judgement-call smells

**Mysterious Name.** New Facts on `AiReposActorTests` use Ask. [CONTEXT.md](CONTEXT.md) **AI**: it is not named Ask.

```
member _.``Ask without reponame sends no repos``() =
```

**Divergent Change.** [AiRepos.fs](src/Server/AiRepos.fs) binds `AiRepos` and also owns `AiAskArgs.fromText` (keyname and reponame).

**Shotgun Surgery.** One bind/inject change edits Server, appsettings, harness, plan map/spec/arch, and [16 — AiRepos from appsettings](plan/llm-connector/issues/16-airepos-from-appsettings.md). Expected for this ticket.

**Duplicated Code** (suppressed). `AiRepos.fromConfig` repeats the `AiKeys.fromConfig` indexer shape. The ticket asked for that same pattern.

## No hit

No mutable. No Exceptions. No line over 100 characters in the new F#. Public `fromConfig` / `resolve` / `fromText` require the module name (`RequireQualifiedAccess`). Core does not see repos ([core-api.md](.agents/rules/core-api.md)). CloudAgents stays settings-blind. New plan links wrap number and name ([refer-by-name.md](.agents/rules/refer-by-name.md)). Stage stays `done` on the follow-up. Surgical under-100-line preference is not a script fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

Counts: 1 hard (grouped `keys` and `repos`), 1 scan FILE exempt, 3 smells (1 suppressed). Worst hard: `keys` and `repos` ungrouped on `actorFn`.
