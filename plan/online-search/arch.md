# Online search architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: Find and Move share one Search Actor and NodeId list; the globe requests it below the cap of 200; hit Headers ride Want `nodes`; Query evaluates remotely and inserts Refs.

Out of scope: dialog layout, file search, paging, a continuation cursor, and expression spelling for a stop under 200.

## 2. Types

[x] `SearchPicture.Match`, `SearchPicture.Request`, and `SearchPicture.Reply`.

```fsharp
type Match =
    | Text of string
    | Generation of int

type Request =
    { text: string
      startId: NodeId
      generation: int option }

type Reply =
    { ids: NodeId list
      matchKey: Match }
```

[x] `Api.SearchActorDoor`, `ActorStart`, and `ActorResult`. Search uses the root for all `ActorStart` Node ids. The wire key for `actorNodeId` remains `"commandId"`.

```fsharp
type SearchActorDoor =
    { changes: CoreChangesDoor
      recordStart: ActorStart -> Async<Result<unit, string>>
      recordStop: NodeId -> Async<unit> }

type ActorStart =
    { zoomId: NodeId
      focusId: NodeId
      actorNodeId: NodeId
      graphIds: NodeId list
      eventId: EventId }

type ActorResult =
    | ActorSucceeded
    | ActorFailed of string
    | ActorCancelled
    | ActorQuery of ids: NodeId list
```

[x] `SyncResponse`, `ChildNode`, and `Op.Replace`. Search Headers ride `nodes`; query results use `ChildNode.reference`.

```fsharp
type SyncResponse =
    { events: Ev list
      nodes: Node list
      childMap: Map<NodeId, ChildNode list> }
type ChildNode =
    { ref: Ownership
      id: NodeId }

| Replace of parentId: NodeId * oldChildren: ChildNode list * newChildren: ChildNode list
```

[x] `SearchDialogState` is in `src/Shared/ViewModel.fs`.

```fsharp
type SearchDialogState =
    { invokedCommand: string
      query: string
      selectedIndex: int
      returnTo: Mode
      onPick: NodeSearchResult -> VM -> VM * Effect list
      serverHitIds: NodeId list }
```

[ ] `SearchDialogState` adds globe selection.

[o] `SearchPicture.Gap`, `ArmSearchQuietGap`, and `CancelSearchQuietGap` are replaced by globe selection.

## 3. Interfaces

### ViewModelSearch

[x]

```fsharp
searchHitCap : int
startSearch : string -> NodeId -> Graph -> SearchCursor option
takeResults : int -> SearchCursor -> NodeSearchResult list * SearchCursor option
searchNodes : string -> NodeId -> Graph -> NodeSearchResult list
```

### SearchPicture

[x]

```fsharp
matchKeyOf : Request -> Match
```

[ ]

```fsharp
keepReply : Match -> NodeId list -> Reply -> NodeId list
```

### SearchDialog

[x]

```fsharp
openSearchDialogWithOnPick : string -> (NodeSearchResult -> VM -> VM * Effect list) -> VM -> VM * Effect list
currentSearchResults : VM -> NodeSearchResult list
applyServerReply : SearchPicture.Reply -> VM -> VM * Effect list
```

### SearchPictureClient

[x]

```fsharp
runSearch : (Msg -> unit) -> string -> NodeId -> unit
```

### Update

[x]

```fsharp
update : Msg -> VM -> VM * Effect list
```

### RouteRegistration

[x]

```fsharp
postSearchRequest : PersistenceContext -> HttpRequest -> Task<IResult>
```

### Api

[x]

```fsharp
postSearch : SearchActorDoor -> string -> Async<IResult>
```

### SearchActor

[x]

```fsharp
actorStart : Graph -> EventId -> ActorStart
reply : (unit -> Graph) -> SearchPicture.Request -> SearchPicture.Reply
functionStart : ActorStart -> GraphCarrier -> FunctionStart
```

### CoreMailboxApi

[x]

```fsharp
recordSearchStart : MailboxHost -> Caller -> ActorStart -> Async<Result<unit, string>>
recordSearchStop : MailboxHost -> Caller -> NodeId -> Async<Result<unit, string>>
```

### CommandRequest

[x]

```fsharp
tryQueryStart : Graph -> SiteMap -> NodeId -> NodeId -> EventId -> ActorStart option
```

### Commands

[x]

```fsharp
execRunOp : VM -> VM * Effect list
```

### RunLaunch

[x]

```fsharp
queueStart : ActorStart -> EventId -> SyncInfo -> Effect list -> SyncInfo * Effect list
```

### CoreMailboxActors

[x]

```fsharp
postActorStart : MailboxContext -> Caller -> Ev -> ActorStart -> PostedReply -> unit
```

### CoreActorPool

[x]

```fsharp
startFunction : FunctionStart -> Result<Credential, string>
```

### CoreDoor

[x]

```fsharp
actorStop : CoreChangesDoor -> ActorResult -> Async<Result<unit, string>>
```

[ ] Proposed Query Ref post.

