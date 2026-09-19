# Independent review: 14 — Provider-named AI errors

Independent two-axis review. The reviewer did not write the implementation. Not approval. Ticket [14 — Provider-named AI errors](plan/llm-connector/issues/14-provider-named-ai-errors.md) **Status:** stays `coded`.

**Verdict: Good**

**Pin:** tip `4125f7ff` (named `cursor/provider-named-ai-errors-75cd`). User named `origin/staging`; three-dot `origin/staging...HEAD` is non-empty (merge-base `9a678913`, equals current `origin/staging`).

**Commits:** `d2cf010b` File llm-connector 14 provider-named AI errors; `dcf43471` Carry provider-named AI auth errors to Client; `4125f7ff` Keep AskCancelHarness helpers local to auth tests.

**Spec:** [14 — Provider-named AI errors](plan/llm-connector/issues/14-provider-named-ai-errors.md). Glossary [CONTEXT.md](CONTEXT.md) **AI** (not Ask). Chrome conveyance owned by [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md); this ticket supplies the message.

**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (script invoked as `python3`; `python` is absent). Exit 1. Printed FILE-growth on [AskCancelHarness.fs](tests/Server.Tests/AskCancelHarness.fs) 473→476. [fsharp-source](.agents/rules/fsharp-source.md) file-size cap does not apply to tests. measure-fs-size bindings are 2–8 lines.

Axis drafts: [Standards](code-review-standards-provider-named-ai-errors.md), [Spec](code-review-spec-provider-named-ai-errors.md). Axes stay separate below.

## Standards

Range: `git diff origin/staging...HEAD`. Tip `4125f7ff`. Merge-base `9a678913`.

### Mechanical scan

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

### Hard violations

The FILE hit on [AskCancelHarness](tests/Server.Tests/AskCancelHarness.fs) cites [fsharp-source](.agents/rules/fsharp-source.md) (800/400). That rule does not apply to tests. It is not a violation.

measure-fs-size bindings are 2–8 lines. All sit under the 40-line function rule in [fsharp-source](.agents/rules/fsharp-source.md). The scan did not report a long line, a tab, or `mutable`.

[14 — Provider-named AI errors](plan/llm-connector/issues/14-provider-named-ai-errors.md) Context and What to build item **2.2 Run Agent Actor** name the work **Run Agent Actor**. [CONTEXT.md](CONTEXT.md) **AI** / **Run AI**: do not say Agent for this Actor; avoid Run Agent. Item **3.2 Ask scrub** uses **Ask** as the item name. [markdown-writing](.agents/rules/markdown-writing.md) requires glossary words.

No other documented-standard hits in the F# hunks. [CoreEventDispatch](src/Server/Core/CoreEventDispatch.fs) relays the `ActorFailed` string. [CursorAdapter](src/CloudAgents/Internal/CursorAdapter.fs) names the provider. [core-api](.agents/rules/core-api.md) EventId serial is unchanged. Project Stage stays `done`. New plan lines do not use git place names as delivery status ([planning-docs](.agents/rules/planning-docs.md), [project-stage](.agents/rules/project-stage.md)). [core-agent-behavior](.agents/rules/core-agent-behavior.md): `fromHttpError` is used; no unused leftover from this range.

### Smells (judgement)

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

## Spec

Range: `git diff origin/staging...HEAD` (tip `4125f7ff`; merge-base `9a678913`). Spec: [14 — Provider-named AI errors](plan/llm-connector/issues/14-provider-named-ai-errors.md). Conveyance: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) (Poll / lastCmdResult). Glossary: [CONTEXT.md](CONTEXT.md) **AI**. 09 lock: [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md). Arch fake: [llm-connector architecture](plan/llm-connector/arch.md) Locked **CloudAgents setFake**.

### (a) Missing or partial

None. CloudAgents maps empty key and HTTP 401 to `AuthenticationFailed` that names Cursor (`Could not send message to Cursor: missing key` / `unauthorized`). `setFake` yields `Failed` with that unauthorized string, so proof needs no live key. `ActorFailed of string` encodes and decodes on Event JSON (`message` omitted when empty); Core copies the string. [ActorLive.fs](src/Shared/ActorLive.fs) `lastCmdResult` shows it as Error with command label **AI**; ActorSucceeded / ActorCancelled Ask labels are AI. No Graph Error outline, no AiKeys, no raw provider dumps into outline Nodes.

### (b) Not asked for

1. **Every poll `Failed` string, not only start/auth.** Spec: “Run Agent Actor — start/auth failure becomes that named string on Actor stop, not a dropped unit Failed.” [RunAgentActor.fs](src/Server/RunAgentActor.fs) maps every `Ok(Failed msg)` to `ActorFailed msg`. [CursorAdapter.fs](src/CloudAgents/Internal/CursorAdapter.fs) `mapStatus` also turns ERROR / EXPIRED / other into Failed `"error"` / `"expired"` / `"unknown status"`. Ticket 14 What to build is missing-key and unauthorized start/auth, not agent-body status text.

### (c) Implemented but wrong

1. **Non-auth Failed is not generic empty.** Spec: “`ActorFailed of string`; empty means generic.” Non-goals: “Raw provider dumps in Graph (09 lock stays).” 09: “ActorFinished records a safe domain error; no raw provider payload in Graph Events.” This copy stores a body `Failed` string (for example 09 `provider-body-secret`) on ActorStop JSON and shows it as lastCmdResult Error. Live ERROR shows `"error"`, not empty generic `"Actor failed."`, and not the locked auth wording.

Two findings. Worst: `Ok(Failed msg)` copies body Failed strings onto ActorFailed (against empty-means-generic and the 09 dump lock this ticket said stays).

## Summary

Standards: 0 hard, 1 glossary wording note, 3 judgement smells (1 suppressed); worst in-axis: ticket item names **Run Agent Actor** / **Ask scrub** vs [CONTEXT.md](CONTEXT.md) **AI**. Spec: 2 findings; worst in-axis: `Ok(Failed msg)` copies body Failed strings onto ActorFailed. No must-fix on the ticket checklist (CloudAgents names Cursor; `setFake` Unauthorized; ActorFailed string through Event JSON; lastCmdResult Error labeled AI; no Graph Error / AiKeys).
