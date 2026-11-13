# Actor as client architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: `startFunction` is the one pool start, `Order.StartActor` of `ActorOrder` carries an `ActorFunction`, and the mailbox pull runs that function on the .NET thread pool.

Out of scope: remote starts, per-run endpoints, the `/state` route, and a PersistHandlers rename.

## 2. Types

[x] `GraphCarrier`, `FunctionRun`, `Credential`, `ActorChanges`, and `ActorOrder` are in `src/Shared/History.fs`.
[x] The actor field's type name is `ActorFunction` in `src/Shared/History.fs`.
[o] `FunctionActor`
[x] `ActorChanges.graphAtEvent` returns `GraphAtEvent`.

[x] `FunctionRun` today is `getGraph` and `secret`.

```fsharp
type GraphCarrier = unit -> Graph

type FunctionRun =
    { getGraph: GraphCarrier
      secret: Credential }

type ActorFunction = FunctionRun -> ActorChanges -> Async<ActorResult>

type ActorChanges =
    { postChanges: Ev list -> Async<Result<unit, string>>
      actorStop: ActorResult -> Async<Result<unit, string>>
      graphAtEvent: unit -> Async<Result<GraphAtEvent, string>>
      loadNode: NodeId -> Async<Result<EventId, string>> }

type ActorOrder =
    { actor: ActorFunction
      actorNodeId: NodeId
      actorName: string }
```

[x] `Order.StartActor` of `ActorOrder` carries `ActorOrder`. The log stores that member as-is.

```fsharp
| StartActor of ActorOrder
```

[o] `Order.RunActor`

[x] The pool actor type is in `src/Server/Core/CoreActorPool.fs`. Its name is `ActorFunction`. Its second argument is `CoreChangesDoor`.
[o] The pool name `FunctionActor`.

```fsharp
type ActorFunction = FunctionRun -> CoreChangesDoor -> Async<ActorResult>
```

[x] `FunctionStart` is in `src/Server/Core/CoreActorPool.fs`.
[ ] `FunctionStart.actor` is the pool `ActorFunction`.

```fsharp
type FunctionStart =
    { actor: ActorFunction
      getGraph: GraphCarrier
      actorNodeId: NodeId }
```

[x] `ActorFn` is in `src/Server/Core/CoreActorPool.fs`.
[o] `ActorFn` is the in-process start.

```fsharp
type ActorFn = ActorInput -> CoreChangesDoor -> Async<ActorResult>
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
ActorFunction : FunctionRun -> ActorChanges -> Async<ActorResult>
```

An Actor is a thread from the .NET thread pool. It runs an `ActorFunction`.

### Curried actor

[ ] Client start requests are their own types. Each kind of start has its own request type. The client never builds an `ActorOrder`. The client never builds an `ActorFunction`.
[ ] Each server handler converts its client request into `Order.StartActor` of `ActorOrder`. The handler fills in the curried `ActorFunction`. Every start then has the same shape on the server.

```fsharp
curriedActor : NodeId -> ActorFunction
```

[ ] An old-style actor takes `actorNodeId` and yields `ActorFunction`. The handler sets `actor`, `actorNodeId`, and `actorName` on that `ActorOrder` before Post actor order (`src/Shared/History.fs`).
[x] That conversion feeds Place actor order. The request goes to the pool. The pool launches the `ActorFunction` and affirms the start. The server posts `Order.StartActor` only after that affirmation. A refused start posts nothing. The server replies with the failure to the caller.

### Actor order

[x] `Order.StartActor` of `ActorOrder` is the logged order.

```fsharp
StartActor : ActorOrder -> Order
```

`actorNodeId` is the field on `ActorOrder`. `encodeActorOrder` writes that field under the wire key `focusId` (`src/Shared/EventJson.fs`). `actorName` is the word after `?`. `actorName` stands in for `actor` on the wire.

### Wire actor order

[x] `stubActor` is the actor on a wire read.