```fsharp
postEvents : CoreChangesDoor -> Ev list -> Async<Result<CoreChangesAccepted, string>>
```

### ExprRun

[x]

```fsharp
answerNodeIds : NodeId -> Graph -> string -> NodeId list
```

### ResidentProjection

[x]

```fsharp
wantAnswer : Graph -> NodeId list -> Map<NodeId, ChildNode list> * Node list
installWantAnswer : Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>
```

### SyncLogic

[x]

```fsharp
applySyncResponse : SyncResponse -> ClientSyncState -> Result<ClientSyncState, string>
```

## 4. Flow paths

1. **Shared Find and Move path**

[ ] Find and Move open on one local NodeId list and use the same `onPick` seam. Edits stay local while the globe is clear. The walk stops at 200, skips trash, and includes trash only when it starts at `GraphBuild.trashId`.

```fsharp
openSearchDialogWithOnPick invokedCommand onPick model
update (NodeSearchQuery query) model
searchNodes query zoomRoot graph
currentSearchResults model
```

2. **Shared Search Actor path**

[ ] Below 200, the globe and later edits request one root-only server reply for Find or Move. The reply contains distinct Node ids, replaces only the matching list, and has no page or continuation cursor.

```fsharp
update (NodeSearchQuery query) model
runSearch dispatch query zoomRoot
postSearchRequest persistence request
postSearch door body
actorStart graph eventId
recordSearchStart host caller start
reply getGraph request
startSearch query graph.root graph
takeResults searchHitCap cursor
recordSearchStop host caller graph.root
keepReply currentMatch kept reply
applyServerReply reply model
currentSearchResults model
```

3. **Want nodes ride**

[ ] The shared reply ids become the Want ids. Hit Headers ride `SyncResponse.nodes`; no `childMap` entry exists only to carry a hit Header.

```fsharp
wantAnswer graph reply.ids
applySyncResponse response state
installWantAnswer childMap nodes graph
```

4. **Query Actor path**

[x] Run queues one remote evaluation. [ ] `answerNodeIds` stops at 200 or an earlier function stop, omits trash except through `trash`, and does not page.

```fsharp
execRunOp model
tryQueryStart graph siteMap zoomId focusId eventId
queueStart request eventId syncInfo effects
postActorStart context caller event request reply
functionStart request getGraph
startFunction start
answerNodeIds lineId graph line
actorStop door (ActorQuery ids)
```

5. **Proposed Query Ref post**

[ ] The Query Actor posts `Op.Replace` with `ChildNode.reference` results on the event source. The same response carries their Want `nodes`.

```fsharp
answerNodeIds lineId graph line
postEvents door events
wantAnswer graph ids
applySyncResponse response state
installWantAnswer childMap nodes graph
```

## 5. Module map

[x] ViewModelSearch is the module in `src/Shared/ViewModelSearch.fs`.
[x] SearchPicture is the module in `src/Shared/SearchPicture.fs`.
[x] SearchDialog is the module in `src/Client/SearchDialog.fs`.
[x] SearchPictureClient is the module in `src/Client/SearchPictureClient.fs`.
[x] Update is the module in `src/Client/Update.fs`.
[x] Api is the module in `src/Server/Api.fs`.
[x] RouteRegistration is the module in `src/Server/RouteRegistration.fs`.
[x] SearchActor is the module in `src/Server/SearchActor.fs`.
[x] CoreMailboxApi is the module in `src/Server/Core/CoreMailboxApi.fs`.
[x] CoreMailboxActors is the module in `src/Server/Core/CoreMailboxActors.fs`.
[x] CoreActorPool is the module in `src/Server/Core/CoreActorPool.fs`.
[x] CoreDoor is the module in `src/Server/Core/CoreDoor.fs`.
[x] CommandRequest is the module in `src/Shared/CommandRequest.fs`.
[x] Commands is the module in `src/Client/Commands.fs`.
[x] RunLaunch is the module in `src/Shared/RunLaunch.fs`.
[x] ExprRun is the module in `src/Shared/ExprRun.fs`.
[x] ExprPrimitive is the module in `src/Shared/ExprPrimitive.fs`.
[x] ResidentProjection is the module in `src/Shared/ResidentProjection.fs`.
[x] SyncLogic is the module in `src/Shared/SyncLogic.fs`.
[x] `ActorStart`, `ActorResult`, `Op.Replace`, and `EventBody.Change` are in `src/Shared/History.fs`.
[x] `SyncResponse` is in `src/Shared/ApiResponses.fs`.
[x] `ChildNode.reference` is in `src/Shared/Model.fs`.
[x] `GraphBuild.trashId` is in `src/Shared/GraphBuild.fs`.
[ ] `SearchDialogState` holds globe selection in `src/Shared/ViewModel.fs`.

## 6. Slicing strategy

[x] tracer-cut

## 7. Open questions

1. **Server trash argument** — Which argument carries `request.startId = GraphBuild.trashId` into the walk while server zoom remains root-only?
