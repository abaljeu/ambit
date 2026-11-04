# Actor as client architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: one function start for all Actor starts, the function in memory, and the log stores each Order member as-is.

Out of scope: remote starts and per-run endpoints.

## 2. Types

[x] `GraphCarrier`, `FunctionRun`, and `FunctionActor` are in `src/Server/Core/CoreActorPool.fs`.
[ ] `FunctionActor` moves to `src/Shared/History.fs`. `FunctionRun` and `GraphCarrier` move there with it.
[ ] `Credential` is `Credential of string` in `src/Server/Core/CoreChanges.fs`. It moves to `src/Shared/History.fs`. Moving it does not take `Caller`.
[x] `Graph` and `NodeId` are in `src/Shared/Model.fs`.
[x] `CoreChangesDoor` is in `src/Server/Core/CoreChangesDoor.fs`. `FunctionActor` names it. The door holds `MailboxHost` and `Caller`.
[x] `MailboxHost` is in `src/Server/Core/MailboxHost.fs`. It holds `MailboxProcessor<CoreQueue>`, `PersistFilling`, and `Session`.
[x] `Caller` is in `src/Server/Core/CoreChanges.fs`. It holds `Authority`, a name, and `Credential`. `Authority` is in `src/Shared/History.fs`.

```fsharp
type GraphCarrier = unit -> Graph

type FunctionRun =
    { getGraph: GraphCarrier
      secret: Credential }

type FunctionActor = FunctionRun -> CoreChangesDoor -> Async<unit>
```

[x] `FunctionStart`

```fsharp
type FunctionStart =
    { actor: FunctionActor
      getGraph: GraphCarrier
      focusId: NodeId
      commandId: NodeId }
```

[x] `FunctionPending` [o] `BagPending`

```fsharp
type PendingBody =
    | BagPending of actorFn: ActorFn * input: ActorInput
    | FunctionPending of actor: FunctionActor * getGraph: GraphCarrier * secret: Credential
```

[ ] `ActorOrder` is the order on the mailbox queue.

```fsharp
type ActorOrder =
    { actor: FunctionActor
      getGraph: GraphCarrier
      focusId: NodeId
      commandName: string }
```

[ ] `Order.ActorOrder` carries `ActorOrder`. The log stores that member as-is.

```fsharp
| ActorOrder of ActorOrder
```

[o] `Order.RunActor of commandName: string * focusId: NodeId`

[o] `ActorFn`

```fsharp
type ActorFn = ActorInput -> CoreChangesDoor -> Async<unit>
```

## 3. Interfaces

### SearchActor

[x] `functionStart` closes over the command node id.

```fsharp
functionStart : ActorStart -> GraphCarrier -> FunctionStart
```

### RunAgentActor

[o] `actorFn` closes over `GrokBotBinding`.

```fsharp
actorFn : AiKeySet -> AiRepo list -> GrokBotBinding -> ActorFn
```

[ ] `actorFn` returns `FunctionActor`. The closure holds `GrokBotBinding`. The AI handler builds `ActorOrder` and calls `postActorOrder`.

```fsharp
actorFn : AiKeySet -> AiRepo list -> GrokBotBinding -> FunctionActor
```

### CoreActorPool

[x] `startFunction` [o] `startActor` [o] `startPeerActor` [x] `schedule`

```fsharp
startFunction : FunctionStart -> Result<Credential, string>
startActor : ActorStart -> (unit -> Graph) -> Result<Credential, StartError>
startPeerActor : PeerActorName -> ActorStart -> (unit -> Graph) -> Result<Credential, string>
schedule : Credential -> CoreChangesDoor -> unit
```

[x] `putFunctionStart` mints the Actor credential and the Actor session id.

```fsharp
putFunctionStart : (Credential -> NodeId -> NodeId -> string -> PendingBody -> unit) -> FunctionStart -> Result<Credential, string>
```

[x] `runSchedule` calls `Async.Start` with the row cancel token.

[ ] 15 — Delete the old Actor starts renames `runSchedule` to `launchPending`.

```fsharp
runSchedule : (Credential -> (PendingBody * LiveRow) option) -> Credential -> CoreChangesDoor -> unit
```

### TestActor

[ ] `functionActor` is the test command `FunctionActor`. Its handler builds `ActorOrder` and calls `postActorOrder`.

```fsharp
functionActor : FunctionActor
```

### Api

[ ] `postLoadSaveCommand` constructs the git Load or Save `FunctionActor`, builds `ActorOrder`, and calls `postActorOrder`.

```fsharp
postLoadSaveCommand : LoadSaveCommandRouter -> CoreChangesDoor -> string -> Async<IResult>
```

### RouteRegistration

