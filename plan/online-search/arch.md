# Online search architecture

Spec: [spec](spec.md)
Updated: 2026-10-04
Sequence: unsettled

The Search spec and the Query spec stay separate. Dialog UI stays out of scope. File search stays out of scope. Paging and a continuation cursor are out of scope. Locks are in the story paths. Where a mechanism is still a design, the module map marks it **Proposed design**. [map](map.md) Decisions so far stays empty. This architecture does not resolve a ticket. It adds no claim under [doc/current](doc/current/).

Vocabulary: say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.

## 1. Story paths

1. **Residence hits first**
   1. [ ] **Find dialog** — [Search dialog](src/Client/SearchDialog.fs) shows the hits the Browser already has. Move uses that same first phase.

2. **Server completes the picture**
   1. [ ] **Actor** — Find, Move, and Query each start their own running Actor. The Actor walks or evaluates on the server Graph and then stops. Proposed design: one Actor function outside Core. See §2 item 1 **Search Actor**.
   2. [ ] **One result** — The Actor returns one result and stops. There is no continuation cursor.

3. **Duplicates on Node id**
   1. [ ] **Node id** — A client hit and a server hit are the same when they share a Node id. The server phase drops that id.
   2. [ ] **Shown ids** — Proposed design: the start message carries the Node ids the client already showed. The Actor omits those ids.

4. **Cap of 200**
   1. [ ] **Find and Move** — Find returns at most 200 hits and then stops. Move returns at most 200 hits and then stops. This cap is a lock.
   2. [ ] **Query** — The query function may set a lower limit. The server returns at most 200 for a query and then stops. If the function asks for more than 200, the server stops at 200. The query does not page. This cap is a lock.

5. **Skip trash**
   1. [ ] **Ordinary search skips trash** — Search skips trash unless it starts at the trash node.

6. **Start at the trash node**
   1. [ ] **TRASH** — The search starts in trash when its start Node is TRASH ([GraphBuild.trashId](src/Shared/GraphBuild.fs)).

7. **Dialog shows server hits**
   1. [ ] **Show N** — If the server finds N items, the Find dialog shows them. Move shows that same N. N is at most 200.
   2. [ ] **Want nodes** — Proposed design: those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. No new package. See §2 item 3 **Want nodes for hits**.

8. **Move uses the same search**
   1. [ ] **Move Actor** — Move starts its own Actor and uses the same two-phase walk, the same Node-id dedup, and the same cap of 200 as Find.

9. **Insert Refs under the query line**
   1. [ ] **Refs** — Results under the query line are Refs, not Owned Children.
   2. [ ] **Existing Ref shape** — [ExprRun](src/Shared/ExprRun.fs) `run` already materialises a Node answer with `ChildNode.reference`.
   3. [ ] **Post the Replace** — Proposed design: the query Actor posts a Change on the event source whose child list uses that Ref shape under the query line. Server eval is not the local eval inside `ExprRun.run`. See §2 item 2 **Query Actor**.

10. **Query skips trash**
    1. [ ] **Ordinary query skips trash** — An ordinary query skips trash.

11. **Trash function**
    1. [ ] **trash** — The function is named `trash`. It works like `root` for reaching trash.

12. **Server items inserted**
    1. [ ] **Insert N** — The query expression inserts the N items the server finds, as Refs under the query line. N is at most 200, and lower when the function sets a lower limit.
    2. [ ] **Nodes then Refs** — Proposed design: the same Poll carries the Want `nodes` for those ids and the Change that inserts the Refs, so a Ref points at a Node the Browser has.

13. **Server evaluates**
    1. [ ] **Server Graph** — The server evaluates the query. The Actor then stops.

### 14. Shared segments

1. **One Actor result**
   1. [ ] **Actor thread** — The walk or the eval runs on that command's Actor, off the mailbox. The Actor posts its one result and stops. Same clear-fast rule as [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
   2. [ ] **Cap** — The result holds at most 200 Node ids.
   3. [ ] **Want nodes** — Those Node ids ride the existing Want answer `nodes` list.

2. **Query Change**
   1. [ ] **Ref Change** — Query also posts the Ref Replace on the event source.
   2. [ ] **Lower limit** — When the function sets a limit under 200, the result uses that limit. Otherwise the result stops at 200.

### 15. Test seam

1. [ ] **One result of Node ids** — Proposed design: the narrowest shared point is that one result of at most 200 Node ids. Find and Move tests install it with [installWantAnswer](src/Shared/ResidentProjection.fs). Query tests also expect the Ref Replace under the query line, and a function limit under 200.

## 2. Module map

