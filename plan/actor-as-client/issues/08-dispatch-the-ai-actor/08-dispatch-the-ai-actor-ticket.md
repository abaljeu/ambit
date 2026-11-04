# 08 — Start the AI Actor
**Status**: defined
One job: Run on a node that contains `?ai` puts `Order.ActorOrder` on the mailbox queue. The actor function is `RunAgentActor.actorFn`. The Actor session id stays minted by the pool.

[ ] That Run is the same `execRunOp` and `postActorStart` path as `?test`.
[ ] `actorFn` returns `FunctionActor`. The closure holds the LLM HTTP session. Its input is `FunctionRun` and `CoreChangesDoor`.

Type:

[ ] coding

Blockers:

[ ] [07 — Start the test command](../07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

## Inputs

### Working

[ ] `RouteRegistration.fs` registers `ActorName "ai"` as `RunAgentActor.actorFn`.
[ ] `RunAgentActor.actorFn` is an `ActorFn`. It reads `input.sessionId` and passes that string to the Grok wake and to `requestGrokCancel`.
[ ] `putFunctionStart` mints the Actor `sessionId` with `Guid.NewGuid` and stores it on the live row.
[ ] `deliver` and `takeInbox` key the live row by that Actor session id.
[ ] `postActorOrder` is on `MailboxHost`.

### Reference

[ ] [Arch](../../arch.md). The pool mints the Actor session id. The closure holds `GrokBotBinding`.

## Do not load

[ ] The git Load and Save names.
[ ] Do not add a `CoreMsg` case.
[ ] Do not add the Actor session id to `FunctionRun` or to the closure.
[ ] `src/Server/ActorDispatch.fs`. Do not add it.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change: `actorFn` returns `FunctionActor`. The closure holds the LLM HTTP session. The wake and cancel use that session.
3. [ ] Change: `postActorStart` for a `?ai` Run builds `ActorOrder` with that function, `getGraph`, the FocusId, and the command name, and calls `postActorOrder`.
4. [ ] Leave the Actor session id minted in `putFunctionStart`. `deliver` and `takeInbox` keep that id.
5. [ ] Contract: Remove the `ai` pair from `pool.register`.
6. [ ] Do not write `Order.StartActor` or `Order.RunActor`. Do not look up the function from `commandName`.

## Outputs

[ ] `src/Server/Core/CoreMailboxActors.fs`
[ ] `src/Server/RunAgentActor.fs`
[ ] `src/Server/RouteRegistration.fs`
[ ] `tests/Server.Tests/AgentAskTests.fs` — the existing ask tests, through `postActorOrder`

## Stop when

[ ] Record the stop at `plan/actor-as-client/reports/08-dispatch-the-ai-actor-stop.md`, then ask.
[ ] Stop when the edit mints the Actor session id outside the pool, or stores it in the closure.
[ ] Stop when the edit adds a route, a handler, `ActorDispatch.fs`, or a command-name map.
[ ] Stop when a test file outside Outputs must change.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: `AgentAskTests`
[ ] All executed tests pass.
[ ] Write the verdict to `plan/actor-as-client/reports/08-dispatch-the-ai-actor-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
