# Actor as client architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: `startFunction` is the one pool start, `Order.ActorOrder` carries an `ActorFunction`, and the mailbox pull runs that function on the .NET thread pool.

Out of scope: remote starts, per-run endpoints, the `/state` route, and a PersistHandlers rename.

## 2. Types

[x] `GraphCarrier`, `FunctionRun`, `Credential`, `ActorChanges`, and `ActorOrder` are in `src/Shared/History.fs`.
[x] The actor field's type name is `FunctionActor`.
[o] `FunctionActor`
[ ] The type name is `ActorFunction`.
[x] `ActorChanges.graphAtEvent` returns `GraphAtEvent`.

```fsharp
type GraphCarrier = unit -> Graph

type FunctionRun =
    { getGraph: GraphCarrier
      secret: Credential }

type ActorFunction = FunctionRun -> ActorChanges -> Async<unit>

type ActorChanges =
    { postChanges: Ev list -> Async<Result<unit, string>>
      actorStop: ActorResult -> Async<Result<unit, string>>
      graphAtEvent: unit -> Async<Result<GraphAtEvent, string>>
      loadNode: NodeId -> Async<Result<EventId, string>> }

type ActorOrder =
    { actor: ActorFunction
      focusId: NodeId
      actorName: string }
```

[x] `Order.ActorOrder` carries `ActorOrder`. The log stores that member as-is.

```fsharp
| ActorOrder of ActorOrder
```

[o] `Order.RunActor`

[x] The pool actor type is in `src/Server/Core/CoreActorPool.fs`. Its name is `FunctionActor`. Its second argument is `CoreChangesDoor`.
[o] The pool name `FunctionActor`.
[ ] The pool type name is `ActorFunction`. The second argument stays `CoreChangesDoor`.

```fsharp
type ActorFunction = FunctionRun -> CoreChangesDoor -> Async<unit>
```

[x] `FunctionStart` is in `src/Server/Core/CoreActorPool.fs`.
[ ] `FunctionStart.actor` is the pool `ActorFunction`.

```fsharp
type FunctionStart =
    { actor: ActorFunction
      getGraph: GraphCarrier
      focusId: NodeId
      actorNodeId: NodeId }
```

[x] `ActorFn` is in `src/Server/Core/CoreActorPool.fs`.
[o] `ActorFn` is the in-process start.

```fsharp
type ActorFn = ActorInput -> CoreChangesDoor -> Async<unit>
```

[x] `FunctionPending` [o] `BagPending`

```fsharp
type PendingBody =
    | BagPending of actorFn: ActorFn * input: ActorInput
    | FunctionPending of actor: ActorFunction * getGraph: GraphCarrier * secret: Credential
```

## 3. Interfaces

### Actor function

[ ] The Shared actor is `ActorFunction`.

```fsharp
ActorFunction : FunctionRun -> ActorChanges -> Async<unit>
```

An Actor is a thread from the .NET thread pool. It runs an `ActorFunction`.

### Curried actor

[ ] An old-style actor takes `actorNodeId` and yields `ActorFunction`.

```fsharp
curriedActor : NodeId -> ActorFunction
```

The endpoint applies `actorNodeId` before Post actor order. `ActorOrder` stores that `ActorFunction`. `ActorOrder` has no `actorNodeId` field.

### Actor order

[x] `Order.ActorOrder` is the logged order.

```fsharp
ActorOrder : ActorOrder -> Order
```

`focusId` is the node that has focus when the command runs. `actorName` is the word after `?`. `actorName` stands in for `actor` on the wire.

### Wire actor order

[x] `stubActor` is the actor on a wire read.

```fsharp
stubActor : ActorFunction
encodeActorOrder : ActorOrder -> IEncodable
decodeActorOrder : string -> Result<ActorOrder, string>
```

`encodeActorOrder` writes `focusId` and `actorName`. `decodeActorOrder` sets `actor` to `stubActor`. A wire-read `ActorOrder` is a log record. `stubActor` stays unrun.

### Log actor order

[x] `MailboxLog` accepts `Order.ActorOrder`.

```fsharp
historyBody : Order -> Result<EventBody, string>
```

### Post actor order

[x] `postActorOrder` queues one `ActorOrder`.

```fsharp
postActorOrder : MailboxHost -> ActorOrder -> unit
```

[x] That post uses `Authority "Actor"` and `Credential ""`.
[o] `Authority "Actor"` and `Credential ""` on this path.
[ ] The post takes the client's `Caller`.

```fsharp
postActorOrder : MailboxHost -> Caller -> ActorOrder -> unit
```

`Mailbox.placeForCaller` sets the placed authority from that `Caller`.

### Place actor order

[x] `placeOrder` replies unsupported for `Order.ActorOrder`.

```fsharp
placeOrder : MailboxContext -> PlacedOrder -> Caller -> AsyncReplyChannel<Result<EventId, string>> -> unit
```

