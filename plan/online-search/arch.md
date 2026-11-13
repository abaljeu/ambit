# Online search architecture

Spec: [[spec.md]] (user-facing Projects only. An internal Project may have no spec.md.)

Marks: [x] exists today. [ ] required. [o] obsolescent.

## 1. Bounds

In scope: Find and Move share one Search Actor and NodeId list; hit Headers ride Want `nodes`; Query evaluates remotely and inserts Refs.

Out of scope: dialog layout, file search, paging, a continuation cursor, and expression spelling for a stop under 200.

## 2. Types

[x] `Match`

```fsharp
type Match =
    | Text of string
    | Generation of int
```

[x] `Request`

```fsharp
type Request =
    { text: string
      startId: NodeId
      generation: int option }
```

[x] `Reply`

```fsharp
type Reply =
    { ids: NodeId list
      matchKey: Match }
```

[x] `SearchActorDoor`

```fsharp
type SearchActorDoor =
    { changes: CoreChangesDoor
      recordStart: ActorStart -> Async<Result<unit, string>>
      recordStop: NodeId -> Async<unit> }
```

[x] `ActorStart`

```fsharp
type ActorStart =
    { zoomId: NodeId
      focusId: NodeId
      actorNodeId: NodeId
      graphIds: NodeId list
      eventId: EventId }
```

[x] `ActorResult`

```fsharp
type ActorResult =
    | ActorSucceeded
    | ActorFailed of string
    | ActorCancelled
    | ActorQuery of ids: NodeId list
```

[x] `SyncResponse`

```fsharp
type SyncResponse =
    { events: Ev list
      nodes: Node list
      childMap: Map<NodeId, ChildNode list> }
```

[x] `ChildNode`

```fsharp
type ChildNode =
    { ref: Ownership
      id: NodeId }
```

[x] `Replace`

```fsharp
| Replace of parentId: NodeId * oldChildren: ChildNode list * newChildren: ChildNode list
```

[x] `SearchDialogState`

```fsharp
type SearchDialogState =
    { invokedCommand: string
      query: string
      selectedIndex: int
      returnTo: Mode
      onPick: NodeSearchResult -> VM -> VM * Effect list
      serverHitIds: NodeId list }
```

## 3. Interfaces

### Local search

[x]

```fsharp
searchHitCap : int
startSearch : string -> NodeId -> Graph -> SearchCursor option
takeResults : int -> SearchCursor -> NodeSearchResult list * SearchCursor option
searchNodes : string -> NodeId -> Graph -> NodeSearchResult list
```

[x] `searchHitCap` is 200. The walk stops there.

[ ] Include versus omit compares a node to `GraphBuild.trashId`. The walk starts at that node when `zoomRoot` equals it.

[ ] `startSearch` keeps `string -> NodeId -> Graph -> SearchCursor option`.

### Search picture

[x]

```fsharp
matchKeyOf : Request -> Match
```

[ ]

```fsharp
keepReply : Match -> NodeId list -> Reply -> NodeId list
```

[ ] `keepReply` returns the reply ids when `reply.matchKey` equals the current `Match`. A different `Match` returns the kept ids.

### Search dialog

[x]

```fsharp
openSearchDialogWithOnPick : string -> (NodeSearchResult -> VM -> VM * Effect list) -> VM -> VM * Effect list
currentSearchResults : VM -> NodeSearchResult list
applyServerReply : int -> SearchPicture.Reply -> Node list -> VM -> VM * Effect list
```

[x] Find and Move open on one NodeId list and share `onPick`.

[ ] `applyServerReply` stores the server ids. The client adds the carried nodes to the residency Graph and then lists them. `serverOnlyRows` excludes Node ids the client search found. `currentSearchResults` keeps the residency-search hits and appends the remaining server hits. Alan, 2026-11-13.

### Search text edit

[x]

```fsharp
update : Msg -> VM -> VM * Effect list
```

[x] `NodeSearchQuery` stores the query and clears `serverHitIds`.

### Search transport

[x]

```fsharp
runSearch : (Msg -> unit) -> string -> NodeId -> unit
```

[x] `runSearch` posts `Request`. `startId` is the zoom root. `generation` is absent. The request carries the search text and that zoom root.

### Search route

[x]

```fsharp
postSearchRequest : PersistenceContext -> HttpRequest -> Task<IResult>
```

[x] `postSearchRequest` reads the body and calls Search door.

### Search door

[x]

```fsharp
postSearch : SearchActorDoor -> string -> Async<IResult>
```

[x] `postSearch` reads State through `changes`, records the start, runs Find and Move reply, records the stop at the root, and returns one reply. Errors are `Result` values.

### Root-only start

[x]

```fsharp
actorStart : Graph -> EventId -> ActorStart
```

[x] `zoomId`, `focusId`, `actorNodeId`, and `graphIds` are `graph.root`. `focusId` holds that root so the stop pairs with the start. Search does not take a focus. The wire key for `actorNodeId` remains `"commandId"`.

### Find and Move reply

[x]