```fsharp
stubActor : ActorFunction
encodeActorOrder : ActorOrder -> IEncodable
decodeActorOrder : string -> Result<ActorOrder, string>
```

`encodeActorOrder` writes the wire key `focusId` from `actorNodeId`, and `actorName`. `decodeActorOrder` sets `actor` to `stubActor`. A wire-read `ActorOrder` is a log record. `stubActor` stays unrun.

### Log actor order

[x] `MailboxLog.historyBody` accepts `Order.StartActor` and `Order.ClientRequestActor` (`src/Shared/MailboxLog.fs`).

```fsharp
historyBody : Order -> Result<EventBody, string>
```

### Post actor order

[x] `postActorOrder` queues one `Order.StartActor`.

[o] `postActorOrder : MailboxHost -> ActorOrder -> unit`
[o] `Authority "Actor"` and `Credential ""` on this path.
[x] The post takes the client's `Caller`. The authority on the placed order is that caller's authority (`src/Server/Core/MailboxHost.fs`).

```fsharp
postActorOrder : MailboxHost -> Caller -> ActorOrder -> Async<Result<EventId, string>>
```

`Mailbox.placeForCaller` sets the placed authority from that `Caller`.

### Place actor order

[x] `placeOrder` runs `replyActorOrder` for `Order.StartActor` of `ActorOrder` (`src/Server/Core/CoreMailboxLoop.fs`).

```fsharp
placeOrder : MailboxContext -> PlacedOrder -> Caller -> AsyncReplyChannel<Result<EventId, string>> -> unit
```

[x] `replyActorOrder` sends the request to `startFunction`. `startFunction` admits the actor. `schedule` launches `order.actor` on the .NET thread pool. The `Ok` credential is the affirmation.
[x] A refusal comes from `admitFunctionStart`.
[x] The mailbox supplies `getGraph` when `scheduleActorOrder` builds `FunctionStart`. `ActorOrder` does not carry `getGraph`.
[x] `FunctionStart.actorNodeId` is `order.actorNodeId` (`scheduleActorOrder` in `src/Server/Core/CoreMailboxLoop.fs`).
[x] The `Caller` is the client's `Caller` on `Mailbox.PlaceOrder`.
[ ] The Neither case runs inside the Load actor. [23 — Run the Load actor for Git and Neither](issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md). Desktop_mapped waits for the WebDAV design on [20 — Move Desk Load and Desk Save](issues/20-move-desk-load-and-save/20-move-desk-load-and-save-ticket.md).
[x] `postPlacedOrder` returns `Error "ClientRequestActor is retired"` for `Order.ClientRequestActor` and does not post that order. `placeOrder` uses that same refusal.
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

[x] `startFunction` returns `Ok` secret when `admitFunctionStart` accepts the node. That `Ok` is the affirmation.
[x] `admitFunctionStart` returns `Error "actor node already has a live Actor"` when that node is live. `startFunction` returns that error.
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

### Posted run

[x] `postActorStart` starts `?test` through `queueAdmittedTest`, `?ai` through `queueAdmittedAi`, and a query Run through `queueAdmittedQuery`. Every other name replies `Error "unknown actor"` (`src/Server/Core/CoreMailboxActors.fs`).

```fsharp
postActorStart : MailboxContext -> Caller -> Ev -> ActorStart -> PostedReply -> unit
```

[x] `postActorStart` for `?test` reads `runDefinitionId`, applies `TestActor.functionActorAt`, builds `ActorOrder`, and calls `postActorOrder` with that `Caller`.
[x] `postActorStart` for `?ai` reads `runDefinitionId` from that Run, applies `RunAgentActor.actorFn` to that `ActorStart`, builds `ActorOrder`, and calls **Post actor order** with that `Caller`.
[x] `postActorStart` for a query Run calls `SearchActor.isQueryRequest`, applies `SearchActor.functionActorAt`, builds `ActorOrder` with `actorName` `query`, and calls **Post actor order**.

