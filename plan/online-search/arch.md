# Online search architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: Find and Move share one Search Actor, the globe requests that Actor when the local list is under 200, hit Headers ride the existing Want `nodes` list, and a query expression evaluates on the server.

Out of scope: dialog layout, file search, paging, a continuation cursor, a quiet-gap Start, and expression spelling for the stop under 200.

## 2. Types

[x] `SearchPicture.Match`, `SearchPicture.Request`, and `SearchPicture.Reply` are in `src/Shared/SearchPicture.fs`. The request carries `text`, `startId`, and optional `generation`. Shown Node ids stay off the request. The reply carries `ids` and `matchKey`.

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

[o] `SearchPicture.Gap` is the quiet-gap state.

```fsharp
type Gap =
    | Idle
    | Waiting of Match
```

[x] `SearchActor.Walk` is in `src/Server/SearchActor.fs`.

```fsharp
type Walk =
    { text: string
      zoomRoot: NodeId
      graph: Graph
      matchKey: SearchPicture.Match }
```

[x] `Api.SearchActorDoor` is in `src/Server/Api.fs`. `changes` reads State. `recordStart` records the start on the event source. `recordStop` is the queue message that records the stop.

```fsharp
type SearchActorDoor =
    { changes: CoreChangesDoor
      recordStart: ActorStart -> Async<Result<unit, string>>
      recordStop: NodeId -> Async<unit> }
```

[x] `ActorStart` and `ActorResult` are in `src/Shared/History.fs`. Find and Move `actorStart` sets `zoomId`, `focusId`, `actorNodeId`, and `graphIds` to the root. `focusId` holds that root so the stop pairs with the start. Search does not take a focus. The wire key for `actorNodeId` is `"commandId"`.

```fsharp
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

[x] `GraphCarrier`, `FunctionRun`, `FunctionActor`, and `FunctionStart` are in `src/Server/Core/CoreActorPool.fs`.

```fsharp
type GraphCarrier = unit -> Graph

type FunctionRun =
    { getGraph: GraphCarrier
      secret: Credential }

type FunctionActor = FunctionRun -> CoreChangesDoor -> Async<unit>

type FunctionStart =
    { actor: FunctionActor
      getGraph: GraphCarrier
      focusId: NodeId
      actorNodeId: NodeId }
```

[x] `SyncResponse` is in `src/Shared/ApiResponses.fs`. `nodes` and `childMap` are the Want answer.

```fsharp
type SyncResponse =
    { events: Ev list
      nodes: Node list
      childMap: Map<NodeId, ChildNode list> }
```

[x] `ChildNode` is in `src/Shared/Model.fs`. `reference` builds a Ref.

```fsharp
type ChildNode =
    { ref: Ownership
      id: NodeId }
```

[x] `Op.Replace` is the child-list Change in `src/Shared/History.fs`.

```fsharp
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

[ ] `SearchDialogState` holds whether the globe is selected.

[o] Quiet-gap effects in `src/Shared/ViewModelSync.fs`.

```fsharp
| RequestSearchPicture of text: string * zoomRoot: NodeId
| ArmSearchQuietGap of query: string
| CancelSearchQuietGap
```

[x] `GraphBuild.trashId` is the trash node. `ViewModelSearch.searchHitCap` is 200.

## 3. Interfaces

### ViewModelSearch

[x] The client algorithm stops at `searchHitCap`. The visited set lists each NodeId once.

```fsharp
searchHitCap : int
startSearch : string -> NodeId -> Graph -> SearchCursor option
takeResults : int -> SearchCursor -> NodeSearchResult list * SearchCursor option
searchNodes : string -> NodeId -> Graph -> NodeSearchResult list
```

[ ] `startSearch` includes trash when `zoomRoot` is `GraphBuild.trashId`. Otherwise the walk omits trash.

### SearchPicture

[x] `matchKeyOf` is `Generation` when `generation` is present, otherwise `Text`.

```fsharp
matchKeyOf : Request -> Match
encodeRequest : Request -> JsonValue
decodeRequest : Decoder<Request>
encodeReply : Reply -> JsonValue
decodeReply : Decoder<Reply>
```

[o] `awaitGap`, `startAfterGap`, and `applyReply`. `applyReply` keeps a tail that fills the room under `searchHitCap`.

```fsharp
awaitGap : int -> Match -> Gap
startAfterGap : Gap -> Match -> int -> Match option
applyReply : int -> Match -> Reply -> NodeId list option
keepReply : int -> Match -> NodeId list -> Reply -> NodeId list
```

