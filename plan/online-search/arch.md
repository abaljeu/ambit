# Online search architecture

Spec: [spec](spec.md)
Updated: 2026-10-07
Sequence: tracer-cut

The Search spec and the Query spec stay separate sections of [spec](spec.md). Both specs live in that file. Dialog UI stays out of scope. File search stays out of scope. Paging and a continuation cursor are out of scope. Locks are in the story paths and in Decisions so far below. Where a mechanism is still a design, the module map marks it **Proposed design**. This architecture does not resolve a ticket. to-arch step 5 is complete for committed Module map items. Each committed module has a doc/current home, and this arch links to it: [Search Actor](../../doc/current/search-actor.md), [Query Actor](../../doc/current/query-actor.md), and [Want nodes for hits](../../doc/current/want-nodes.md). The Query Actor Ref post is a proposed design and has no doc/current home. The file `src/Server/SearchActor.fs` is the locked home of both Actors.

Vocabulary: say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.

**Decisions so far**. The same locks are on [map](map.md) Decisions so far.

1. **One spec file** — Both specs live in [spec](spec.md). The Search spec and the Query spec stay separate sections of that file.
2. **No limit syntax** — A query function may stop under 200. The server stops at 200. This architecture does not add expression spelling for that stop.
3. **Actor file** — `src/Server/SearchActor.fs` is the home of the Search Actor and the Query Actor.
4. **Sequence** — tracer-cut.
5. **Node id** — The identity of a result is the NodeId. The server list lists each Node id once. Answer: [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md). See item 10 **Globe**.
6. **Remote query eval** — Query eval is remote. The Query Actor evaluates on the server. Answer: [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) is not edited.
7. **Standing locks** — The cap of 200, the globe on the search bar, the shared Find and Move Actor, no paging, the trash rules, and Want nodes for hits stay locks. The short quiet gap is not a Find lock. The Query Ref post stays a proposed design.
8. **Shared search algorithm** — One search algorithm serves Find and Move. Step 1: the client algorithm stops at 200 hits. That work is [14 — Cap of 200](issues/14-cap-of-200.md) section 1 **Find and Move**. Step 2: the Search Actor reuses that algorithm. Today [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) take the search text, the zoom, and the Graph. The reply holds up to 200 hits. One reply and no continuation cursor stay a seam. The Find start is item 10 **Globe**. The query cap stays on [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap** and is blocked by [11 — Server evaluates](issues/11-server-evaluates.md) only.
9. **Root only** — Search does not take a focus. The server walk uses the root as the zoom. Actor Start supplies the root and the full server Graph. `zoomId`, `commandId`, and `graphIds` are the root. The shared `focusId` field holds that same root so ActorStop can pair with the start.
10. **Globe** — Alan, 2026-10-07. The Find dialog opens on a local search and shows N hits. When N is under 200, the globe on the search bar can request the server. When the search text is unchanged, the server reply replaces the client list. When the person edits the text, the search is local or server according to whether the globe is selected. This supersedes the quiet-gap Start. [08 — Duplicates on Node id](issues/08-duplicates-on-node-id.md) is superseded by [21 — Globe requests the server](issues/21-globe-requests-the-server.md). The server list lists each Node id once. The start does not carry shown Node ids.
11. **Want nodes** — Alan, 2026-10-07. A server reply with Find results includes the found Nodes as Want-fulfillment. Those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. There is no new package. Move uses that same reply. The Query Ref post stays a proposed design. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

## 1. Story paths

1. **Residence hits first**
   1. [ ] **Keypress** — While the globe is not selected, every keypress recomputes on the client only and updates [Search dialog](src/Client/SearchDialog.fs) immediately. No server message goes out on that keypress. This local incremental search is a lock. Claim home: [Workspace graph](../../doc/current/graph.md) (Reference search).
   2. [ ] **Move keypress** — While the globe is not selected, and when Move recomputes on each keypress the same way, Move uses this same client path.

2. **Globe requests the server**
   1. [ ] **Open stays local** — The dialog opens on a local search and shows N hits.
   2. [ ] **Globe** — When N is under 200, the globe on the search bar can request the server. When N is 200, that request is not sent. That request starts the shared Find and Move Actor. The Actor uses the same client search algorithm. Actor Start supplies the root and the full server Graph. The zoom is the root. Search does not take a focus. Move uses this Actor. Query does not use this globe. Query keeps its own Actor. This globe is a lock. See §2 item 1 **Search Actor**.
   3. [ ] **Replace** — When the search text is unchanged, the server reply replaces the client list. The server list lists each Node id once. The start does not carry shown Node ids. A reply for an older search string does not replace the list. The reply matches the search text, or an equivalent generation of that text.
   4. [ ] **Edit follows the globe** — When the person edits the text, the search is local when the globe is not selected, and a server search when the globe is selected.
   5. [ ] **Up to 200 hits** — The reply holds at most 200 Node ids from that same algorithm, then the walk stops. The Actor posts that one reply. There is no continuation cursor.

3. **Cap of 200**
   1. [ ] **Client stop** — The client search algorithm returns at most 200 hits and then stops. Find and Move use that stop on the residence Graph. This step does not start the Search Actor. This cap is a lock.
   2. [ ] **Find and Move** — The Search Actor uses that same stop. The local list stops at 200. The server list stops at 200. Find and Move share that cap. This cap is a lock.
   3. [ ] **Query** — The query function may stop under 200. The server returns at most 200 for a query and then stops. If the function asks for more than 200, the server stops at 200. The query does not page. This cap is a lock. The query cap waits on [11 — Server evaluates](issues/11-server-evaluates.md) only.

4. **Skip trash**
   1. [ ] **Ordinary search skips trash** — Search skips trash unless it starts at the trash node.

5. **Start at the trash node**
   1. [ ] **TRASH** — The search starts in trash when its start Node is TRASH ([GraphBuild.trashId](src/Shared/GraphBuild.fs)).

6. **Dialog shows server hits**
   1. [ ] **Show N** — If the server finds N items, the Find dialog shows that server list in place of the client list. Move shows that same list. N is at most 200.
   2. [ ] **Want nodes** — Those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. There is no new package. This ride is a lock. See §2 item 3 **Want nodes for hits**. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

7. **Move uses the same search**
   1. [ ] **Same backend** — Move uses this same search. Find shows the hit list in the dialog. Move shows that same list. Both use the globe, the same server request, the same Want nodes ride, the same cap of 200, the same Node id list, and the same trash rule. This shared backend is a lock.

8. **Insert Refs under the query line**
   1. [ ] **Refs** — Results under the query line are Refs, not Owned Children.
   2. [ ] **Existing Ref shape** — [ExprRun](src/Shared/ExprRun.fs) `run` already materialises a Node answer with `ChildNode.reference`.
   3. [ ] **Post the Replace** — Proposed design: the Query Actor posts a Change on the event source whose child list uses that Ref shape under the query line. Server eval is story path 12 **Server evaluates**. See §2 item 2 **Query Actor**.

9. **Query skips trash**
    1. [ ] **Ordinary query skips trash** — An ordinary query skips trash.

10. **Trash function**
    1. [ ] **trash** — The function is named `trash`. It works like `root` for reaching trash.

11. **Server items inserted**
    1. [ ] **Insert N** — The query expression inserts the N items the server finds, as Refs under the query line. N is at most 200, and lower when the function stops under 200.
    2. [ ] **Nodes then Refs** — Proposed design: the same Poll carries the Want `nodes` for those ids and the Change that inserts the Refs, so a Ref points at a Node the Browser has.

12. **Server evaluates**
    1. [x] **Server Graph** — The Query Actor evaluates the query on the full server Graph once, when Run hits a Node whose text contains `=`, off the mailbox. The Actor then stops. Run queues that ActorStart on `syncInfo.pending` behind the edit. The post is `POST /ambit/changes` (alias `POST /ambit/events`). The list turn classifies from the post-edit State. The observable reply is Node ids on ActorStop. A keypress does not start this Actor. This eval is remote. Detail is [Server evaluates](issues/11-server-evaluates.md). The Ref post stays [16 — Insert Refs under the query line](issues/16-insert-refs-under-the-query-line.md).

### 13. Shared segments

1. **One reply**
   1. [ ] **Actor thread** — The walk runs on the shared Find and Move Actor, off the mailbox. The globe starts that Actor when N is under 200. An edit starts a server search when the globe is selected. A keypress does not start the Actor when the globe is not selected. The walk is the client search algorithm. Actor Start supplies the root and the full server Graph. The zoom is the root. Search does not take a focus. Query starts its own Actor when the line runs. Each Actor posts one reply and stops. Same clear-fast rule as [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
   2. [ ] **Cap** — The reply holds at most 200 Node ids.
   3. [ ] **Want nodes** — Those hit Headers ride the existing Want answer `nodes` list. This ride is a lock. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).

2. **Query Change**
   1. [ ] **Ref Change** — Query also posts the Ref Replace on the event source.
   2. [ ] **Lower limit** — When the function stops under 200, the result uses that stop. Otherwise the result stops at 200.

### 14. Test seam

1. [ ] **One result of Node ids** — Find and Move tests install one result of at most 200 Node ids with [installWantAnswer](src/Shared/ResidentProjection.fs). That install is a lock. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md). Query tests also expect the Ref Replace under the query line, and a function stop under 200. The Ref Replace stays a proposed design. That Ref expectation is [16 — Insert Refs under the query line](issues/16-insert-refs-under-the-query-line.md). [Server evaluates](issues/11-server-evaluates.md) observes Node ids. A provisional soft-stop in those tests does not close [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap**.

## 2. Module map

1. **Search Actor** — The shared backend is a lock. The file name is a lock.
   Find and Move share one running Actor. Query does not use that Actor. The function is outside Core. There is no search actor under `src/Server` today. The locked home is the file below. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs a Graph Change.
   File: `src/Server/SearchActor.fs`
   Claim home: [Search Actor](../../doc/current/search-actor.md)

   1. **State**
      Claim home: [Search Actor](../../doc/current/search-actor.md) Data.
      1. [ ] **One walk** — The Actor holds one walk, off the mailbox, and then stops. It keeps no continuation cursor.
      2. [ ] **Trash** — The walk skips trash unless the start Node is TRASH.
      3. [ ] **Node id** — The server list lists each Node id once. The reply replaces the client list. The Actor does not take shown Node ids.
      4. [ ] **Cap** — The walk uses the client search algorithm and stops at 200 hits. This cap is a lock.
   2. **Interface**
      Claim home: [Search Actor](../../doc/current/search-actor.md) Interface and Messages.
      1. [ ] **Start** — The globe on the search bar starts the shared Actor when N is under 200. An edit starts a server search when the globe is selected. Move does not start a second Actor. Actor Start supplies the root and the full server Graph. The zoom is the root. Search does not take a focus. The start does not carry shown Node ids. A keypress does not start the Actor when the globe is not selected.
      2. [ ] **Stop** — The Actor returns at most 200 hits and stops. When the search text is unchanged, the reply replaces the client list. The Actor posts one reply. There is no continuation cursor.
      3. [ ] **Reply match** — The result carries the search text it was computed for, or an equivalent generation. The dialog drops the result when that text is not current.
   3. **Uses**
      Claim home: [Search Actor](../../doc/current/search-actor.md) Uses.
      1. [ ] **Server Graph** — The Actor reads the full server Graph. The walk is the client search algorithm. That algorithm takes the search text, the zoom, and the Graph. Actor Start supplies the root and that Graph. The zoom is the root. Search does not take a focus.
      2. [x] **ActorStart** — The running Actor is recorded on the event source as ActorStart. `zoomId`, `commandId`, and `graphIds` are the root. The shared `focusId` field holds that same root. Search does not take a focus. The walk reads the full server Graph from State. One reply, then the queue puts ActorStop on the event source.
      3. [x] **Door** — `Api.SearchActorDoor` has `changes`, `recordStart`, and `recordStop`. The mailbox cases are `RecordSearchStart` and `RecordSearchStop`. `changes` reads State. `recordStart` records ActorStart. `recordStop` is a queue message. The queue puts ActorStop on the event source. The id is the root.

2. **Query Actor** — The separate Actor is a lock. The file name is a lock. The Ref post stays a proposed design.
   The Query Actor evaluates the query on the server. This eval is remote. Query does not share the Find and Move Actor. The same file starts this Actor. The cap of 200 is a lock.
   File: `src/Server/SearchActor.fs`
   Claim home: [Query Actor](../../doc/current/query-actor.md)

   1. **State**
      Claim home: [Query Actor](../../doc/current/query-actor.md) Data.
      1. [x] **One result** — One result, then stop. No page and no cursor.
      2. [ ] **Limit** — The function may stop under 200. A request above 200 stops at 200.
      3. [ ] **Trash** — Ordinary eval skips trash. `trash` reaches trash the way `root` reaches ROOT.
   2. **Interface**
      1. [x] **Eval** — The Actor evaluates the expression on the server Graph once, when Run hits a Node whose text contains `=`. The request is the existing Run ActorStart on the pending event list, behind the edit. The observable reply is Node ids on ActorStop. Detail: [Server evaluates](issues/11-server-evaluates.md). Claim home: [Query Actor](../../doc/current/query-actor.md) Interface and Messages.
      2. [ ] **Refs** — Proposed design. The Actor posts a Change that inserts `ChildNode.reference` children under the query line. The shape matches [ExprRun](src/Shared/ExprRun.fs). This Ref post has no doc/current home. The locked Ref shape is [Query Actor](../../doc/current/query-actor.md) Interface and Messages. Server eval is Interface item 1 **Eval**.
   3. **Uses**
      1. [ ] **Event source** — Proposed design. The Ref Replace is a Change on the event source. This proposed design has no doc/current home.
      2. [ ] **ExprRun shape** — `ChildNode.reference` as in ExprRun's materialise path. Claim home: [Query Actor](../../doc/current/query-actor.md) Messages.
      3. [x] **Search Actor** — Query has its own running Actor. It starts when the line runs. It does not use the Find and Move Actor or the Find globe. Claim home: [Query Actor](../../doc/current/query-actor.md) Job and Uses.

3. **Want nodes for hits** — This ride is a lock.
   A server reply with Find results includes the found Nodes as Want-fulfillment. Those hit Headers ride in the existing Want answer `nodes` list. There is no new package. The list is one reply of at most 200 Node ids, not a page. Move uses that same reply. The Query Ref post is not this module.
   File: [ResidentProjection](src/Shared/ResidentProjection.fs)
   Claim home: [Want nodes for hits](../../doc/current/want-nodes.md)

   1. **State**
      Claim home: [Want nodes for hits](../../doc/current/want-nodes.md) Data.
      1. [ ] **Hit headers** — A hit Node is Resident after install. A hit with no new `childMap` key stays Unloaded for its Children.
   2. **Interface**
      Claim home: [Want nodes for hits](../../doc/current/want-nodes.md) Interface.
      1. [ ] **nodes** — The result's Nodes are added to the Want answer `nodes` list.
      2. [ ] **childMap** — Ordinary Want edges stay `childMap`. The result does not add a `childMap` key only to carry a hit Header.
      3. [ ] **Install** — [installWantAnswer](src/Shared/ResidentProjection.fs) merges `nodes` and `childMap` as it does today.
   3. **Uses**
      Claim home: [Want nodes for hits](../../doc/current/want-nodes.md) Uses.
      1. [ ] **Poll** — Post-Event and Poll already carry the Want answer. The one reply uses that carrier.

## 3. Seams

1. **Want install**
   1. [ ] Interface on **Want nodes for hits**. This ride is a lock. [installWantAnswer](src/Shared/ResidentProjection.fs) is the install door. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).