### Test command

[x] `functionActor` in `src/Server/TestActor.fs` is the pool `ActorFunction` from `src/Server/Core/CoreActorPool.fs`.
[x] `functionActorAt` is the test command. It takes `runDefinitionId` and `focusId` and returns the shared `ActorFunction`.

```fsharp
functionActorAt : NodeId -> NodeId -> ActorFunction
```

### AI actor

[o] `actorFn` returns `ActorFn`.
[x] `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs` is **Actor function** (`ActorFunction` in `src/Shared/History.fs`) for the `ActorStart` on that Run.

[ ] The wake POST sends the session id from `ActorInput.sessionId`.
[ ] `RunAgentActor.actorFn` in `src/Server/RunAgentActor.fs` reads `input.sessionId` and passes that value to the wake call.
[ ] `putActorStart` in `src/Server/Core/CoreActorPool.fs` mints that id onto `ActorInput.sessionId` and the live-row `sessionId`.
[ ] `deliver` and `takeInbox` in `src/Server/Core/CoreActorPool.fs` look up the live row by that session id.

### Cancel by focus node

[x] `cancelByFocus` takes the focus node id.
[ ] That node id is the actor handle. The live row leaves from that path.

### Query command

[x] `postSearchRequest` places Find and Move through `recordSearchStart`. That function posts `Order.StartActor`. The actor is `BootActors.search`, set by `RouteRegistration.CreateBoot` to `SearchActor.searchActorAt`.
[ ] `postSearchRequest` builds the query `ActorFunction` through Curried actor and calls Post actor order.

```fsharp
postSearchRequest : PersistenceContext -> HttpRequest -> Task<IResult>
```

### Load and save

[x] Git Load and Git Save call `GithubTransportActor.start`, which places `Order.StartActor` (`src/Server/RouteRegistration.fs`). Desk calls `CoreMailboxApi.startLoadSaveCommand`.
[ ] `postLoadSaveCommand` builds the git `ActorFunction` through Curried actor and calls Post actor order.
[ ] 20 — Move Desk Load and Desk Save takes Desktop_mapped callers off `CoreMsg.StartLoadSaveCommand` after 23 — Run the Load actor for Git and Neither. WebDAV has its own design effort first. The message is deleted at the end of that ticket.

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

```fsharp
Run command
Posted run
Curried actor
Post actor order
Pool start
Pool launch
Shared changes
Place actor order
```

2. **Run a query**

```fsharp
Query command
Curried actor
Post actor order
Pool start
Pool launch
Place actor order
```

3. **Git Load or Save**

```fsharp
Load and save
Curried actor
Post actor order
Pool start
Pool launch
Place actor order
```

4. **Desktop_mapped Load or Save**

[ ] Desktop_mapped Load and Save leave `CoreMsg.StartLoadSaveCommand` in 20 — Move Desk Load and Desk Save after the Git case and the Neither case are straight.

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

[x] `postActorStart` sends a query Run through `queueAdmittedQuery`, which places `Order.StartActor`. The admitted names are `test`, `ai`, and a query. Every other name replies `Error "unknown actor"`.

```fsharp
Posted run
Post actor order
Pool start
Pool launch
Place actor order
```

7. **Named start today**

[x] A placed `Order.ClientRequestActor` stays out of the mailbox log.

```fsharp
Place actor order
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
   - Interface: `src/Shared/History.fs` `Order.StartActor`
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
    - Interface: `src/Server/TestActor.fs` `functionActorAt`
    - Uses:
      1. [ ] Curried actor
13. **AI actor**
    - Interface: `src/Server/RunAgentActor.fs` `actorFn`
    - Uses:
      1. [x] Actor function
      2. [x] Pool start
14. **Query command**
    - Interface: `src/Server/RouteRegistration.fs` `postSearchRequest`
    - Uses:
      1. [ ] Curried actor
      2. [x] Post actor order
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
