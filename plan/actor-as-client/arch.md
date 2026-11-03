# Actor as client architecture

Spec: [[spec.md]]

## 1. Story paths

1. **Run an actor function**

[x] Set up the actor function outside Core.
[x] The closure holds the values that run needs.
[x] The closure can hold a graph id.
[x] The closure can hold an LLM HTTP session.
[x] Give that function to the Core mailbox.
[x] `CoreActorPool.startFunction` stores it as `FunctionPending` on the live row.
[x] `putFunctionStart` mints the Actor session id.
[x] The actor function travels in memory through that pending queue.
[x] A logged Order never stores the actor function.
[x] A log entry cannot hold a function.
[x] The mailbox pulls the function from that queue with `runSchedule`.
[ ] 15 — Delete the old Actor starts renames `runSchedule` to `launchPending`.
[x] `runSchedule` runs the function with `Async.Start` and the row's cancel token.
[x] The function input is `getGraph`, the Actor credential, and `coreChanges`.

## 2. Module map

[x] SearchActor is the module in `src/Server/SearchActor.fs`.
[x] CoreMailboxActors is the module in `src/Server/Core/CoreMailboxActors.fs`.
[x] CoreActorPool is the module in `src/Server/Core/CoreActorPool.fs`.

## 3. Slicing strategy

[x] expand-contract
