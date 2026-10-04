# Online search

Two specs. They stay separate. File names for a split stay open on [map](map.md) item 7 **Spec file names**. Until that item is named, both specs live in this file.

This file records the Destination, the Notes that still stand, and Alan's later locks. It does not resolve a map ticket. A mechanism Alan asked this project to design is marked **Proposed design** in [arch](arch.md). It is not a lock.

## 1. Search spec

### 1. Problem Statement

1. **Residence-only Find** — Find searches the Resident Graph. `searchNodes` walks that Graph. The person does not see hits that are only on the Server. Move uses that same residence list today.
2. **Short keystroke** — A short keystroke such as the letter e must not return the whole Graph.

### 2. Solution

1. **Two phases** — Find and Move are a two-phase fulfillment of the existing incremental client search. The client shows what it already has. Then the server completes the picture.
2. **Own server support** — Find has its own server support. Move has its own server support. File search is out of this spec.
3. **Want-fulfillment** — If the server finds N items, the Find dialog shows them. Move shows that same N. That showing is Want-fulfillment. The way a found-Node list rides Want is a proposed design in [arch](arch.md). It is not a lock.
4. **Duplicates** — A client hit and a server hit are the same when they share a Node id. The second phase drops duplicates on Node id.
5. **Screen plus a page** — The incremental page is a screen plus about a page. The search resumes only when the dialog asks for more. Continuation uses an Actor and a shared message system. That message system is a proposed design in [arch](arch.md). It is not a lock.
6. **Trash** — Search skips trash unless it starts at the trash node. The trash node is TRASH.
7. **Move** — Move is in this spec. It uses this same search. File search is not in this spec.

### 3. User Stories

1. **Residence hits first** — As a person, I want Find to show the hits the Browser already has, so that I see those hits before the server answers.
2. **Server completes the picture** — As a person, I want the server to complete the picture after those hits, so that Find continues past the Resident Graph.
3. **Duplicates on Node id** — As a person, I want the server phase to drop a hit that shares a Node id with a hit the client already showed, so that the dialog lists that Node once.
4. **Screen plus a page** — As a person, I want each incremental page to be a screen plus about a page, so that a short keystroke such as the letter e does not return the whole Graph.
5. **Resume when the dialog asks** — As a person, I want the next page only when the dialog asks for more, so that the search stays on that page size.
6. **Skip trash** — As a person, I want search to skip trash, so that an ordinary search stays out of deleted Nodes.
7. **Start at the trash node** — As a person, I want search to include trash when it starts at the trash node TRASH, so that a search that starts there can see trash.
8. **Dialog shows server hits** — As a person, I want the Find dialog to show the N items the server finds, so that the server answer is visible.
9. **Move uses the same search** — As a person, I want Move to use this same two-phase search, so that Move has its own server support and the same N hits.

### 4. Out of Scope

1. **Dialog UI** — Layout and controls of the Find dialog are out of scope for this Search spec.
2. **Query spec** — The query expression is [§2 Query spec](#2-query-spec) in this file.
3. **File search** — File search is out of scope for this Search spec.
4. **Expression catalog** — Expression syntax and the catalog stay on [expression language](plan/expression-language/project.md).
5. **Search hydration** — Fetch before navigate when a Find hit is not Resident stays on [browser residency](plan/browser-residency/project.md). [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) still points that beat there.
6. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
7. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
8. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).

### 5. Further Notes

1. **Duplicate identity** — Locked here as Node id. [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) is not resolved by this spec.
2. **Page bound** — The lock is a screen plus about a page, resumed only when the dialog asks for more. This spec does not add a new page count.
3. **Trash start** — The search starts in trash when it starts at the trash node TRASH.
4. **Move** — Move is in this spec. File search is not.
5. **Proposed mechanisms** — The message system and the Want ride are proposed designs in [arch](arch.md).
6. **Spec file names** — Open on [map](map.md) item 7 **Spec file names**.
7. **Research reports** — Reports linked from the research tickets are not accepted Answers.
8. **Map** — [map](map.md). Decisions so far stays empty. This spec does not resolve a ticket.
9. **Vocabulary** — Say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.