[ ] `placeOrder` calls Pool start and runs `order.actor` on the .NET thread pool.
[ ] The mailbox supplies `getGraph` when it builds `FunctionStart`. `ActorOrder` does not carry `getGraph`.
[ ] `FunctionStart.actorNodeId` is the `actorNodeId` Curried actor applied. `placeOrder` reads that id from the closure.
[ ] The `Caller` is the client's `Caller` on `Mailbox.PlaceOrder`.
[ ] A Desk Load or Save runs no actor.
[o] `placeOrder` accepts `Order.StartActor`, `Order.StartPeerActor`, or `Order.LoadSave`.
[o] `placeOrder` runs `stubActor`.
[o] `src/Server/ActorDispatch.fs`
[o] A new `CoreMsg` case.

### Pool start

[x] `startFunction` [o] `startActor` [o] `startPeerActor`

```fsharp
startFunction : FunctionStart -> Result<Credential, string>
startActor : ActorStart -> (unit -> Graph) -> Result<Credential, StartError>
startPeerActor : PeerActorName -> ActorStart -> (unit -> Graph) -> Result<Credential, string>
```

[x] `putFunctionStart` mints the Actor credential and the Actor session id.

```fsharp
putFunctionStart : (Credential -> NodeId -> NodeId -> string -> PendingBody -> unit) -> FunctionStart -> Result<Credential, string>
```

### Shared changes

[ ] `actorChanges` builds `ActorChanges` from `CoreChangesDoor`.

```fsharp
actorChanges : CoreChangesDoor -> ActorChanges
```

The pool `ActorFunction` keeps `CoreChangesDoor`. `actorChanges` adapts that door for the Shared `ActorFunction`. `postChanges` returns `Error` when the door is not writable.

### Pool launch

[x] `runSchedule` calls `Async.Start` with the row cancel token.

```fsharp
runSchedule : (Credential -> (PendingBody * LiveRow) option) -> Credential -> CoreChangesDoor -> unit
```

[ ] 15 — Delete the old Actor starts renames `runSchedule` to `launchPending`.

### Posted run

[x] `postActorStart` starts a posted `ActorStart` through `startChosen`.

```fsharp
postActorStart : MailboxContext -> Caller -> Ev -> ActorStart -> PostedReply -> unit
```

[ ] `postActorStart` reads `actorNodeId` from that Run, applies Curried actor, builds `ActorOrder`, and calls Post actor order with that `Caller`.

### Test command

[x] `functionActor` is the pool `FunctionActor`.
[ ] `functionActor` is the test command after Curried actor.

```fsharp
functionActor : NodeId -> ActorFunction
```

### AI actor

[x] `actorFn` returns `ActorFn`.
[ ] `actorFn` returns Curried actor. The closure holds `GrokBotBinding`.

```fsharp
actorFn : AiKeySet -> AiRepo list -> GrokBotBinding -> NodeId -> ActorFunction
```

The Actor session id stays minted by Pool start.

### Query command

[x] `postSearchRequest` records a search start.
[ ] `postSearchRequest` builds the query `ActorFunction` through Curried actor and calls Post actor order.

```fsharp
postSearchRequest : PersistenceContext -> HttpRequest -> Task<IResult>
```

### Load and save

[x] `postLoadSaveCommand` starts git Load or Save through `CoreMsg.StartLoadSaveCommand`.
[ ] `postLoadSaveCommand` builds the git `ActorFunction` through Curried actor and calls Post actor order.
[ ] A Desk Load or Save places `Order.ActorOrder` and runs no actor.

```fsharp
postLoadSaveCommand : LoadSaveCommandRouter -> CoreChangesDoor -> string -> Async<IResult>
```

### Run command

[x] `execRunOp` is the Run command. It calls `queueRun`, which calls `RunLaunch.queueStart`.

```fsharp
execRunOp : VM -> VM * Effect list
```

### Actor name

[x] `actorNameFromText` returns the word after `?`.

```fsharp
actorNameFromText : string -> string option
```

[x] `isCommandText` keeps its name. The Run error string stays `No command found`.

## 4. Flow paths

1. **Place an ActorOrder**

[ ] The endpoint applies Curried actor. Post actor order sends the client's Caller. Place actor order runs the `ActorFunction`.

```fsharp
Run command
Posted run
Curried actor
Post actor order
Place actor order
Pool start
Shared changes
Pool launch
```

2. **Run a query**

[ ] Query command posts `Order.ActorOrder`. Place actor order runs that `ActorFunction`.

```fsharp
Query command
Curried actor
Post actor order
Place actor order
Pool start
```

3. **Git Load or Save**

[ ] Load and save posts `Order.ActorOrder` for git. Place actor order runs that `ActorFunction`.

```fsharp
Load and save
Curried actor
Post actor order
Place actor order
Pool start
```

4. **Desk Load or Save**

[ ] Load and save posts `Order.ActorOrder`. Place actor order runs no actor.

```fsharp
Load and save
Post actor order
Place actor order
```

5. **Read an order from the wire**

[x] Wire actor order sets `actor` to `stubActor`.

```fsharp
Wire actor order
Actor order
Log actor order
```

6. **Query start today**

[x] `startChosen` builds the function inside the mailbox, then calls Pool start.