1. **Search Actor** — Proposed design, not a lock.
   Find, Move, and Query each start their own running Actor. The function is outside Core. There is no search actor under `src/Server` today. This names the proposed home. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs a Graph Change.
   File: `src/Server/SearchActor.fs`

   1. **State**
      1. [ ] **One walk** — The Actor holds one walk, off the mailbox, and then stops. It keeps no continuation cursor.
      2. [ ] **Trash** — The walk skips trash unless the start Node is TRASH.
      3. [ ] **Dedup** — The Actor drops a Node id the client already showed.
      4. [ ] **Cap** — The result stops at 200 hits. This cap is a lock.
   2. **Interface**
      1. [ ] **Start** — Find starts one Actor. Move starts another. The start message carries the Node ids the client already showed. That carried list is a proposed design.
      2. [ ] **Stop** — The Actor returns one result of at most 200 hits and stops.
   3. **Uses**
      1. [ ] **Server Graph** — The Actor reads the server Graph.
      2. [ ] **ActorStart** — The running Actor is recorded on the event source as ActorStart.

2. **Query Actor** — Proposed design, not a lock.
   The server evaluates the query. The same proposed file starts this Actor. The cap of 200 is a lock.
   File: `src/Server/SearchActor.fs`

   1. **State**
      1. [ ] **One result** — One result, then stop. No page and no cursor.
      2. [ ] **Limit** — The function's own limit applies when it is under 200. A request above 200 stops at 200.
      3. [ ] **Trash** — Ordinary eval skips trash. `trash` reaches trash the way `root` reaches ROOT.
   2. **Interface**
      1. [ ] **Eval** — The Actor evaluates the expression on the server Graph.
      2. [ ] **Refs** — The Actor posts a Change that inserts `ChildNode.reference` children under the query line. The shape matches [ExprRun](src/Shared/ExprRun.fs). The Actor does not call local `ExprRun.run` as the eval.
   3. **Uses**
      1. [ ] **Event source** — The Ref Replace is a Change on the event source.
      2. [ ] **ExprRun shape** — `ChildNode.reference` as in ExprRun's materialise path.
      3. [ ] **Search Actor** — Same start and stop shape. Query has its own running Actor.

3. **Want nodes for hits** — Proposed design, not a lock.
   The found-Node list uses the Want package that already exists. The list is the one result of at most 200, not a page.
   File: [ResidentProjection](src/Shared/ResidentProjection.fs)

   1. **State**
      1. [ ] **Hit headers** — A hit Node is Resident after install. A hit with no new `childMap` key stays Unloaded for its Children.
   2. **Interface**
      1. [ ] **nodes** — The result's Nodes are added to the Want answer `nodes` list.
      2. [ ] **childMap** — Ordinary Want edges stay `childMap`. The result does not add a `childMap` key only to carry a hit Header.
      3. [ ] **Install** — [installWantAnswer](src/Shared/ResidentProjection.fs) merges `nodes` and `childMap` as it does today.
   3. **Uses**
      1. [ ] **Poll** — Post-Event and Poll already carry the Want answer. The one result uses that carrier.

## 3. Seams

1. **Want install**
   1. [ ] Interface on **Want nodes for hits**. Proposed design. [installWantAnswer](src/Shared/ResidentProjection.fs) is the install door.
2. **Ref Change**
   1. [ ] Interface on **Query Actor**. Proposed design. The event source applies the Ref Replace.
3. **Cap**
   1. [ ] **200** — Find, Move, and Query stop at 200. This is a lock. A query function may stop lower.
4. **Dialog UI**
   1. **Out of scope** — The Find dialog layout is out of scope. The dialog does not ask for a next page.

## 4. Alternative considered

1. **Paging** — A continuation cursor, and mailbox messages Next and Page, so the dialog can ask for more. Alan locked no paging. That arrangement is out of this architecture.
2. **New Want type** — A second answer beside `nodes` and `childMap`. [installWantAnswer](src/Shared/ResidentProjection.fs) already merges `nodes`. A second type is a wider interface. This arrangement loses.
3. **Winner** — One result of at most 200, then stop. Hit Headers ride the existing `nodes` list. The query posts the Ref Replace on the event source. Sequence stays unsettled.

## 5. Unsettled

1. **Spec file names** — The file names of the Search spec and the Query spec. [map](map.md) item 7 **Spec file names**. Both specs stay in [spec](spec.md).
2. **Limit syntax** — A query function may set a lower limit than 200. The spelling of that limit in the expression is not locked.
3. **Actor file name** — `src/Server/SearchActor.fs` is the proposed home. Alan did not lock the file name.
4. **Sequence** — tracer-cut, module-build, or expand-contract is not locked. The earlier expand-contract note was for Next and Page. Those messages are gone.
5. **Tickets** — [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) and [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md) stay unresolved. The locks above are in this architecture. They are not ticket Answers.
