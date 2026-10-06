# Online search architecture

Spec: [spec](spec.md)
Updated: 2026-10-05
Sequence: tracer-cut

The Search spec and the Query spec stay separate sections of [spec](spec.md). Both specs live in that file. Dialog UI stays out of scope. File search stays out of scope. Paging and a continuation cursor are out of scope. Locks are in the story paths and in Decisions so far below. Where a mechanism is still a design, the module map marks it **Proposed design**. This architecture does not resolve a ticket. to-arch step 5 is complete for committed Module map items. Each committed module has a doc/current home, and this arch links to it: [Search Actor](../../doc/current/search-actor.md) and [Query Actor](../../doc/current/query-actor.md). Want nodes for hits is a proposed design and has no doc/current home. The Query Actor Ref post is a proposed design and has no doc/current home. The file `src/Server/SearchActor.fs` is the locked home of both Actors.

Vocabulary: say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.

**Decisions so far** (Alan, 2026-10-05). The same locks are on [map](map.md) Decisions so far.

1. **One spec file** — Both specs live in [spec](spec.md). The Search spec and the Query spec stay separate sections of that file.
2. **No limit syntax** — A query function may stop under 200. The server stops at 200. This architecture does not add expression spelling for that stop.
3. **Actor file** — `src/Server/SearchActor.fs` is the home of the Search Actor and the Query Actor.
4. **Sequence** — tracer-cut.
5. **Node id** — The identity of a result is the NodeId. A client hit and a server hit are the same hit when they share a NodeId. Answer: [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md).
6. **Remote query eval** — Query eval is remote. The Query Actor evaluates on the server. Answer: [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) is not edited.
7. **Standing locks** — The cap of 200, the short quiet gap, the shared Find and Move Actor, no paging, and the trash rules stay locks. Want nodes for hits and the Ref post stay proposed designs.
8. **Shared search algorithm** — One search algorithm serves Find and Move. Step 1: the client algorithm stops at 200 hits. That work is [14 — Cap of 200](issues/14-cap-of-200.md) section 1 **Find and Move**. Step 2: the Search Actor reuses that algorithm. Today [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) take the search text, the zoom, and the Graph. Actor Start supplies root, focus, and the full server Graph. The Search walk may ignore focus. [07 — Server completes the picture](issues/07-server-completes-the-picture.md) is blocked by that client stop. The reply holds up to 200 hits. One reply and no continuation cursor stay a seam. The query cap stays on [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap** and is blocked by [11 — Server evaluates](issues/11-server-evaluates.md) only.

## 1. Story paths

1. **Residence hits first**
   1. [ ] **Keypress** — Every keypress recomputes on the client only and updates [Search dialog](src/Client/SearchDialog.fs) immediately. No server message goes out on a keypress. This local incremental search is a lock. Claim home: [Workspace graph](../../doc/current/graph.md) (Reference search).
   2. [ ] **Move keypress** — When Move recomputes on each keypress the same way, it uses this same client path.

2. **Server completes the picture**
   1. [ ] **Quiet gap** — One server request fires after the search text has been unchanged for a short quiet gap. The gap has no millisecond value. If the text changes before the gap ends, the request is not sent. If the client already has 200 hits, the request is not sent. This is a lock.
   2. [ ] **Actor** — That one request starts the shared Find and Move Actor. The Actor uses the same client search algorithm. Actor Start supplies root, focus, and the full server Graph. The Search walk may ignore focus. Move uses this Actor. Query does not use this gap. Query keeps its own Actor. Proposed design: one Actor function outside Core for the shared backend. See §2 item 1 **Search Actor**. This shared backend is a lock.
   3. [ ] **Up to 200 hits** — The reply holds at most 200 Node ids from that same algorithm, then the walk stops. The Actor posts that one reply. There is no continuation cursor. Client hits plus this reply stop at 200.
   4. [ ] **Stale reply** — A reply for an older search string is ignored. The reply matches the search text, or an equivalent generation of that text. The dialog applies a reply only when it matches the current text. This is a lock.

3. **Duplicates on Node id**
   1. [ ] **Node id** — A client hit and a server hit are the same when they share a Node id. The server phase drops that id.
   2. [ ] **Shown ids** — The one request asks only for hits the client does not already have. The start message carries those Node ids. This carried list is a lock. The Actor omits those ids.

4. **Cap of 200**
   1. [ ] **Client stop** — The client search algorithm returns at most 200 hits and then stops. Find and Move use that stop on the residence Graph. This step does not start the Search Actor. This cap is a lock.
   2. [ ] **Find and Move** — The Search Actor uses that same stop. The combined client hits and server hits stop at 200. Find and Move share that cap. This cap is a lock.
   3. [ ] **Query** — The query function may stop under 200. The server returns at most 200 for a query and then stops. If the function asks for more than 200, the server stops at 200. The query does not page. This cap is a lock. The query cap waits on [11 — Server evaluates](issues/11-server-evaluates.md) only.

5. **Skip trash**
   1. [ ] **Ordinary search skips trash** — Search skips trash unless it starts at the trash node.

6. **Start at the trash node**
   1. [ ] **TRASH** — The search starts in trash when its start Node is TRASH ([GraphBuild.trashId](src/Shared/GraphBuild.fs)).

7. **Dialog shows server hits**
   1. [ ] **Show N** — If the server finds N items, the Find dialog shows them. Move shows that same N. N is at most 200.
   2. [ ] **Want nodes** — Proposed design: those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. No new package. See §2 item 3 **Want nodes for hits**.

8. **Move uses the same search**
   1. [ ] **Same backend** — When Move recomputes on each keypress the same way Find does, Move uses the shared Find and Move Actor. Find shows the hit list in the dialog. Move shows that same list. Both use the same server request, the same quiet gap, the same Node-id dedup, the same cap of 200, and the same trash rule. This shared backend is a lock.

9. **Insert Refs under the query line**
   1. [ ] **Refs** — Results under the query line are Refs, not Owned Children.
   2. [ ] **Existing Ref shape** — [ExprRun](src/Shared/ExprRun.fs) `run` already materialises a Node answer with `ChildNode.reference`.
   3. [ ] **Post the Replace** — Proposed design: the Query Actor posts a Change on the event source whose child list uses that Ref shape under the query line. The Query Actor evaluates on the server. See §2 item 2 **Query Actor**.

10. **Query skips trash**
    1. [ ] **Ordinary query skips trash** — An ordinary query skips trash.

11. **Trash function**
    1. [ ] **trash** — The function is named `trash`. It works like `root` for reaching trash.

12. **Server items inserted**
    1. [ ] **Insert N** — The query expression inserts the N items the server finds, as Refs under the query line. N is at most 200, and lower when the function stops under 200.
    2. [ ] **Nodes then Refs** — Proposed design: the same Poll carries the Want `nodes` for those ids and the Change that inserts the Refs, so a Ref points at a Node the Browser has.

13. **Server evaluates**
    1. [ ] **Server Graph** — The Query Actor evaluates the query on the server once, when the line runs. The Actor then stops. A keypress does not start this Actor. This eval is remote.

### 14. Shared segments

1. **One reply**
   1. [ ] **Actor thread** — The walk runs on the shared Find and Move Actor, off the mailbox. Find and Move start that Actor once, after the quiet gap, not on a keypress. The walk is the client search algorithm. Actor Start supplies root, focus, and the full server Graph. The Search walk may ignore focus. Query starts its own Actor when the line runs. Each Actor posts one reply and stops. Same clear-fast rule as [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
   2. [ ] **Cap** — The reply holds at most 200 Node ids.
   3. [ ] **Want nodes** — Those Node ids ride the existing Want answer `nodes` list.

2. **Query Change**
   1. [ ] **Ref Change** — Query also posts the Ref Replace on the event source.
   2. [ ] **Lower limit** — When the function stops under 200, the result uses that stop. Otherwise the result stops at 200.

### 15. Test seam

1. [ ] **One result of Node ids** — Proposed design: the narrowest shared point is that one result of at most 200 Node ids. Find and Move tests install it with [installWantAnswer](src/Shared/ResidentProjection.fs). Query tests also expect the Ref Replace under the query line, and a function stop under 200.

## 2. Module map

1. **Search Actor** — The shared backend is a lock. The file name is a lock.
   Find and Move share one running Actor. Query does not use that Actor. The function is outside Core. There is no search actor under `src/Server` today. The locked home is the file below. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs a Graph Change.
   File: `src/Server/SearchActor.fs`
   Claim home: [Search Actor](../../doc/current/search-actor.md)

   1. **State**
      Claim home: [Search Actor](../../doc/current/search-actor.md) Data.
      1. [ ] **One walk** — The Actor holds one walk, off the mailbox, and then stops. It keeps no continuation cursor.
      2. [ ] **Trash** — The walk skips trash unless the start Node is TRASH.
      3. [ ] **Dedup** — The Actor drops a Node id the client already showed.
      4. [ ] **Cap** — The walk uses the client search algorithm and stops at 200 hits. This cap is a lock.
   2. **Interface**
      Claim home: [Search Actor](../../doc/current/search-actor.md) Interface and Messages.
      1. [ ] **Start** — Find and Move start the same Actor after the quiet gap. Move does not start a second Actor. Actor Start supplies root, focus, and the full server Graph. The start message also carries the Node ids the client already showed. That carried list is a lock. A keypress does not start the Actor.
      2. [ ] **Stop** — The Actor returns at most 200 hits and stops. Client hits plus this reply stop at 200. The Actor posts one reply. There is no continuation cursor.
      3. [ ] **Reply match** — The result carries the search text it was computed for, or an equivalent generation. The dialog drops the result when that text is not current.
   3. **Uses**
      Claim home: [Search Actor](../../doc/current/search-actor.md) Uses.
      1. [ ] **Server Graph** — The Actor reads the full server Graph. The walk is the client search algorithm. That algorithm takes the search text, the zoom, and the Graph. Actor Start supplies root, focus, and that Graph. The walk may ignore focus.
      2. [x] **ActorStart** — The running Actor is recorded on the event source as ActorStart. The record supplies root and focus. `graphIds` is the root. The walk reads the full server Graph from State. One reply, then ActorStop.

2. **Query Actor** — The separate Actor is a lock. The file name is a lock. The Ref post stays a proposed design.
   The Query Actor evaluates the query on the server. This eval is remote. Query does not share the Find and Move Actor. The same file starts this Actor. The cap of 200 is a lock.
   File: `src/Server/SearchActor.fs`
   Claim home: [Query Actor](../../doc/current/query-actor.md)

   1. **State**
      Claim home: [Query Actor](../../doc/current/query-actor.md) Data.
      1. [ ] **One result** — One result, then stop. No page and no cursor.
      2. [ ] **Limit** — The function may stop under 200. A request above 200 stops at 200.
      3. [ ] **Trash** — Ordinary eval skips trash. `trash` reaches trash the way `root` reaches ROOT.
   2. **Interface**
      1. [ ] **Eval** — The Actor evaluates the expression on the server Graph. Claim home: [Query Actor](../../doc/current/query-actor.md) Interface and Messages.
      2. [ ] **Refs** — Proposed design. The Actor evaluates on the server. It posts a Change that inserts `ChildNode.reference` children under the query line. The shape matches [ExprRun](src/Shared/ExprRun.fs). This Ref post has no doc/current home. The locked Ref shape is [Query Actor](../../doc/current/query-actor.md) Interface and Messages.
   3. **Uses**
      1. [ ] **Event source** — Proposed design. The Ref Replace is a Change on the event source. This proposed design has no doc/current home.
      2. [ ] **ExprRun shape** — `ChildNode.reference` as in ExprRun's materialise path. Claim home: [Query Actor](../../doc/current/query-actor.md) Messages.
      3. [ ] **Search Actor** — Query has its own running Actor. It starts when the line runs. It does not use the Find and Move Actor or that quiet gap. Claim home: [Query Actor](../../doc/current/query-actor.md) Job and Uses.

3. **Want nodes for hits** — Proposed design, not a lock.
   The found-Node list uses the Want package that already exists. The list is one reply of at most 200 Node ids, not a page.
   File: [ResidentProjection](src/Shared/ResidentProjection.fs)
   This proposed design has no doc/current home.

   1. **State**
      1. [ ] **Hit headers** — A hit Node is Resident after install. A hit with no new `childMap` key stays Unloaded for its Children.
   2. **Interface**
      1. [ ] **nodes** — The result's Nodes are added to the Want answer `nodes` list.
      2. [ ] **childMap** — Ordinary Want edges stay `childMap`. The result does not add a `childMap` key only to carry a hit Header.
      3. [ ] **Install** — [installWantAnswer](src/Shared/ResidentProjection.fs) merges `nodes` and `childMap` as it does today.
   3. **Uses**
      1. [ ] **Poll** — Post-Event and Poll already carry the Want answer. The one reply uses that carrier.

## 3. Seams

1. **Want install**
   1. [ ] Interface on **Want nodes for hits**. Proposed design. [installWantAnswer](src/Shared/ResidentProjection.fs) is the install door. This proposed design has no doc/current home.
2. **Ref Change**
   1. [ ] Interface on **Query Actor**. Proposed design. The event source applies the Ref Replace. This proposed design has no doc/current home.
3. **Cap**
   1. [ ] **200** — The client search algorithm stops at 200. The Search Actor uses that same stop. Find and Move share the cap of 200. Query stops at 200. This is a lock. A query function may stop lower. Find and Move send no request when the client already has 200 hits. Claim homes: [Search Actor](../../doc/current/search-actor.md), [Query Actor](../../doc/current/query-actor.md).
4. **Quiet gap**
   1. [ ] **Short quiet gap** — One shared Find and Move request after the text is unchanged. No millisecond value. A text change before the gap ends sends nothing. A stale reply is ignored. This is a lock. Query does not use this seam. Claim homes: [Search Actor](../../doc/current/search-actor.md), [Query Actor](../../doc/current/query-actor.md).
5. **Dialog UI**
   1. **Out of scope** — The Find dialog layout is out of scope. The dialog does not ask for a next page. Each keypress still updates the hit list from the client.

## 4. Alternative considered

1. **Paging** — A continuation cursor, and mailbox messages Next and Page, so the dialog can ask for more. Alan locked no paging. That arrangement is out of this architecture.
2. **New Want type** — A second answer beside `nodes` and `childMap`. [installWantAnswer](src/Shared/ResidentProjection.fs) already merges `nodes`. A second type is a wider interface. This arrangement loses.
3. **Message per key** — A server request on every keypress. Alan locked the keypress path to the client. That arrangement is out.
4. **Winner** — Local recompute on each keypress. The client search algorithm stops at 200 hits. Find and Move share one server request after a short quiet gap, skipped when the text changes or the client already has 200 hits. That request uses the same algorithm on the full server Graph. Actor Start supplies root, focus, and that Graph. The Search walk may ignore focus. The reply holds up to 200 hits. Client hits plus that reply stop at 200. A stale reply is ignored. Hit Headers ride the existing `nodes` list. Query is one remote evaluation on its own Actor when the line runs. Sequence is tracer-cut.