[ ] `keepReply` returns the reply ids when `reply.matchKey` equals the current `Match`. A different `Match` returns the kept ids. Those ids replace the client list.

```fsharp
keepReply : Match -> NodeId list -> Reply -> NodeId list
```

### SearchDialog

[x] `openSearchDialogWithOnPick` opens Find or Move on the local list. `currentSearchResults` reads that local cache.

```fsharp
openSearchDialogWithOnPick : string -> (NodeSearchResult -> VM -> VM * Effect list) -> VM -> VM * Effect list
currentSearchResults : VM -> NodeSearchResult list
applyServerReply : SearchPicture.Reply -> VM -> VM * Effect list
```

[ ] While the globe is not selected, `currentSearchResults` is `searchNodes` on the residence Graph. A keypress calls `searchNodes`.

[ ] `applyServerReply` writes the server ids onto the dialog. `currentSearchResults` shows that server list in place of the client list.

[o] `searchPictureEffects` emits `RequestSearchPicture` after the quiet gap when the local count is under `searchHitCap`.

```fsharp
searchPictureEffects : string -> VM -> VM * Effect list
```

### SearchDialogView

[o] `armQuietGap` and `cancelQuietGap`.

```fsharp
armQuietGap : string -> (Msg -> unit) -> unit
cancelQuietGap : unit -> unit
```

### SearchPictureClient

[x] `runSearch` posts `SearchPicture.Request`. `startId` is the client zoom. `generation` is absent.

```fsharp
runSearch : (Msg -> unit) -> string -> NodeId -> unit
```

### Update

[x] `update` on `NodeSearchQuery` stores the query and clears `serverHitIds`.

```fsharp
update : Msg -> VM -> VM * Effect list
```

[ ] While the globe is not selected, that update calls `searchNodes`.

[ ] While the globe is selected and the local count is under `searchHitCap`, that update emits `RequestSearchPicture`. At `searchHitCap` the update stays on `searchNodes`.

[o] That update emits `ArmSearchQuietGap`.

### App

[x] `RequestSearchPicture` calls `runSearch`.

[o] `ArmSearchQuietGap` calls `armQuietGap`. `CancelSearchQuietGap` calls `cancelQuietGap`.

### RouteRegistration

[x] `postSearchRequest` reads the body and calls `postSearch`.

```fsharp
postSearchRequest : PersistenceContext -> HttpRequest -> Task<IResult>
```

### Api

[x] `postSearch` decodes the request, reads State, records the start, runs `reply`, records the stop, and returns the reply JSON. The stop id is the root.

```fsharp
postSearch : SearchActorDoor -> string -> Async<IResult>
```

### SearchActor

[x] Find and Move share this module and call `reply`. Query calls `functionStart`.

```fsharp
oneReply : Walk -> SearchPicture.Reply
actorStart : Graph -> EventId -> ActorStart
reply : (unit -> Graph) -> SearchPicture.Request -> SearchPicture.Reply
isQueryRequest : Graph -> ActorStart -> bool
functionStart : ActorStart -> GraphCarrier -> FunctionStart
```

[x] `reply` sets `zoomRoot` to `graph.root` and calls `startSearch` and `takeResults`. `oneReply` returns at most `searchHitCap` ids. The reply is one id list.

[x] `actorStart` sets `zoomId`, `focusId`, `actorNodeId`, and `graphIds` to `graph.root`.

[x] `isQueryRequest` is `CommandRequest.isQueryText` on `actorNodeId`.

[x] `functionStart` evaluates once on `getGraph`, then calls `CoreDoor.actorStop` with `ActorQuery`.

[ ] `reply` includes trash when `request.startId` is `GraphBuild.trashId`. Otherwise the walk omits trash.

[ ] The Find reply ids are the Want ids. `wantAnswer` builds `nodes`. No `childMap` key is added only to carry a hit Header.

### CoreMailboxApi

[x] `recordSearchStart` and `recordSearchStop`. The walk stays off this queue.

```fsharp
recordSearchStart : MailboxHost -> Caller -> ActorStart -> Async<Result<unit, string>>
recordSearchStop : MailboxHost -> Caller -> NodeId -> Async<Result<unit, string>>
```

### CoreEventDispatch

[x] `actorStart` commits `EventBody.ActorStart` on the event source. `actorStop` commits `EventBody.ActorStop`.

```fsharp
actorStart : Context -> Caller -> ActorStart -> Result<unit, string>
actorStop : Context -> Caller -> NodeId -> ActorResult -> Result<unit, string>
```

