# Online search architecture

Spec: [spec](spec.md)
Updated: 2026-10-04
Sequence: expand-contract (proposed design, not a lock)

The Search spec and the Query spec stay separate. Dialog UI stays out of scope. File search stays out of scope. Locks are in the story paths. Where Alan asked for a design, the module map marks it **Proposed design**. [map](map.md) Decisions so far stays empty. This architecture does not resolve a ticket. It adds no claim under [doc/current](doc/current/).

Vocabulary: say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.

## 1. Story paths

1. **Residence hits first**
   1. [ ] **Find dialog** — [Search dialog](src/Client/SearchDialog.fs) shows the hits the Browser already has, from the existing incremental client search. Move uses that same first phase.

2. **Server completes the picture**
   1. [ ] **Actor** — Find and Move each have their own server support. An Actor walks the server Graph after the client phase. Proposed design: one Actor function outside Core, one running Actor per command. See §2 item 2 **Search Actor**.
   2. [ ] **Next page** — The Actor returns the next page only when the dialog asks for more. Proposed design: the ask is the shared mailbox message Next. See §2 item 1 **Continuation messages**.

3. **Duplicates on Node id**
   1. [ ] **Node id** — A client hit and a server hit are the same when they share a Node id. The server phase drops that id.
   2. [ ] **Shown ids** — Proposed design: the start message carries the Node ids the client already showed. The Actor omits those ids. This is not a separate protocol.

4. **Screen plus a page**
   1. [ ] **Dialog page** — One page is a screen plus about a page. Proposed design: the server returns the page the dialog asked for. This architecture adds no new page count.

5. **Resume when the dialog asks**
   1. [ ] **Ask** — The next page is sent only after the dialog asks. The Actor does not push a further page on its own.

6. **Skip trash**
   1. [ ] **Ordinary search skips trash** — Search skips trash unless it starts at the trash node.

7. **Start at the trash node**
   1. [ ] **TRASH** — The search starts in trash when its start Node is TRASH ([GraphBuild.trashId](src/Shared/GraphBuild.fs)).

8. **Dialog shows server hits**
   1. [ ] **Show N** — If the server finds N items, the Find dialog shows them. Move shows that same N.
   2. [ ] **Want nodes** — Proposed design: those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. No new package. See §2 item 4 **Want nodes for hits**.

9. **Move uses the same search**
   1. [ ] **Move Actor** — Move starts its own Actor and uses the same two-phase walk, the same Node-id dedup, and the same page rule as Find.

10. **Insert Refs under the query line**
    1. [ ] **Refs** — Results under the query line are Refs, not Owned Children.
    2. [ ] **Existing Ref shape** — [ExprRun](src/Shared/ExprRun.fs) `run` already materialises a Node answer with `ChildNode.reference`.
    3. [ ] **Post the Replace** — Proposed design: the query Actor posts a Change on the event source whose child list uses that Ref shape under the query line. Server eval is not the local eval inside `ExprRun.run`. See §2 item 3 **Query Actor**.

11. **Function limit**
    1. [ ] **Function's own limit** — The query function sets its own limit. The query does not use the Find dialog page. This architecture does not spell a new limit syntax.

12. **Query skips trash**
    1. [ ] **Ordinary query skips trash** — An ordinary query skips trash.

13. **Trash function**
    1. [ ] **trash** — The function is named `trash`. It works like `root` for reaching trash.

14. **Server items inserted**
    1. [ ] **Insert N** — The query expression inserts the N items the server finds, as Refs under the query line.
    2. [ ] **Nodes then Refs** — Proposed design: the same Poll carries the Want `nodes` for those ids and the Change that inserts the Refs, so a Ref points at a Node the Browser has.

15. **Server evaluates**
    1. [ ] **Server Graph** — The server evaluates the query. This path replaces the earlier local-eval hop.

### 16. Shared segments

1. **Continuation any Actor can use**
   1. [ ] **Mailbox** — A fast mailbox message asks for the next page. A fast mailbox message returns a page of Node ids. Proposed design: Next and Page on the existing mailbox. Other Actors reuse this pair. It is not a search-only protocol.
   2. [ ] **Actor thread** — The walk or the eval runs on the Actor, off the mailbox. The mailbox message clears fast. Same rule as [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md).
   3. [ ] **Want nodes** — The page's Node ids ride the existing Want answer `nodes` list.

2. **Query Change**
   1. [ ] **Page** — Query uses Page once, bounded by the function's own limit.
   2. [ ] **Ref Change** — Query also posts the Ref Replace on the event source.

### 17. Test seam

1. [ ] **Page of Node ids** — Proposed design: the narrowest shared point is the Page of Node ids. Find and Move tests install that page with [installWantAnswer](src/Shared/ResidentProjection.fs). Query tests also expect the Ref Replace under the query line.

## 2. Module map