2. **Ref Change**
   1. [ ] Interface on **Query Actor**. Proposed design. The event source applies the Ref Replace. This proposed design has no doc/current home.
3. **Cap**
   1. [ ] **200** — The client search algorithm stops at 200. The Search Actor uses that same stop. Find and Move share the cap of 200. Query stops at 200. This is a lock. A query function may stop lower. The local list stops at 200. The server list stops at 200. The globe sends no request when the local list already has 200 hits. Claim homes: [Search Actor](../../doc/current/search-actor.md), [Query Actor](../../doc/current/query-actor.md).
4. **Globe**
   1. [ ] **Globe on the search bar** — When N is under 200, the globe can request the server. When the search text is unchanged, the reply replaces the client list. An edit follows the globe. A reply for an older search string does not replace the list. This is a lock. Query does not use this seam. Claim homes: [Search Actor](../../doc/current/search-actor.md), [Query Actor](../../doc/current/query-actor.md), [Search](../../doc/current/search.md). [21 — Globe requests the server](issues/21-globe-requests-the-server.md) is the Find start.
5. **Dialog layout**
   1. **Out of scope** — The Find dialog layout is out of scope. The globe on the search bar is in scope. The dialog does not ask for a next page. While the globe is not selected, each keypress updates the hit list from the client.