### CoreMailboxActors

[x] `startChosen` calls `functionStart` and `startFunction` for a query. `postActorStart` runs that choice for a posted Run.

```fsharp
startChosen : MailboxContext -> ActorStart -> (unit -> Graph) -> Result<Credential option, StartError>
postActorStart : MailboxContext -> Caller -> Ev -> ActorStart -> PostedReply -> unit
```

### CoreActorPool

[x] `startFunction` is the pool start for a query.

```fsharp
startFunction : FunctionStart -> Result<Credential, string>
```

### CoreDoor

[x] `getState`, `postEvents`, and `actorStop`. Errors are `Result` values.

```fsharp
getState : CoreChangesDoor -> Async<Result<State, string>>
postEvents : CoreChangesDoor -> Ev list -> Async<Result<CoreChangesAccepted, string>>
actorStop : CoreChangesDoor -> ActorResult -> Async<Result<unit, string>>
```

### CommandRequest

[x] A query line contains `=`. `isCommandText` excludes a `?` actor name.

```fsharp
isQueryText : string -> bool
tryQueryStart : Graph -> SiteMap -> NodeId -> NodeId -> EventId -> ActorStart option
```

### Commands

[x] `execRunOp` is the Run command. On a query line it calls `tryQueryStart`, then `queueStart`.

```fsharp
execRunOp : VM -> VM * Effect list
```

### RunLaunch

[x] `queueStart` queues `EventBody.ActorStart` on `syncInfo.pending`.

```fsharp
queueStart : ActorStart -> EventId -> SyncInfo -> Effect list -> SyncInfo * Effect list
```

### ExprRun

[x] `answerNodeIds` returns Node ids. `run` materialises a Node answer with `ChildNode.reference`.

```fsharp
answerNodeIds : NodeId -> Graph -> string -> NodeId list
run : NodeId -> Graph -> string -> Line
```

[ ] `answerNodeIds` returns at most `searchHitCap` ids. A function stop under that cap stays. Ordinary eval omits trash.

### ExprPrimitive

[x] The catalog row `root` returns the root node.

[ ] The catalog row `trash` returns `GraphBuild.trashId` the way `root` returns the root.

### ResidentProjection

[x] `installWantAnswer` merges `nodes` and `childMap`. `wantAnswer` builds that pair from ids.

```fsharp
wantAnswer : Graph -> NodeId list -> Map<NodeId, ChildNode list> * Node list
installWantAnswer : Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>
```

[ ] A hit Header stays off `childMap`. An absent `childMap` key leaves the Children Unloaded.

### SyncLogic

[x] `applySyncResponse` applies the event tail, then `installWantAnswer`.

```fsharp
applySyncResponse : SyncResponse -> ClientSyncState -> Result<ClientSyncState, string>
```

## 4. Flow paths

1. **Residence hits first**

[ ] The globe is not selected. Find and Move recompute on the client.

```fsharp
update (NodeSearchQuery query) model
searchNodes query zoomRoot graph
currentSearchResults model
```

2. **Globe requests the server**

[ ] The local count is under `searchHitCap` and the globe is selected. One reply. No continuation cursor.

```fsharp
update (NodeSearchQuery query) model
runSearch dispatch text zoomRoot
postSearchRequest persistence req
postSearch door body
getState door
actorStart graph eventId
recordSearchStart host caller start
reply getGraph request
startSearch text graph.root graph
takeResults count cursor
recordSearchStop host caller rootId
keepReply current kept reply
applyServerReply reply model
```

3. **Cap of 200**

[x] Find and Move stop at `searchHitCap` on the residence Graph and on `reply`.

```fsharp
searchNodes query zoomRoot graph
oneReply walk
```

[ ] A query stops at `searchHitCap`. A function stop under that cap stays.

```fsharp
answerNodeIds lineId graph line
```

4. **Skip trash**

[ ] An ordinary Find, Move, or query omits trash.

```fsharp
startSearch query zoomRoot graph
answerNodeIds lineId graph line
```

5. **Start at the trash node**

[ ] The client walk starts at `GraphBuild.trashId` when `zoomRoot` is that id.

```fsharp
startSearch query GraphBuild.trashId graph
```

6. **Dialog shows server hits**

[ ] The server list replaces the client list. Hit Headers ride `SyncResponse.nodes`.

```fsharp
wantAnswer graph reply.ids
applySyncResponse response state
installWantAnswer childMap nodes graph
applyServerReply reply model
currentSearchResults model
```

7. **Move uses the same search**

