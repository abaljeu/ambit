# 08 — Dispatch the AI Actor
**Status**: defined
One job: The dispatcher maps the command name `ai` to the AI Actor function. The closure holds the LLM HTTP session. The Actor session id stays minted by the pool.

[ ] The function input stays `FunctionRun` and `CoreChangesDoor`. The call is `pool.startFunction`.

Type:

[ ] coding

Blockers:

[ ] [07 — Dispatch the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `RunAgentActor.actorFn` is an `ActorFn`. It reads `input.sessionId` and passes that string to the Grok wake and to `requestGrokCancel`.
[ ] `putFunctionStart` mints the Actor `sessionId` with `Guid.NewGuid` and stores it on the live row.
[ ] `deliver` and `takeInbox` key the live row by that Actor session id.

### Reference

[ ] [Actor as client](../../project.md). Alan, 2026-11-03: the pool mints the Actor session id. The closure holds the LLM HTTP session. That earlier closure note was not the Actor session id.

## Do not load

[ ] The git Load and Save names.
[ ] Do not add a `CoreMsg` case.
[ ] Do not add the Actor session id to `FunctionRun` or to the closure.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Map `ai` in `ActorDispatch.fs`. The closure holds the LLM HTTP session. The wake and cancel use that session.
3. [ ] `startChosen` calls `pool.startFunction` for `ai`. The function input is `FunctionRun` and `CoreChangesDoor`.
4. [ ] Leave the Actor session id minted in `putFunctionStart`. `deliver` and `takeInbox` keep that id.
5. [ ] Remove the `ai` pair from `pool.register`.

## Outputs

[ ] `src/Server/ActorDispatch.fs`
[ ] `src/Server/RunAgentActor.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `tests/Server.Tests/AgentAskTests.fs` — the existing ask tests, on the dispatcher path

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/08-dispatch-the-ai-actor-stop.md`, then ask.
[ ] Stop when the edit mints the Actor session id in the dispatcher, or stores it in the closure.
[ ] Stop when the edit adds the Actor session id to the function input.
[ ] Stop when a test file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `AgentAskTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