1. **Continuation messages** — Proposed design, not a lock.
   Other Actors reuse these messages. Find, Move, and Query are the first callers. The Server git Actor stays the example of an Actor that posts to the mailbox while Core performs the mutation. These messages are not a Peer path and not a CAS.
   File: [CoreMsg](src/Server/Core/CoreMsg.fs)

   1. **State**
      1. [ ] **No new store** — The Event sequence stays the event source. Next and Page are mailbox messages. They are not a second log.
   2. **Interface**
      1. [ ] **Next** — A fast message. The dialog sends it only when it asks for more. It carries the Node ids the client already showed.
      2. [ ] **Page** — A fast message. It carries the next page of Node ids. The Actor omits ids the client already showed.
      3. [ ] **Clear fast** — Both messages finish in the mailbox processor. The walk stays on the Actor.
   3. **Uses**
      1. [ ] **Mailbox** — The existing mailbox. Core performs Graph Changes. The Actor posts them.
      2. [ ] **ActorStart** — One running Actor per command, recorded on the event source as ActorStart.

2. **Search Actor** — Proposed design, not a lock.
   Find and Move each start one Actor. The function is outside Core. There is no search actor under `src/Server` today. This names the proposed home.
   File: `src/Server/SearchActor.fs`

   1. **State**
      1. [ ] **Walk** — The Actor holds the walk off the mailbox.
      2. [ ] **Trash** — The walk skips trash unless the start Node is TRASH.
      3. [ ] **Dedup** — The Actor drops a Node id the client already showed.
   2. **Interface**
      1. [ ] **Start** — Find starts one Actor. Move starts another.
      2. [ ] **Page** — One page when Next arrives. No further page until the next Next.
   3. **Uses**
      1. [ ] **Continuation messages** — Next and Page.
      2. [ ] **Server Graph** — The Actor reads the server Graph.

3. **Query Actor** — Proposed design, not a lock.
   The server evaluates the query. The function limit is the query's own limit.
   File: `src/Server/SearchActor.fs`

   1. **State**
      1. [ ] **One result** — One Page, capped by the function's own limit.
      2. [ ] **Trash** — Ordinary eval skips trash. `trash` reaches trash the way `root` reaches ROOT.
   2. **Interface**
      1. [ ] **Eval** — The Actor evaluates the expression on the server Graph.
      2. [ ] **Refs** — The Actor posts a Change that inserts `ChildNode.reference` children under the query line. The shape matches [ExprRun](src/Shared/ExprRun.fs). The Actor does not call local `ExprRun.run` as the eval.
   3. **Uses**
      1. [ ] **Continuation messages** — Page.
      2. [ ] **Event source** — The Ref Replace is a Change on the event source.
      3. [ ] **ExprRun shape** — `ChildNode.reference` as in ExprRun's materialise path.

4. **Want nodes for hits** — Proposed design, not a lock.
   The found-Node list uses the Want package that already exists.
   File: [ResidentProjection](src/Shared/ResidentProjection.fs)

   1. **State**
      1. [ ] **Hit headers** — A hit Node is Resident after install. A hit with no new `childMap` key stays Unloaded for its Children.
   2. **Interface**
      1. [ ] **nodes** — The Page's Nodes are added to the Want answer `nodes` list.
      2. [ ] **childMap** — Ordinary Want edges stay `childMap`. A search page does not add a `childMap` key only to carry a hit Header.
      3. [ ] **Install** — [installWantAnswer](src/Shared/ResidentProjection.fs) merges `nodes` and `childMap` as it does today.
   3. **Uses**
      1. [ ] **Poll** — Post-Event and Poll already carry the Want answer. The page uses that carrier.
      2. [ ] **Page** — The mailbox attaches the Page's Nodes to that answer.

## 3. Seams

1. **Continuation messages**
   1. [ ] Interface on **Continuation messages**. Proposed design. Tests that page an Actor cross Next and Page.
2. **Want install**
   1. [ ] Interface on **Want nodes for hits**. Proposed design. [installWantAnswer](src/Shared/ResidentProjection.fs) is the install door.
3. **Ref Change**
   1. [ ] Interface on **Query Actor**. Proposed design. The event source applies the Ref Replace.
4. **Dialog UI**
   1. **Out of scope** — The Find dialog layout is out of scope. The dialog's existing ask for more is the Next send.

## 4. Alternative considered

1. **Search-only protocol** — A Find socket with its own cursor and its own payload. Other Actors could not reuse it. Alan asked for a message system other Actors can reuse, so this arrangement loses.
2. **New Want type** — A second answer beside `nodes` and `childMap`. [installWantAnswer](src/Shared/ResidentProjection.fs) already merges `nodes`. A second type is a wider interface. This arrangement loses.
3. **Winner** — Proposed design: Next and Page on the mailbox, hit Headers in the existing `nodes` list, and the query Ref Replace on the event source. Sequence expand-contract is the same proposal: add the messages and the `nodes` use beside today's Want, and keep the dialog.

## 5. Unsettled

1. **Spec file names** — The file names of the Search spec and the Query spec. [map](map.md) item 7 **Spec file names**. Both specs stay in [spec](spec.md).
2. **Limit syntax** — The query function sets its own limit. The spelling of that limit in the expression is not locked.
3. **Page count** — The lock is a screen plus about a page. No new integer is locked. The proposed design uses the page the dialog asks for.
4. **Message names** — Next and Page are proposed names. Alan did not lock the spellings.
5. **Actor file name** — `src/Server/SearchActor.fs` is the proposed home. Alan did not lock the file name.
6. **Tickets** — [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) and [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md) stay unresolved. The locks above are in this architecture. They are not ticket Answers.
