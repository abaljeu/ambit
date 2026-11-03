# Actor as client architecture

Marks: [ ] required, [/] in progress, [x] done, [o] obsolescent.

## 1. Set up the actor function

[x] The actor function is a closure defined outside Core.
[x] The closure holds the values that run needs.
[x] `SearchActor.functionStart` holds the command node id in that closure.
[x] An LLM HTTP session is a value the same closure can hold.

## 2. Give the function to the Core mailbox

[x] The Core mailbox passes the actor function to `CoreActorPool.startFunction`.
[x] `putFunctionStart` stores `FunctionPending` on the live row.
[x] That pending body is the mailbox queue for the actor function.
[x] The actor function travels in memory through that queue.
[x] `putFunctionStart` mints the Actor session id on the live row.
[x] `putFunctionStart` mints the Actor credential on the live row.

## 3. Run the function from the queue

[x] The mailbox calls `schedule`.
[x] `schedule` is `runSchedule`.
[ ] [15 — Delete the old Actor starts](plan/actor-as-client/issues/15-delete-the-old-actor-starts/15-delete-the-old-actor-starts-ticket.md) renames `runSchedule` to `launchPending`.
[x] `runSchedule` takes the pending body off the live row.
[x] `runSchedule` runs the actor function with `Async.Start` and the row's cancel token.
[x] The function input is `getGraph`, the Actor credential, and `coreChanges`.
[x] `coreChanges` has type `CoreChangesDoor`.

## Logged Order

[x] The actor function is never stored in a logged Order.
[x] A log entry cannot hold a function.

## Open questions

No status mark. This note does not decide these items.

- Landed tickets add `Order.RunActor` (`commandName`, `focusId`) as a log record. 06 — Log Order.RunActor adds that record. Alan has questioned it.
- Landed tickets map command names to functions in `src/Server/ActorDispatch.fs`. 07 — Dispatch the test command adds that module. Alan has questioned it.
