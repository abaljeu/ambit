# Actor as client architecture

Marks: [x] exists in code today. [ ] required. [o] obsolescent.

## 1. Flow paths (developer-facing paths)

### 1. Set up the actor function

[x] `FunctionActor`

```fsharp
type GraphCarrier = unit -> Graph

type FunctionRun =
    { getGraph: GraphCarrier
      secret: Credential }

type FunctionActor = FunctionRun -> CoreChangesDoor -> Async<unit>
```

[x] `SearchActor.functionStart` closes over the command node id.

```fsharp
let functionStart (request: ActorStart) (getGraph: GraphCarrier) : FunctionStart =
    let lineId = request.commandId
    { actor =
        fun run changes ->
            async {
                let result = ActorQuery (queryIds run.getGraph lineId)
                let! _ = CoreDoor.actorStop (CoreDoor.forCaller (actorCaller run.secret) changes) result
                return ()
            }
      getGraph = getGraph
      focusId = request.focusId
      commandId = request.commandId }
```

[o] `RunAgentActor.actorFn` closes over `GrokBotBinding`. Its result type is `ActorFn`.

```fsharp
let actorFn (keys: AiKeySet) (repos: AiRepo list) (grok: GrokBotBinding) : ActorFn =
    fun input coreChanges ->
        async {
            let! result = runBody keys repos grok input coreChanges
            do! stop input coreChanges result
        }
```

[ ] A caller builds a `FunctionActor`. The closure holds the graph ids or the `GrokBotBinding`.

```fsharp
let actorFn (keys: AiKeySet) (repos: AiRepo list) (grok: GrokBotBinding) : FunctionActor
```

### 2. Give the function to the Core mailbox

[o] `dispatchStartActor` calls `pool.startActor` through `dispatchActorStartResult`.

```fsharp
let dispatchStartActor context caller request reply =
    dispatchActorStartResult context caller request reply (fun start getState ->
        context.pool.startActor start getState |> Result.map Some)
```

[o] `dispatchStartPeerActor` calls `pool.startPeerActor` through `dispatchActorStartResult`.

```fsharp
let dispatchStartPeerActor context caller peerName request reply =
    dispatchActorStartResult context caller request reply (fun start getState ->
        context.pool.startPeerActor peerName start getState
        |> Result.mapError StartError.Rejected
        |> Result.map Some)
```

[x] `prepareActorStart` admits the caller and runs that `start`.

```fsharp
let prepareActorStart
    (context: MailboxContext)
    (caller: Caller)
    (request: ActorStart)
    (start: ActorStart -> (unit -> Graph) -> Result<Credential option, StartError>)
    : Result<Credential option * (Caller -> CoreChangesDoor), StartError>
```

[x] `finishReadyStart` appends `EventBody.ActorStart`, then calls `pool.schedule` in that same mailbox step.

```fsharp
let finishReadyStart context caller request secret make submissionId =
    match
        CoreEventDispatch.appendLifecycle
            (CoreMailboxEvents.eventDispatchContext context)
            caller
            submissionId
            (EventBody.ActorStart request)
    with
    | Ok stored ->
        context.pool.schedule secret (make caller)
        Ok stored
    | Error err -> Error err
```

[x] `startChosen` calls `pool.startFunction` for a query. It builds the function inside the mailbox. `QueryActorTests` and `CoreActorPoolFunctionStartTests` call `startFunction` directly.

```fsharp
let startChosen context request getState =
    if SearchActor.isQueryRequest (getState ()) request then
        SearchActor.functionStart request getState
        |> context.pool.startFunction
        |> Result.mapError StartError.Rejected
        |> Result.map Some
    else
        context.pool.startActor request getState |> Result.map Some
```

[ ] `StartFunction` carries the actor function and `focusId` in memory. The function is not a log field.

```fsharp
| StartFunction of
    caller: Caller *
    actor: FunctionActor *
    getGraph: GraphCarrier *
    focusId: NodeId *
    commandId: NodeId *
    commandName: string *
    reply: AsyncReplyChannel<Result<unit, string>>
```