```fsharp
reply : (unit -> Graph) -> SearchPicture.Request -> SearchPicture.Reply
```

[o] `reply` passes `graph.root` into the search. The walk starts at the workspace root.
[ ] `reply` starts the full-database search at the zoom root on `request.startId`. Include versus omit compares a node to `GraphBuild.trashId`.

[x] The reply is one list of distinct Node ids. There is no continuation cursor.

### Search event record

[x]

```fsharp
recordSearchStart : MailboxHost -> Caller -> ActorStart -> Async<Result<unit, string>>
recordSearchStop : MailboxHost -> Caller -> NodeId -> Async<Result<unit, string>>
```

[x] `recordSearchStart` commits `ActorStart` on the event source. `recordSearchStop` commits `ActorStop`. The walk stays off this queue.

### Run command

[x]

```fsharp
execRunOp : VM -> VM * Effect list
```

[x] Run on a line that contains `=` queues one start.

### Query line

[x]

```fsharp
tryQueryStart : Graph -> SiteMap -> NodeId -> NodeId -> EventId -> ActorStart option
```

[x] A query line contains `=`.

### Queue start

[x]

```fsharp
queueStart : ActorStart -> EventId -> SyncInfo -> Effect list -> SyncInfo * Effect list
```

[x] `queueStart` queues `ActorStart` on `syncInfo.pending`.

### Posted query start

[x]

```fsharp
postActorStart : MailboxContext -> Caller -> Ev -> ActorStart -> PostedReply -> unit
```

[x] `postActorStart` runs Query function for a posted Run, then Pool start.

### Query function

[x]

```fsharp
functionStart : ActorStart -> GraphCarrier -> FunctionStart
```

[x] `functionStart` evaluates once on the server Graph, then calls Actor stop with `ActorQuery`.

### Pool start

[x]

```fsharp
startFunction : FunctionStart -> Result<Credential, string>
```

[x] `startFunction` is the pool start for a query.

### Query answer

[x]

```fsharp
answerNodeIds : NodeId -> Graph -> string -> NodeId list
```

[ ] `answerNodeIds` returns at most 200 ids. A function stop under that cap stays. Ordinary eval omits a node equal to `GraphBuild.trashId`. The signature stays `NodeId -> Graph -> string -> NodeId list`. There is no page.

### Trash function

[ ] The catalog row `trash` returns `GraphBuild.trashId` the way `root` returns the root.

### Actor stop

[x]

```fsharp
actorStop : CoreChangesDoor -> ActorResult -> Async<Result<unit, string>>
```

[x] Errors are `Result` values. A query posts one `ActorQuery` and stops.

### Query ref post

[x]

```fsharp
postEvents : CoreChangesDoor -> Ev list -> Async<Result<CoreChangesAccepted, string>>
```

[ ] Proposed Query Ref post. The Query Actor posts `Op.Replace`. Each result child is `ChildNode.reference`. The same response carries the Want `nodes`.

### Want nodes

[x]

```fsharp
wantAnswer : Graph -> NodeId list -> Map<NodeId, ChildNode list> * Node list
installWantAnswer : Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>
```

[ ] The reply ids are the Want ids. Hit Headers ride `SyncResponse.nodes`. The client adds those nodes to the residency Graph and then lists the hits. No `childMap` entry exists only to carry a hit Header.

### Sync install

[x]

```fsharp
applySyncResponse : SyncResponse -> ClientSyncState -> Result<ClientSyncState, string>
```

[x] `applySyncResponse` applies the event tail, then Want nodes.

## 4. Flow paths

1. **Shared Find and Move path**

[ ] Find and Move share the residency-search hits.

```fsharp
Search dialog
Search text edit
Local search
Search dialog
```

2. **Shared Search Actor path**

[ ] Both searches start at the zoom root.

```fsharp
Search transport
Search route
Search door
Root-only start
Search event record
Find and Move reply
Local search
Search event record
Search picture
Search dialog
```

3. **Want nodes ride**

[ ] Hit Headers ride the existing Want `nodes` list.

```fsharp
Want nodes
Sync install
```

4. **Query Actor path**

[x] Run queues one remote evaluation. [ ] The answer keeps the cap and the trash compare.

```fsharp
Run command
Query line
Queue start
Posted query start
Query function
Pool start
Query answer
Actor stop
```

5. **Proposed Query Ref post**

[ ] The query results become Refs under the query line, with their Want `nodes`.

```fsharp
Query answer
Query ref post
Want nodes
Sync install
```

## 5. Module map

1. **Local search**
   - Interface: `src/Shared/ViewModelSearch.fs` `searchHitCap`, `startSearch`, `takeResults`, `searchNodes`
   - Compare target: `GraphBuild.trashId` in `src/Shared/GraphBuild.fs`
   - Uses:
     1. [x] none
2. **Search picture**
   - Interface: `src/Shared/SearchPicture.fs` `matchKeyOf`, `keepReply`
   - Type `Match`, `Request`, and `Reply` in `src/Shared/SearchPicture.fs`
   - [o] Type `Gap` in `src/Shared/SearchPicture.fs`
   - Uses:
     1. [x] none
