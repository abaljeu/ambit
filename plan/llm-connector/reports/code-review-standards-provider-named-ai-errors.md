# Standards — provider-named AI errors

Range: `git diff origin/staging...HEAD`. Tip `4125f7ff`. Merge-base `9a678913`.

## Mechanical scan

```
tests/Server.Tests/AskCancelHarness.fs  .agents/rules/fsharp-source.md  FILE 473->476  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/CloudAgents/Internal/CursorAdapter.fs::providerName: lines 35-36 (2 lines)
src/CloudAgents/Internal/CursorAdapter.fs::authFailed: lines 37-40 (4 lines)
src/CloudAgents/Internal/CursorAdapter.fs::fromHttpError: lines 41-48 (8 lines)
src/CloudAgents/PublicTypes.fs::couldNotSend: lines 54-55 (2 lines)
src/Server/RunAgentActor.fs::failedFromError: lines 16-20 (5 lines)
src/Shared/ActorLive.fs::actorCmd: lines 63-64 (2 lines)
src/Shared/ActorLive.fs::failedText: lines 65-67 (3 lines)
src/Shared/EventJson.fs::decodeActorResultTag: lines 25-31 (7 lines)
src/Shared/EventJson.fs::actorResultFrom: lines 32-37 (6 lines)
```

Scan exit 1.

## Hard violations

The FILE hit on [AskCancelHarness](tests/Server.Tests/AskCancelHarness.fs) cites [fsharp-source](.agents/rules/fsharp-source.md) (800/400). That rule does not apply to tests. It is not a violation.

measure-fs-size bindings are 2–8 lines. All sit under the 40-line function rule in [fsharp-source](.agents/rules/fsharp-source.md). The scan did not report a long line, a tab, or `mutable`.

[14 — Provider-named AI errors](plan/llm-connector/issues/14-provider-named-ai-errors.md) Context and What to build item **2.2 Run Agent Actor** name the work **Run Agent Actor**. [CONTEXT.md](CONTEXT.md) **AI** / **Run AI**: do not say Agent for this Actor; avoid Run Agent. Item **3.2 Ask scrub** uses **Ask** as the item name. [markdown-writing](.agents/rules/markdown-writing.md) requires glossary words.

No other documented-standard hits in the F# hunks. [CoreEventDispatch](src/Server/Core/CoreEventDispatch.fs) relays the `ActorFailed` string. [CursorAdapter](src/CloudAgents/Internal/CursorAdapter.fs) names the provider. [core-api](.agents/rules/core-api.md) EventId serial is unchanged. Project Stage stays `done`. New plan lines do not use git place names as delivery status ([planning-docs](.agents/rules/planning-docs.md), [project-stage](.agents/rules/project-stage.md)). [core-agent-behavior](.agents/rules/core-agent-behavior.md): `fromHttpError` is used; no unused leftover from this range.

## Smells (judgement)

**Duplicated Code** — empty-key guard in `startAgent`, `pollStatus`, and `fromHttpError`:

```
        if String.IsNullOrWhiteSpace config.ApiKey then
            Error (authFailed "missing key")
```

Unauthorized HTTP in [CursorHttp](src/CloudAgents/Internal/CursorHttp.fs) `createAgent` and `getRunStatus`:

```
            if response.StatusCode = HttpStatusCode.Unauthorized then
                Error "unauthorized"
```

`cancelRun` does not use that shape.

**Shotgun Surgery** — `ActorFailed of string` in [History](src/Shared/History.fs) forces Core, Server, CloudAgents, Event JSON, ActorLive, and tests. That spread is normal for a wire DU.

**Mysterious Name** — `actorCmd` in [ActorLive](src/Shared/ActorLive.fs) holds the AI command label:

```
    let private actorCmd = Some "AI"
```

**Primitive Obsession** — empty string as generic `ActorFailed`. The ticket asks for that; suppress.
