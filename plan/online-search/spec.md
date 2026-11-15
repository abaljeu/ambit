# Online search

Two specs. They stay separate sections of this file. Both specs live here. There is no second spec file.

This file records the Destination, the Notes that still stand, and Alan's later locks. It does not resolve a map ticket. A mechanism Alan asked this project to design is marked **Proposed design** in [arch](arch.md). It is not a lock.

## 1. Search spec

### 1. Problem Statement

1. **Residence-only Find** — Find searches the Resident Graph. `searchNodes` walks that Graph from the zoom root. Hits that are only on the Server stay out of that search. Move uses those same residency-search hits today.
2. **Short keystroke** — A short keystroke such as the letter e must not return the whole Graph. The cap is 200 hits.

### 2. Solution

1. **Open stays local** — The Find dialog opens on the residency search. That search starts at the zoom root. Each keypress recomputes on the client and updates the dialog immediately. A keypress then sends no server message.
2. **Append** — Alan, 2026-11-13. When the search text is unchanged, the server results exclude Node ids the client search found, and the dialog appends those server hits. The server result list carries the found matching nodes on the Want answer `nodes` list. The client adds those nodes to the residency Graph and then lists them, including a hit that was absent from the residency before the reply. A reply for an older search string leaves the list. The reply matches the search text, or an equivalent generation of that text.
3. **One backend** — One server backend serves both Find and Move. Move has no separate server path and no separate Actor. File search is out of this spec. This shared backend is a lock.
4. **Want-fulfillment** — A server reply with Find results includes the found Nodes as Want-fulfillment. Those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. The client adds those nodes to the residency Graph and then lists the hits. There is no new package. This ride is a lock. Move uses that same reply when it uses this search.
5. **Node id** — The server list lists each Node id once. The dialog excludes Node ids the client search found when it appends the server hits. `SearchDialog.serverOnlyRows` is that exclusion. Displayed rows play no part, except that both searches start at the zoom root.
6. **Cap of 200** — The residency search stops at 200 client hits. The full-database search stops at 200 server hits. Find and Move share that cap. There is no paging. There is no continuation cursor. This cap is a lock.
7. **Trash** — Search skips trash unless it starts at the trash node. The trash node is TRASH.
8. **Move** — Move uses this same search. Find lists the residency-search hits and the appended server hits in the dialog. Move uses that same hit list. Both use the same server request, the same Want nodes ride, the same cap of 200, the same Node id list, and the same trash rule. File search is not in this spec.
9. **Cancel or replace** — Alan, 2026-11-15. Closing the Find dialog cancels its pending server search actor. A committed change of the search text cancels the pending actor for the earlier text and starts the actor for the new text. The cancel is the existing actor cancel on [27 — Dialog cancels or replaces the search actor](issues/27-dialog-cancels-or-replaces-search-actor.md). The stored row is `Notice.ActorStopped` of `ActorCancelled`. `CoreMsg` stays as it is.

### 3. User Stories

1. **Open stays local** — As a person, I want the Find dialog to open on the residency search, so that I see the client hits from that search.
2. **Keypress stays local** — As a person, I want each keypress to recompute on the client, so that typing does not wait on the server.
3. **Already 200** — As a person, I want no server request when the residency search already has 200 client hits, so that the search stops at 200.
4. **Append** — As a person, I want the dialog to keep the residency-search hits and append server hits whose Node ids the client search did not find, after the client adds the carried nodes to the residency Graph, so that I see those client hits and those server hits.
5. **Stale reply** — As a person, I want a server reply for an older search string to leave the list, so that the dialog lists hits for the current search text.
6. **Node id** — As a person, I want the dialog to list each Node id once, so that a Node id the client search found stays one hit after the server reply.
7. **Cap of 200** — As a person, I want each Find search to stop at 200 hits, so that a short keystroke such as the letter e does not return the whole Graph.
8. **Skip trash** — As a person, I want search to skip trash, so that an ordinary search stays out of deleted Nodes.
9. **Start at the trash node** — As a person, I want search to include trash when it starts at the trash node TRASH, so that a search that starts there can see trash.
10. **Dialog shows server hits** — As a person, I want the Find dialog to keep the residency-search hits and append the server hits the client search did not find, after the client adds the carried nodes to the residency Graph, including a hit that was not resident before the reply, so that I see those server hits in the dialog.
11. **Move uses the same search** — As a person, I want Move to use this same search, so that Move shares the Want nodes ride, the cap of 200, the Node id list, and the trash rule.
12. **Resumed typing** — As a person, I want the next residency search to use nodes that an earlier server reply installed, so that a changed search text lists those nodes before any new server reply.
13. **Dialog cancels or replaces the search actor** — As a person, I want closing the Find dialog to cancel its pending server search, and a committed change of the search text to cancel the earlier search and run the new one, so that the dialog shows the new search.

