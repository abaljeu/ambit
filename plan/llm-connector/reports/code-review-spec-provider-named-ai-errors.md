# Spec axis: 14 — Provider-named AI errors

Range: `git diff origin/staging...HEAD` (tip `4125f7ff`; merge-base `9a678913`). Spec: [14 — Provider-named AI errors](plan/llm-connector/issues/14-provider-named-ai-errors.md). Conveyance: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) (Poll / lastCmdResult). Glossary: [CONTEXT.md](CONTEXT.md) **AI**. 09 lock: [09 — Agent failure preserves children](plan/llm-connector/issues/09-agent-failure-preserves-children.md). Arch fake: [llm-connector architecture](plan/llm-connector/arch.md) Locked **CloudAgents setFake**.

## (a) Missing or partial

None. CloudAgents maps empty key and HTTP 401 to `AuthenticationFailed` that names Cursor (`Could not send message to Cursor: missing key` / `unauthorized`). `setFake` yields `Failed` with that unauthorized string, so proof needs no live key. `ActorFailed of string` encodes and decodes on Event JSON (`message` omitted when empty); Core copies the string. [ActorLive.fs](src/Shared/ActorLive.fs) `lastCmdResult` shows it as Error with command label **AI**; ActorSucceeded / ActorCancelled Ask labels are AI. No Graph Error outline, no AiKeys, no raw provider dumps into outline Nodes.

## (b) Not asked for

1. **Every poll `Failed` string, not only start/auth.** Spec: “Run Agent Actor — start/auth failure becomes that named string on Actor stop, not a dropped unit Failed.” [RunAgentActor.fs](src/Server/RunAgentActor.fs) maps every `Ok(Failed msg)` to `ActorFailed msg`. [CursorAdapter.fs](src/CloudAgents/Internal/CursorAdapter.fs) `mapStatus` also turns ERROR / EXPIRED / other into Failed `"error"` / `"expired"` / `"unknown status"`. Ticket 14 What to build is missing-key and unauthorized start/auth, not agent-body status text.

## (c) Implemented but wrong

1. **Non-auth Failed is not generic empty.** Spec: “`ActorFailed of string`; empty means generic.” Non-goals: “Raw provider dumps in Graph (09 lock stays).” 09: “ActorFinished records a safe domain error; no raw provider payload in Graph Events.” This copy stores a body `Failed` string (for example 09 `provider-body-secret`) on ActorStop JSON and shows it as lastCmdResult Error. Live ERROR shows `"error"`, not empty generic `"Actor failed."`, and not the locked auth wording.

Two findings. Worst: `Ok(Failed msg)` copies body Failed strings onto ActorFailed (against empty-means-generic and the 09 dump lock this ticket said stays).