[ ] `dispatchStartFunction` calls `startFunction`, logs `commandName` and `focusId`, then `schedule`, in that same mailbox step.

```fsharp
let dispatchStartFunction
    (context: MailboxContext)
    (caller: Caller)
    (actor: FunctionActor)
    (getGraph: GraphCarrier)
    (focusId: NodeId)
    (commandId: NodeId)
    (commandName: string)
    (reply: AsyncReplyChannel<Result<unit, string>>)
    : unit =
    let start =
        { actor = actor
          getGraph = getGraph
          focusId = focusId
          commandId = commandId }
    match context.pool.startFunction start with
    | Error err -> reply.Reply(Error err)
    | Ok secret ->
        match context.coreChanges.Value with
        | None -> reply.Reply(Error "mailbox not initialized")
        | Some make ->
            // log commandName and focusId. actor is not in the log.
            context.pool.schedule secret (make caller)
            reply.Reply(Ok ())
```

[ ] The log stores `commandName` and `focusId` only. A log entry cannot hold a function.

open — `Order.RunActor of commandName: string * focusId: NodeId`. Tickets add that case. Alan has questioned it.

```fsharp
// open. Not decided.
| RunActor of commandName: string * focusId: NodeId
```

open — `src/Server/ActorDispatch.fs` maps `commandName` to a function. Alan has questioned that map.

```fsharp
// open. Not decided.
let actorFor (commandName: string) : FunctionActor
```

### 3. Pool

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

[x] `putFunctionStart` mints the Actor credential and the Actor session id.

```fsharp
let putFunctionStart
    (putLive: Credential -> NodeId -> NodeId -> string -> PendingBody -> unit)
    (request: FunctionStart)
    : Result<Credential, string> =
    let secret = Credential (Guid.NewGuid().ToString("N"))
    let sessionId = Guid.NewGuid().ToString()
    putLive secret request.focusId request.commandId sessionId
        (FunctionPending (request.actor, request.getGraph, secret))
    Ok secret
```

[x] `startFunction` [o] `startActor` [o] `startPeerActor` [x] `schedule`

```fsharp
type CoreActorPool =
    { startActor: ActorStart -> (unit -> Graph) -> Result<Credential, StartError>
      startFunction: FunctionStart -> Result<Credential, string>
      startPeerActor: PeerActorName -> ActorStart -> (unit -> Graph) -> Result<Credential, string>
      schedule: Credential -> CoreChangesDoor -> unit }
```

### 4. Run when the mailbox pulls the function

[x] `runSchedule` [ ] 15 — Delete the old Actor starts renames `runSchedule` to `launchPending`.

```fsharp
let runSchedule
    (takePendingBody: Credential -> (PendingBody * LiveRow) option)
    (secret: Credential)
    (coreChanges: CoreChangesDoor)
    : unit =
    match takePendingBody secret with
    | Some (pending, row) ->
        let work =
            match pending with
            | BagPending (actorFn, input) -> actorFn input coreChanges
            | FunctionPending (actor, getGraph, secret) ->
                actor { getGraph = getGraph; secret = secret } coreChanges
        Async.Start (work, row.cancel.Token)
    | None -> ()
```

[x] `schedule` is that function. `finishReadyStart` calls it in the same mailbox step, after the start is logged.

## 2. Module map

[x] SearchActor is the module in `src/Server/SearchActor.fs`. `functionStart` builds the query closure.
[x] RunAgentActor is the module in `src/Server/RunAgentActor.fs`. `actorFn` builds the closure that holds `GrokBotBinding`.
[x] CoreMailboxActors is the module in `src/Server/Core/CoreMailboxActors.fs`.
[x] CoreMailboxLoop is the module in `src/Server/Core/CoreMailboxLoop.fs`.
[x] CoreActorPool is the module in `src/Server/Core/CoreActorPool.fs`.

## 3. Slicing strategy

[x] expand-contract
