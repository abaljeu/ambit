# Standards review: AiKeys from appsettings

Range: `git diff origin/staging...HEAD` at `103511960ac4a62c73b71f0e43eb626f4cac4c08`. Mechanical scan hits are documented-standard hits.

## Documented-standard hits

1. **Bare id on [15 — AiKeys from appsettings](../issues/15-aikeys-from-appsettings.md)** — [refer-by-name.md](../../../.agents/rules/refer-by-name.md): include the name with every id. Line 11 says "stays the ticket 14 path". Use [14 — Provider-named AI errors](../issues/14-provider-named-ai-errors.md).
2. **[RouteRegistration.fs](../../../src/Server/RouteRegistration.fs) 415 to 417 lines** — [fsharp-source.md](../../../.agents/rules/fsharp-source.md): a file already over 400 lines must split when a change increases it. Hunk in `CreateBoot`: `RunAgentActor.actorFn (AiKeys.fromConfig this.Config)`.
3. **[AskCancelHarness.fs](../../../tests/Server.Tests/AskCancelHarness.fs) 476 to 478 lines** — scan cites [fsharp-source.md](../../../.agents/rules/fsharp-source.md) FILE. The same rule says the file-size split does not apply to tests. No production split duty.

Measured bindings in [AiKeys.fs](../../../src/Server/AiKeys.fs) and [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) are under 40 lines. No 40-line hit. New F# has no line over 100 characters and no mutable or exceptions.

## Smells (judgement, not violations)

1. **Middle Man** — [RunAgentActor.fs](../../../src/Server/RunAgentActor.fs) `apiKeyFor` only forwards:

```
    let private apiKeyFor keys (input: ActorInput) =
        AiKeys.resolve keys (commandKeyname input)
```

2. **Duplicated Code** — [AgentAuthErrorTests.fs](../../../tests/Server.Tests/AgentAuthErrorTests.fs) `empty AiKeys names Cursor missing key on ActorStop` repeats the `EventBody.ActorStop` / `CmdLastResult.Error (Some "AI", named)` block from `setFake unauthorized names Cursor on ActorStop`.
3. **Mysterious Name** — [AiKeysTests.fs](../../../tests/Server.Tests/AiKeysTests.fs) facts `Ask without keyname sends the first AiKeys ApiKey`, `Ask keyname sends that entry ApiKey`, `Ask unknown keyname sends empty ApiKey`. [CONTEXT.md](../../../CONTEXT.md): the Actor is AI, not Ask.

No [core-api.md](../../../.agents/rules/core-api.md) miss: composition injects keys; Core takes `ActorFn` only.