### 4. Out of Scope

1. **Dialog layout** — Layout of the Find dialog is out of scope for this Search spec.
2. **Query spec** — The query expression is [§2 Query spec](#2-query-spec) in this file.
3. **File search** — File search is out of scope for this Search spec.
4. **Expression catalog** — Expression syntax and the catalog stay on [expression language](plan/expression-language/project.md).
5. **Search hydration** — Fetch before navigate when a Find hit is not Resident stays on [browser residency](plan/browser-residency/project.md). [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) still points that beat there.
6. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
7. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
8. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).
9. **Paging** — Paging, a continuation cursor, and a resume-when-the-dialog-asks bound are out of scope for this Search spec.

### 5. Further Notes

1. **Node id** — The server list lists each Node id once. [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) is not resolved by this spec.
2. **Cap of 200** — The residency search stops at 200 client hits. The full-database search stops at 200 server hits.
3. **Trash start** — The search starts in trash when it starts at the trash node TRASH.
4. **Move** — Move uses this same search. Find and Move share one server backend. File search is not.
5. **Query** — A query is one server evaluation when the line runs. It is not a message per key. See [§2 Query spec](#2-query-spec).
6. **Want nodes** — Find hit Headers ride the existing Want answer `nodes` list. This ride is a lock. See [map](map.md) Decisions so far item 11 **Want nodes**. The Query Ref post stays a proposed design in [arch](arch.md). Next and Page are not part of this spec.
7. **One spec file** — Both specs live in this file. See [map](map.md) Decisions so far item 1 **One spec file**.
8. **Research reports** — Reports linked from the research tickets are not accepted Answers.
9. **Map** — [map](map.md) Decisions so far holds the locks. This spec does not resolve a ticket.
10. **Vocabulary** — Say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.
11. **Root only** — Alan, 2026-11-13. Both searches start at the zoom root. The client search reads the residency from the zoom root. The server search reads the full database from the zoom root.
12. **Search Actor door** — Find and Move use `Api.SearchActorDoor`. The fields are `changes`, `recordStart`, and `recordStop`. `changes` reads State. `recordStart` places `Order.StartActor` for the actor `search`. `recordStop` returns unit. The stop row is one `Notice.ActorStopped` from `CoreActorPool.postStopUnlessCancelled`. The id is the root.
13. **Start fields** — The request carries `text`. It may carry `generation` for the reply match. It carries `startId`. `startId` is the zoom root. Both searches start at that zoom root. The dialog excludes Node ids the client search found when it appends the server hits.

## 2. Query spec

### 1. Problem Statement

1. **Run line** — A person writes an expression such as `= root descendants with name like "Bob"` and runs that line.
2. **Full database** — The server evaluates that expression against the full database.
3. **Existing nodes** — The result is existing nodes. The Focus node's children stay as they are.
4. **Text Answers** — A Text Answer stays an answer. A Text Answer is not an existing node.

### 2. Solution

1. **Server evaluates** — The server evaluates the expression once, when the line runs. The query has its own server support. A keypress does not send a query message.
2. **Node ids** — The actor stop is `ActorQuery`. That stop carries the Node ids of the existing nodes. This path posts no ops.
3. **Cap of 50** — The cap is `ExprRun.maxMaterialisedAnswers`. That number is 50. A query function may stop under 50. The server stops at 50. The query does not page. This cap is a lock.
4. **Find cap** — The Find word-search cap stays 200. That cap is §1 Search spec in this file.
5. **Trash** — Query skips trash, except the trash function. The trash function is named `trash`. It works like `root` for reaching trash.
6. **Separate from Find** — This spec is not the Find dialog. Find and Move are §1 Search spec in this file.
7. **Example is not a catalog row** — The expression `= root descendants with name like "Bob"` shows the kind of expression. It does not add words to the expression-language catalog. The word `trash` is the locked function name.
8. **Text Answer carrier** — What `ActorQuery` carries for a Text Answer is the open question on [25 — The server evaluates Run](issues/25-server-evaluates-run.md).
9. **Line and plan** — [22 — Run sends = lines to Amble](issues/22-run-sends-equals-lines-to-amble.md) chooses which line runs Amble. 25 — The server evaluates Run is where the plan runs.

### 3. User Stories

1. **Existing nodes** — As a person, I want the server evaluation of an expression such as `= root descendants with name like "Bob"` to return the existing nodes, so that the Focus node's children stay as they are.
2. **Cap of 50** — As a person, I want the server to stop at 50 answers, and to stop under 50 when the function stops there, so that the expression does not return the whole database and does not page.
3. **Skip trash** — As a person, I want an ordinary query to skip trash, so that deleted Nodes stay out of the results.
4. **Trash function** — As a person, I want the function `trash` to work like `root` for reaching trash, so that a query can start from trash the way `root` starts from ROOT.
5. **Insert Refs under the query line** — As a person, I want Refs under the query line, so that those Nodes appear under that line and the line does not own them. That insert is [16 — Insert Refs under the query line](issues/16-insert-refs-under-the-query-line.md).
6. **Server evaluates** — As a person, I want the server to evaluate the expression once when the line runs, so that the result is that evaluation and typing a key does not send a message.

### 4. Out of Scope

1. **Find dialog** — Find and Move are §1 Search spec in this file. Dialog UI is out of scope for this Query spec.
2. **File search** — File search is out of scope for this Query spec.
3. **Expression catalog** — Syntax and the catalog, other than the locked name `trash`, stay on [expression language](plan/expression-language/project.md).
4. **Search hydration** — Fetch before navigate when a hit is not Resident stays on [browser residency](plan/browser-residency/project.md).
5. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
6. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
7. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).
8. **Children** — This evaluation writes no children on the Focus node.
9. **Refs** — This evaluation generates no Refs. The insert of Refs stays the user story **Insert Refs under the query line**.
10. **Paging** — Paging and a continuation cursor are out of scope for this Query spec.

### 5. Further Notes

1. **Server eval** — The server evaluates the expression on the full database. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md) is not resolved by this spec. [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) stays that project's amendment.
2. **Cap of 50** — The server returns at most `ExprRun.maxMaterialisedAnswers` answers for this evaluation. That number is 50. A query function may stop under 50. This spec does not add expression spelling for that stop. The Find word-search cap stays 200.
3. **Trash function** — The name is `trash`. It works like `root` for reaching trash.
4. **Existing nodes** — `ActorQuery` carries the Node ids of existing nodes. The Focus node's children stay as they are. This path posts no ops.
5. **Insert** — The Query Ref post stays a proposed design in [arch](arch.md) for the insert story **Insert Refs under the query line**. This evaluation writes no children.
6. **One spec file** — Both specs live in this file. See [map](map.md) Decisions so far item 1 **One spec file**.
7. **Research reports** — Reports linked from the research tickets are not accepted Answers.
8. **Map** — [map](map.md) Decisions so far holds the locks. This spec does not resolve a ticket.
9. **Query name** — In this project, Query means the expression the server evaluates on the full database. This evaluation returns existing Node ids.
10. **Vocabulary** — Say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.
