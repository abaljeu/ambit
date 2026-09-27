# Slice one: function-passing pool start

## 1. What landed

Expand-alongside on [05 — Expand-contract first aspect](../issues/05-expand-contract-first-aspect.md). CoreActorPool accepts a function-shaped start beside today’s ID-bag ActorStart. The pool does not invoke the Graph carrier and does not stuff Graph or ids into ActorInput.

## 2. Code

1. **[CoreActorPool](../../../src/Server/Core/CoreActorPool.fs)** — `FunctionStart` / `FunctionRun` / `GraphCarrier` / `FunctionActor`; `startFunction` stores `FunctionPending` and schedules `actor getGraph`. `startActor` still builds ActorInput from `graphIds`.
2. **[TestActor](../../../src/Server/TestActor.fs)** — `functionActor` calls `getGraph`, routes Focus text to `hello` or `ping` (`pong` child). `actorFn` ID-bag path unchanged.
3. **[CoreActorPoolFunctionStartTests](../../../tests/Server.Tests/CoreActorPoolFunctionStartTests.fs)** — carrier not called at start; hello and ping dispatch; ID-bag hello still starts.
4. Mock `CoreActorPool` records gained `startFunction` (unused). [Issue42 persist stubs](../../../tests/Server.Tests/Issue42PersistHandlersTests.fs) also gained missing `registerPeer` / `startPeerActor` so the record matches the type.

## 3. Not done

1. Hard cut of the ID-bag path.
2. HTTP / dispatch polymorphism (step-two hypothesis).
3. Rewriting other Actors.

## 4. Proof commands

```
dotnet build tests/Server.Tests -c Debug
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~CoreActorPoolFunctionStartTests"
```

Related ID-bag / mailbox suite:

```
dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~TestActorHelloTests|FullyQualifiedName~CoreActorPoolDeliverTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~CoreMailboxDoorTests|FullyQualifiedName~TestActorCommandErrorTests"
```

## 5. Results

1. **CoreActorPoolFunctionStartTests** — 4 passed.
2. **Related TestActor / pool / mailbox** — 44 passed.
3. **Server.Tests** — 577 passed, 0 skipped. Shared.Tests not rerun (no Shared edits).