[ ] `postSearchRequest` constructs the query `FunctionActor`, builds `ActorOrder`, and calls `postActorOrder`.

```fsharp
postSearchRequest : PersistenceContext -> HttpRequest -> Task<IResult>
```

### EventJson

[ ] `stubActor` is the actor function when `ActorOrder` is read from the wire.

```fsharp
stubActor : FunctionActor
```

[ ] `encodeActorOrder` writes `ActorOrder` on the wire. The actor function stays off the wire.

```fsharp
encodeActorOrder : ActorOrder -> IEncodable
```

[ ] `decodeActorOrder` reads that wire form. `actor` is `stubActor`.

```fsharp
decodeActorOrder : string -> Result<ActorOrder, string>
```

### MailboxHost

[ ] `postActorOrder` puts one `ActorOrder` on the mailbox queue.

```fsharp
postActorOrder : ActorOrder -> unit
```

### CoreMailboxActors

[o] `dispatchStartActor` calls `startActor`. [o] `dispatchStartPeerActor` calls `startPeerActor`.

```fsharp
dispatchStartActor : MailboxContext -> Caller -> ActorStart -> AsyncReplyChannel<Result<unit, string>> -> unit
dispatchStartPeerActor : MailboxContext -> Caller -> PeerActorName -> ActorStart -> AsyncReplyChannel<Result<unit, string>> -> unit
```

[x] `prepareActorStart`

```fsharp
prepareActorStart : MailboxContext -> Caller -> ActorStart -> (ActorStart -> (unit -> Graph) -> Result<Credential option, StartError>) -> Result<Credential option * (Caller -> CoreChangesDoor), StartError>
```

[x] `finishReadyStart` appends `EventBody.ActorStart`, then calls `schedule` in that same mailbox step.

```fsharp
finishReadyStart : MailboxContext -> Caller -> ActorStart -> Credential -> (Caller -> CoreChangesDoor) -> Guid -> Result<Ev, string>
```

[x] `startChosen` calls `startFunction` for a query. `QueryActorTests` and `CoreActorPoolFunctionStartTests` call `startFunction` directly.

```fsharp
startChosen : MailboxContext -> ActorStart -> (unit -> Graph) -> Result<Credential option, StartError>
```

## 4. Flow paths

1. **Run an actor function**

[ ] `postSearchRequest` builds `ActorOrder` and calls `postActorOrder`. The mailbox pull runs that actor function on the .NET thread pool.

```fsharp
postSearchRequest persistence req
ActorOrder actor getGraph focusId commandName
postActorOrder order
Async.Start (actor { getGraph = getGraph; secret = secret } coreChanges)
```

2. **Query start today**

[x] `startChosen` builds the function inside the mailbox, then calls `startFunction`.

```fsharp
startChosen context request getState
functionStart request getState
startFunction start
```

3. **Named start today**

[o] `dispatchStartActor` and `dispatchStartPeerActor` pass `startActor` or `startPeerActor` through `prepareActorStart` and `finishReadyStart`.

```fsharp
dispatchStartActor context caller request reply
prepareActorStart context caller request start
startActor request getState
finishReadyStart context caller request secret make submissionId
schedule secret coreChanges
```

4. **Read an order from the wire**

[ ] `decodeActorOrder` sets `actor` to `stubActor`.

```fsharp
encodeActorOrder order
decodeActorOrder wire
```

## 5. Module map

[x] SearchActor is the module in `src/Server/SearchActor.fs`. `functionStart` builds the query closure.
[x] RunAgentActor is the module in `src/Server/RunAgentActor.fs`. `actorFn` builds the closure that holds `GrokBotBinding`.
[x] TestActor is the module in `src/Server/TestActor.fs`.
[x] Api is the module in `src/Server/Api.fs`.
[x] RouteRegistration is the module in `src/Server/RouteRegistration.fs`.
[x] EventJson is the module in `src/Shared/EventJson.fs`.
[ ] `FunctionActor`, `FunctionRun`, `GraphCarrier`, and `Credential` are in `src/Shared/History.fs`.
[x] `CoreChangesDoor` is in `src/Server/Core/CoreChangesDoor.fs`.
[x] MailboxHost is the module in `src/Server/Core/MailboxHost.fs`. The mailbox queue is `MailboxProcessor<CoreQueue>`.
[x] CoreMailboxActors is the module in `src/Server/Core/CoreMailboxActors.fs`.
[x] CoreActorPool is the module in `src/Server/Core/CoreActorPool.fs`.

## 6. Slicing strategy

[x] expand-contract

## 7. Open questions

open — Whether `CoreChangesDoor` moves to Shared is open. A move drags `MailboxHost` and `Caller`.