## 4. Alternative considered

1. **Paging** — A continuation cursor, and mailbox messages Next and Page, so the dialog can ask for more. Alan locked no paging. That arrangement is out of this architecture.
2. **New Want type** — A second answer beside `nodes` and `childMap`. [installWantAnswer](src/Shared/ResidentProjection.fs) already merges `nodes`. A second type is a wider interface. This arrangement loses.
3. **Message per key** — A server request on every keypress. While the globe is not selected, the keypress path stays on the client. That arrangement stays.
4. **Quiet gap** — One server request after the search text is unchanged, with no globe. Alan replaced that Start with the globe. That arrangement is out.
5. **Winner** — The dialog opens on a local search and shows N hits. While the globe is not selected, each keypress stays on the client. The client search algorithm stops at 200 hits. When N is under 200, the globe on the search bar can request the server. That request uses the same algorithm on the full server Graph. Actor Start supplies the root and that Graph. The zoom is the root. Search does not take a focus. The reply holds up to 200 hits. When the search text is unchanged, the reply replaces the client list. An edit follows the globe. A reply for an older search string does not replace the list. The server list lists each Node id once. Hit Headers ride the existing `nodes` list. That ride is a lock. Query is one remote evaluation on its own Actor when the line runs. The Query Ref post stays a proposed design. Sequence is tracer-cut.