## 2. Query spec

### 1. Problem Statement

1. **Results under the query line** — A person writes an expression such as `= root descendants with name like "Bob"` and wants the results under that query line.
2. **Server eval** — The server evaluates the query. This spec drops the earlier assumption that eval stays on the client Graph.
3. **Refs** — Those results are Refs. They are not Owned Children.

### 2. Solution

1. **Insert under the query line** — The expression inserts its results under the query line as Refs.
2. **Server evaluates** — The server evaluates the query. The query has its own server support.
3. **Function limit** — The query function sets its own limit. A short keystroke must not return the whole Graph.
4. **Trash** — Query skips trash, except the trash function. The trash function is named `trash`. It works like `root` for reaching trash.
5. **Want-fulfillment** — If the server finds N items, the query expression inserts them. That insert is Want-fulfillment.
6. **Separate from Find** — This spec is not the Find dialog. Find and Move are [§1 Search spec](#1-search-spec).
7. **Example is not a catalog row** — The expression `= root descendants with name like "Bob"` shows the kind of expression. It does not add words to the expression-language catalog. The word `trash` is the locked function name.
8. **Insertion module** — [ExprRun](src/Shared/ExprRun.fs) already builds a Ref for a Node answer (`ChildNode.reference` on the path `run` uses to materialise). The server eval, and posting that Ref under the query line, are a proposed design in [arch](arch.md). They are not a second lock on top of Refs.

### 3. User Stories

1. **Insert Refs under the query line** — As a person, I want an expression such as `= root descendants with name like "Bob"` to insert Refs under the query line, so that those Nodes appear under that line and the line does not own them.
2. **Function limit** — As a person, I want the query function to set its own limit, so that the query does not return the whole Graph.
3. **Skip trash** — As a person, I want an ordinary query to skip trash, so that deleted Nodes stay out of the results.
4. **Trash function** — As a person, I want the function `trash` to work like `root` for reaching trash, so that a query can start from trash the way `root` starts from ROOT.
5. **Server items inserted** — As a person, I want the query expression to insert the N items the server finds, so that the server answer appears under the query line as Refs.
6. **Server evaluates** — As a person, I want the server to evaluate the query, so that the result is the server's evaluation.

### 4. Out of Scope

1. **Find dialog** — Find and Move are [§1 Search spec](#1-search-spec). Dialog UI is out of scope for this Query spec.
2. **File search** — File search is out of scope for this Query spec.
3. **Expression catalog** — Syntax and the catalog, other than the locked name `trash`, stay on [expression language](plan/expression-language/project.md).
4. **Search hydration** — Fetch before navigate when a hit is not Resident stays on [browser residency](plan/browser-residency/project.md).
5. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
6. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
7. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).
8. **Owned results** — Owned Children under the query line are out of scope for this Query spec. Results are Refs.

### 5. Further Notes

1. **Server eval** — This lock replaces the earlier local-eval assumption for this spec. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md) is not resolved by this spec. [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) stays that project's amendment.
2. **Function limit** — The function sets its own limit. This spec does not spell a new limit syntax.
3. **Trash function** — The name is `trash`. It works like `root` for reaching trash.
4. **Refs** — Results under the query line are Refs.
5. **Proposed mechanisms** — How the found Nodes ride Want, and how the Actor posts the Ref insert, are proposed designs in [arch](arch.md).
6. **Spec file names** — Open on [map](map.md) item 7 **Spec file names**.
7. **Research reports** — Reports linked from the research tickets are not accepted Answers.
8. **Map** — [map](map.md). Decisions so far stays empty. This spec does not resolve a ticket.
9. **Query name** — In this project, Query means the expression that inserts Refs under the query line.
10. **Vocabulary** — Say event source. Say Server git Actor for that git Actor. Do not say CAS. Do not say Peer.