```fsharp
Posted run
Pool start
Pool launch
```

7. **Named start today**

[o] `dispatchStartActor` and `dispatchStartPeerActor` call `startActor` or `startPeerActor`.

```fsharp
Posted run
Pool start
Pool launch
```

## 5. Module map

1. **Actor function**
   - Interface: `src/Shared/History.fs` `ActorFunction`
   - Type `ActorFunction`, `FunctionRun`, `GraphCarrier`, and `Credential` in `src/Shared/History.fs`
   - [o] Type name `FunctionActor` in `src/Shared/History.fs`
   - Uses:
     1. [ ] Shared changes
2. **Curried actor**
   - Interface: `curriedActor : NodeId -> ActorFunction`
   - Uses:
     1. [ ] Actor function
3. **Actor order**
   - Interface: `src/Shared/History.fs` `Order.ActorOrder`
   - Type `ActorOrder` in `src/Shared/History.fs`
   - Uses:
     1. [x] Actor function
4. **Wire actor order**
   - Interface: `src/Shared/EventJson.fs` `stubActor`, `encodeActorOrder`, `decodeActorOrder`
   - Uses:
     1. [x] Actor order
5. **Log actor order**
   - Interface: `src/Shared/MailboxLog.fs` `historyBody`
   - Uses:
     1. [x] Actor order
6. **Post actor order**
   - Interface: `src/Server/Core/MailboxHost.fs` `postActorOrder`
   - Uses:
     1. [x] Actor order
7. **Place actor order**
   - Interface: `src/Server/Core/CoreMailboxLoop.fs` `placeOrder`
   - Uses:
     1. [ ] Post actor order
     2. [ ] Pool start
     3. [ ] Shared changes
     4. [ ] Pool launch
     5. [x] Wire actor order
8. **Pool start**
   - Interface: `src/Server/Core/CoreActorPool.fs` `startFunction`, `putFunctionStart`
   - Type `FunctionStart` and the pool `ActorFunction` in `src/Server/Core/CoreActorPool.fs`
   - [x] Type `ActorFn` in `src/Server/Core/CoreActorPool.fs`
   - [o] Type name `FunctionActor` in `src/Server/Core/CoreActorPool.fs`
   - Uses:
     1. [x] Actor function
9. **Shared changes**
   - Interface: `src/Server/Core/CoreDoor.fs` `actorChanges`
   - Type `ActorChanges` in `src/Shared/History.fs`
   - Uses:
     1. [x] Pool start
10. **Pool launch**
    - Interface: `src/Server/Core/CoreActorPool.fs` `runSchedule`
    - Uses:
      1. [x] Pool start
11. **Posted run**
    - Interface: `src/Server/Core/CoreMailboxActors.fs` `postActorStart`
    - Uses:
      1. [ ] Curried actor
      2. [ ] Post actor order
      3. [x] Pool start
12. **Test command**
    - Interface: `src/Server/TestActor.fs` `functionActor`
    - Uses:
      1. [ ] Curried actor
13. **AI actor**
    - Interface: `src/Server/RunAgentActor.fs` `actorFn`
    - Uses:
      1. [ ] Curried actor
      2. [x] Pool start
14. **Query command**
    - Interface: `src/Server/RouteRegistration.fs` `postSearchRequest`
    - Uses:
      1. [ ] Curried actor
      2. [ ] Post actor order
15. **Load and save**
    - Interface: `src/Server/Api.fs` `postLoadSaveCommand`
    - Uses:
      1. [ ] Curried actor
      2. [ ] Post actor order
      3. [ ] Place actor order
16. **Run command**
    - Interface: `src/Client/Commands.fs` `execRunOp`
    - Uses:
      1. [x] Actor name
      2. [ ] Posted run
17. **Actor name**
    - Interface: `src/Shared/CommandRequest.fs` `actorNameFromText`
    - Uses:
      1. [x] none

## 6. Seams

1. [x] **Log actor order** / **Actor order** — ``ActorOrder carries command name and FocusId`` in `tests/Shared.Tests/MailboxLogTests.fs`.
2. [x] **Wire actor order** / **Actor order** — ``ActorOrder round-trips on the wire with stubActor`` in `tests/Shared.Tests/EventJsonTests.fs`.
3. [x] **Pool start** / **Test command** — ``TestActor function start posts hello from Graph carrier`` in `tests/Server.Tests/CoreActorPoolFunctionStartTests.fs`.
4. [ ] **Place actor order** / **Pool start** — ``ActorOrder places the actor on the thread pool`` in `tests/Server.Tests/CoreMsgActorCasesTests.fs`.
5. [ ] **Place actor order** / **Load and save** — ``Desk load-save places its order and runs no Actor`` in `tests/Server.Tests/LoadSaveCommandTests.fs`.
6. [ ] **Test command** / **Place actor order** — ``TestActor hello posts one Owned child text hello under Focus`` in `tests/Server.Tests/TestActorHelloTests.fs`.

## 7. Slicing strategy

[x] expand-contract

## 8. Open questions

None