[ ] Move calls the same signatures as **Residence hits first** and **Globe requests the server**. `onPick` is the Move pick.

Move shares these with Find:

- the globe
- the Want `nodes` list
- the cap of 200
- the NodeId list
- the trash rule

```fsharp
openSearchDialogWithOnPick invokedCommand onPick model
searchNodes query zoomRoot graph
runSearch dispatch text zoomRoot
```

8. **Insert Refs under the query line**

[ ] The query actor posts `EventBody.Change` of `Op.Replace` through `postEvents`. Each result child is `ChildNode.reference`.

```fsharp
functionStart request getGraph
postEvents door events
```

9. **Query skips trash**

[ ] Ordinary query eval omits trash.

```fsharp
answerNodeIds lineId graph line
```

10. **Trash function**

[ ] The catalog row `trash` returns the trash node. The shape matches `root`.

```fsharp
answerNodeIds lineId graph line
```

11. **Server items inserted**

[ ] One `SyncResponse` carries the Want `nodes` for the result ids and the `Op.Replace` event.

```fsharp
postEvents door events
wantAnswer graph ids
applySyncResponse response state
installWantAnswer childMap nodes graph
```

12. **Server evaluates**

[x] Run on a line that contains `=` queues one start. The actor evaluates on the server Graph and stops with `ActorQuery`.

```fsharp
execRunOp model
tryQueryStart graph siteMap zoomId focusId eventId
queueStart request eventId syncInfo effects
postActorStart context caller event request reply
isQueryRequest graph request
functionStart request getGraph
startFunction start
answerNodeIds lineId graph line
actorStop door (ActorQuery ids)
```

13. **One reply**

[x] The Find and Move walk is `reply`. It posts one reply and stops.

[x] The query actor posts one `ActorQuery` and stops.

```fsharp
reply getGraph request
recordSearchStop host caller rootId
actorStop door (ActorQuery ids)
```

14. **Query Change**

[ ] The Ref Replace is the Change in **Insert Refs under the query line**. The id list is the stop in **Cap of 200**.

```fsharp
answerNodeIds lineId graph line
postEvents door events
```

15. **Test seam**

[x] A Find or Move test installs at most `searchHitCap` Node ids with `installWantAnswer`.

```fsharp
wantAnswer graph ids
installWantAnswer childMap nodes graph
```

[ ] A query test also expects the `Op.Replace` under the query line, and a function stop under `searchHitCap`.

```fsharp
postEvents door events
answerNodeIds lineId graph line
```

16. **Quiet gap**

[o] `ArmSearchQuietGap` waits, then `searchPictureEffects` emits `RequestSearchPicture`.

```fsharp
update (NodeSearchQuery query) model
armQuietGap query dispatch
searchPictureEffects armedText model
awaitGap clientHitCount key
startAfterGap gap current clientHitCount
runSearch dispatch text zoomRoot
applyReply clientHitCount current reply
```

## 5. Module map

[x] ViewModelSearch is the module in `src/Shared/ViewModelSearch.fs`.
[x] SearchPicture is the module in `src/Shared/SearchPicture.fs`.
[x] SearchDialog is the module in `src/Client/SearchDialog.fs`.
[x] SearchDialogView is the module in `src/Client/SearchDialogView.fs`.
[x] SearchPictureClient is the module in `src/Client/SearchPictureClient.fs`.
[x] Update is the module in `src/Client/Update.fs`.
[x] App is the module in `src/Client/App.fs`.
[x] Api is the module in `src/Server/Api.fs`.
[x] RouteRegistration is the module in `src/Server/RouteRegistration.fs`.
[x] SearchActor is the module in `src/Server/SearchActor.fs`. Find and Move use `reply`. Query uses `functionStart`.
[x] CoreMailboxApi is the module in `src/Server/Core/CoreMailboxApi.fs`.
[x] CoreEventDispatch is the module in `src/Server/Core/CoreEventDispatch.fs`.
[x] CoreMailboxActors is the module in `src/Server/Core/CoreMailboxActors.fs`.
[x] CoreActorPool is the module in `src/Server/Core/CoreActorPool.fs`. `startFunction` is the pool start for a query.
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
[ ] The globe selection is on `SearchDialogState` in `src/Shared/ViewModel.fs`.

## 6. Slicing strategy

[x] tracer-cut

## 7. Open questions

1. **Server trash argument** — `reply` passes `graph.root` into `startSearch`. The walk includes trash when `request.startId` is `GraphBuild.trashId`. The argument that carries that start into `startSearch` is undecided.
