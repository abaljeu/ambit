# 08 — Dispatch the AI Actor
**Status**: defined
One job: The dispatcher maps the command name `ai` to the AI Actor function, and that function's closure holds the session id.

[ ] The session id is not a field of the function input and not an argument of the start.

Type:

[ ] coding

Blockers:

[ ] [07 — Dispatch the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `RunAgentActor.actorFn` is an `ActorFn`. It reads `input.sessionId`, `input.graph`, `input.focusId`, `input.zoomId`, and `input.commandId`.
[ ] `deliver` and `takeInbox` on `CoreActorPool` key the live row by that session id.
[ ] `putActorStart` and `putFunctionStart` mint `sessionId` with `Guid.NewGuid` inside the pool.

### Reference

[ ] [Actor as client](../../project.md). Alan, 2026-11-03: the dispatcher captures the session id in the closure.

## Do not load

[ ] The git Load and Save names.
[ ] `CoreMsg.StartPeerActor`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Stop before editing, and ask, when the dispatcher must mint the session id. The pool mints it today, and the new start accepts only the function and the FocusId, so the live table cannot learn that id from a start argument.
3. [ ] After that answer: map `ai` in `ActorDispatch.fs`. The closure holds the session id. `startChosen` uses that function and the FocusId.
4. [ ] Remove the `ai` pair from `pool.register`.

## Outputs

[ ] `src/Server/ActorDispatch.fs`
[ ] `src/Server/RunAgentActor.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `tests/Server.Tests/AgentAskTests.fs` — the existing ask tests, on the dispatcher path

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/08-dispatch-the-ai-actor-stop.md`, then ask.
[ ] Stop on process step 2 until Alan says where the session id is minted.
[ ] Stop when the edit adds `sessionId` to the function input or to the start arguments.
[ ] Stop when a test file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `AgentAskTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
