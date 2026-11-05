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
[x] `CoreChangesDoor` stays in `src/Server/Core/CoreChangesDoor.fs`. It holds `MailboxHost` and `Caller`.
[ ] `ActorChanges` is the Shared door. It lives in `src/Shared/History.fs`.
[x] `MailboxHost` is in `src/Server/Core/MailboxHost.fs`. It holds `MailboxProcessor<CoreQueue>`, `PersistFilling`, and `Session`.
[x] `Caller` is in `src/Server/Core/CoreChanges.fs`. It holds `Authority`, a name, and `Credential`. `Authority` is in `src/Shared/History.fs`.

```fsharp
type GraphCarrier = unit -> Graph

type FunctionRun =
    { getGraph: GraphCarrier
      secret: Credential }

type FunctionActor = FunctionRun -> ActorChanges -> Async<unit>

type ActorChanges =
    { postChanges: Ev list -> Async<Result<unit, string>>
      actorStop: ActorResult -> Async<Result<unit, string>>
      getState: unit -> Async<Result<State, string>>
      loadNode: NodeId -> Async<Result<EventId, string>> }
```

[x] `FunctionStart`
[x] 17 — Rename commandId to actorNodeId: `ActorStart`, `ActorInput`, `FunctionStart`, and the pool live row use `actorNodeId`. The wire key stays `"commandId"`. The pool's one-live-Actor check uses that id. It runs before 06 — Log ActorOrder.

```fsharp
type FunctionStart =
    { actor: FunctionActor
      getGraph: GraphCarrier
      focusId: NodeId
      actorNodeId: NodeId }
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
      actorName: string }
```

[ ] `focusId` is the node that has focus when the command runs.
[x] Naming debt: `isCommandText` keeps its name. `isScanStopText` and `commandIsLive` still say command for the actor line. The Run error string stays `No command found`.

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

[ ] `actorFn` returns `FunctionActor`. The closure holds `GrokBotBinding`. `postActorStart` builds `ActorOrder` with it and calls `postActorOrder`.

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

[ ] `functionActor` is the test command `FunctionActor`. `postActorStart` builds `ActorOrder` with it and calls `postActorOrder`.

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

[ ] `encodeActorOrder` writes `focusId` and `actorName`. `actorName` stands in for `actor`.

```fsharp
encodeActorOrder : ActorOrder -> IEncodable
```

[ ] `decodeActorOrder` reads `focusId` and `actorName`. `actor` is `stubActor`.

```fsharp
decodeActorOrder : string -> Result<ActorOrder, string>
```

[ ] A wire-read `ActorOrder` is a log record, used like any other logged `Order` member. `stubActor` is never run.

### CoreDoor

[ ] `actorChanges` builds `ActorChanges` from `CoreChangesDoor`. `postChanges` returns `Error` when the door is not writable.

```fsharp
actorChanges : CoreChangesDoor -> ActorChanges
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

[ ] `postActorStart` takes the actor node id from the posted Run and closes over that id when it builds the `FunctionActor`. It builds `ActorOrder` and calls `postActorOrder`.

```fsharp
postActorStart : MailboxContext -> Caller -> Ev -> ActorStart -> PostedReply -> unit
```

### Commands

[x] `execRunOp` is the Run command. It calls `queueRun`, which calls `RunLaunch.queueStart`.

```fsharp
execRunOp : VM -> VM * Effect list
```

### CommandRequest

[x] `actorNameFromText` returns the actor name, the word after `?`.

```fsharp
actorNameFromText : string -> string option
```

## 4. Flow paths

1. **Run an actor function**

[ ] `postSearchRequest` builds `ActorOrder` and calls `postActorOrder`. The mailbox pull runs that actor function on the .NET thread pool.

```fsharp
postSearchRequest persistence req
ActorOrder actor getGraph focusId actorName
postActorOrder order
actorChanges door
Async.Start (actor { getGraph = getGraph; secret = secret } changes)
```

2. **Run a `?` command**

[ ] `execRunOp` posts the Run. `postActorStart` builds `ActorOrder` and calls `postActorOrder`.

```fsharp
execRunOp model
postActorStart context caller event request reply
postActorOrder order
```

3. **Query start today**

[x] `startChosen` builds the function inside the mailbox, then calls `startFunction`.

```fsharp
startChosen context request getState
functionStart request getState
startFunction start
```

4. **Named start today**

[o] `dispatchStartActor` and `dispatchStartPeerActor` pass `startActor` or `startPeerActor` through `prepareActorStart` and `finishReadyStart`.

```fsharp
dispatchStartActor context caller request reply
prepareActorStart context caller request start
startActor request getState
finishReadyStart context caller request secret make submissionId
schedule secret coreChanges
```

5. **Read an order from the wire**

[ ] `decodeActorOrder` sets `actor` to `stubActor`.

```fsharp
encodeActorOrder order
decodeActorOrder wire
```

## 5. Module map

[x] Commands is the module in `src/Client/Commands.fs`. `execRunOp` is the Run command.
[x] CommandRequest is the module in `src/Shared/CommandRequest.fs`. `actorNameFromText` reads the actor name.
[x] SearchActor is the module in `src/Server/SearchActor.fs`. `functionStart` builds the query closure.
[x] RunAgentActor is the module in `src/Server/RunAgentActor.fs`. `actorFn` builds the closure that holds `GrokBotBinding`.
[x] TestActor is the module in `src/Server/TestActor.fs`.
[x] Api is the module in `src/Server/Api.fs`.
[x] RouteRegistration is the module in `src/Server/RouteRegistration.fs`.
[x] EventJson is the module in `src/Shared/EventJson.fs`.
[ ] `FunctionActor`, `FunctionRun`, `GraphCarrier`, `Credential`, and `ActorChanges` are in `src/Shared/History.fs`.
[ ] CoreDoor is the module in `src/Server/Core/CoreDoor.fs`. `actorChanges` builds `ActorChanges`.
[x] `CoreChangesDoor` is in `src/Server/Core/CoreChangesDoor.fs`.
[x] MailboxHost is the module in `src/Server/Core/MailboxHost.fs`. The mailbox queue is `MailboxProcessor<CoreQueue>`.
[x] CoreMailboxActors is the module in `src/Server/Core/CoreMailboxActors.fs`. `postActorStart` starts a posted `ActorStart`.
[x] CoreActorPool is the module in `src/Server/Core/CoreActorPool.fs`.

## 6. Slicing strategy

[x] expand-contract

## 7. Open questions

None