3. **Search dialog**
   - Interface: `src/Client/SearchDialog.fs` `openSearchDialogWithOnPick`, `currentSearchResults`, `applyServerReply`
   - Type `SearchDialogState` in `src/Shared/ViewModel.fs`
   - Uses:
     1. [x] Local search
     2. [x] Search picture
4. **Search text edit**
   - Interface: `src/Client/Update.fs` `update`
   - Uses:
     1. [x] none
5. **Search transport**
   - Interface: `src/Client/SearchPictureClient.fs` `runSearch`
   - Uses:
     1. [x] Search picture
6. **Search route**
   - Interface: `src/Server/RouteRegistration.fs` `postSearchRequest`
   - Uses:
     1. [x] Search door
7. **Search door**
   - Interface: `src/Server/Api.fs` `postSearch`
   - Type `SearchActorDoor` in `src/Server/Api.fs`
   - Uses:
     1. [x] Root-only start
     2. [x] Search event record
     3. [x] Find and Move reply
8. **Root-only start**
   - Interface: `src/Server/SearchActor.fs` `actorStart`
   - Type `ActorStart` in `src/Shared/History.fs`
   - Uses:
     1. [x] none
9. **Find and Move reply**
   - Interface: `src/Server/SearchActor.fs` `reply`
   - Uses:
     1. [x] Local search
10. **Search event record**
    - Interface: `src/Server/Core/CoreMailboxApi.fs` `recordSearchStart`, `recordSearchStop`
    - Uses:
      1. [x] none
11. **Run command**
    - Interface: `src/Client/Commands.fs` `execRunOp`
    - Uses:
      1. [x] Query line
      2. [x] Queue start
12. **Query line**
    - Interface: `src/Shared/CommandRequest.fs` `tryQueryStart`
    - Uses:
      1. [x] none
13. **Queue start**
    - Interface: `src/Shared/RunLaunch.fs` `queueStart`
    - Uses:
      1. [x] none
14. **Posted query start**
    - Interface: `src/Server/Core/CoreMailboxActors.fs` `postActorStart`
    - Uses:
      1. [x] Query function
      2. [x] Pool start
15. **Query function**
    - Interface: `src/Server/SearchActor.fs` `functionStart`
    - Uses:
      1. [x] Query answer
      2. [x] Actor stop
16. **Pool start**
    - Interface: `src/Server/Core/CoreActorPool.fs` `startFunction`
    - Uses:
      1. [x] Query function
17. **Query answer**
    - Interface: `src/Shared/ExprRun.fs` `answerNodeIds`
    - Uses:
      1. [ ] Trash function
18. **Trash function**
    - Interface: `src/Shared/ExprPrimitive.fs`
    - Compare target: `GraphBuild.trashId` in `src/Shared/GraphBuild.fs`
    - Uses:
      1. [ ] none
19. **Actor stop**
    - Interface: `src/Server/Core/CoreDoor.fs` `actorStop`
    - Type `ActorResult` in `src/Shared/History.fs`
    - Uses:
      1. [x] none
20. **Query ref post**
    - Interface: `src/Server/Core/CoreDoor.fs` `postEvents`
    - Type `ChildNode` in `src/Shared/Model.fs`
    - Type `Replace` in `src/Shared/History.fs`
    - Uses:
      1. [ ] Query answer
      2. [ ] Want nodes
21. **Want nodes**
    - Interface: `src/Shared/ResidentProjection.fs` `wantAnswer`, `installWantAnswer`
    - Type `SyncResponse` in `src/Shared/ApiResponses.fs`
    - Uses:
      1. [x] none
22. **Sync install**
    - Interface: `src/Shared/SyncLogic.fs` `applySyncResponse`
    - Uses:
      1. [x] Want nodes

## 6. Seams

1. [x] **Find and Move reply** / **Local search** — `actor reply matches startSearch and takeResults` and `reply reads the carrier graph once and stops at 200` in `tests/Server.Tests/SearchActorTests.fs`. `reply walks from the request start` asserts the zoom hit, then the hit under the workspace root. The required start is the zoom root.
2. [x] **Search door** / **Find and Move reply** — `postSearch returns one reply from the server graph` in `tests/Server.Tests/SearchActorTests.fs`.
3. [x] **Search door** / **Root-only start** — `actorStart records the root` and `postSearch records ActorStart at the root` in `tests/Server.Tests/SearchActorTests.fs`.
4. [x] **Query function** / **Actor stop** — `ActorStop reads the carrier graph once` and `ActorStop ids are not the find walk` in `tests/Server.Tests/QueryActorTests.fs`.
5. [x] **Query function** / **Query answer** — `empty equals line stops with no node ids` in `tests/Server.Tests/QueryActorTests.fs`.
6. [x] **Sync install** / **Want nodes** — `applySyncResponse installs Want answer after Event tail` and `applySyncResponse Load Fetch answer installs through installWantAnswer` in `tests/Shared.Tests/SyncLogicTests.fs`.

## 7. Slicing strategy

[x] tracer-cut

## 8. Open questions

None
