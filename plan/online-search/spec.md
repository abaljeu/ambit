# Online search

Two specs. They stay separate. File names for a split stay open on [map](map.md) item 7 **Spec file names**. Until that item is named, both specs live in this file.

This file synthesizes [map](map.md) Destination and Notes. It does not resolve a map ticket. A line that needs an open ticket stops at the lock and points at that ticket.

## 1. Search spec

### 1. Problem Statement

1. **Residence-only Find** — Find searches the Resident Graph. `searchNodes` walks that Graph. The person does not see hits that are only on the Server.
2. **Short keystroke** — A short keystroke such as the letter e must not return millions of Nodes.

### 2. Solution

1. **Two phases** — Find is a two-phase fulfillment of the existing incremental client search. The client shows what it already has. Then the server completes the picture.
2. **Want-fulfillment** — That completion is Want-fulfillment. If the server finds N items, the Find dialog shows them.
3. **Duplicates removed** — The second phase removes duplicates of hits the client already showed. The identity of a duplicate is open. See Further Notes item 1 **Duplicate identity**.
4. **Screen plus a page** — The result is bounded to a screen plus about a page. How that bound is stated in cursor terms is open. See Further Notes item 2 **Page bound in cursor terms**.
5. **Resume** — The search resumes only when the dialog asks for more. How the server phase continues that incremental search is open. See Further Notes item 3 **Server phase and the same cursor**.
6. **Trash** — Find skips trash unless the search starts in trash. What "starts in trash" points at is open. See Further Notes item 4 **Search starts in trash**.
7. **Find only** — This spec is Find. Whether Move or file search is a related command is open. See Further Notes item 5 **Move and file search**.

### 3. User Stories

1. **Residence hits first** — As a person, I want Find to show the hits the Browser already has, so that I see those hits before the server answers.
2. **Server completes the picture** — As a person, I want the server to complete the picture after those hits, so that Find continues past the Resident Graph.
3. **Duplicates removed** — As a person, I want the server phase to leave out a hit the client already showed, so that the dialog lists that hit once.
4. **Screen plus a page** — As a person, I want Find bounded to a screen plus about a page, so that a short keystroke such as the letter e does not return millions of Nodes.
5. **Resume when the dialog asks** — As a person, I want more hits only when the dialog asks for more, so that Find stays inside that bound.
6. **Skip trash** — As a person, I want Find to skip trash, so that an ordinary search stays out of deleted Nodes.
7. **Start in trash** — As a person, I want Find to include trash when the search starts in trash, so that a search that starts there can see trash.
8. **Dialog shows server hits** — As a person, I want the Find dialog to show the N items the server finds, so that the server answer is visible.

### 4. Out of Scope

1. **Dialog UI** — Layout and controls of the Find dialog are out of scope for this Search spec.
2. **Query spec** — The query expression is [§2 Query spec](#2-query-spec) in this file.
3. **Expression language** — Syntax, catalog, and eval stay on [expression language](plan/expression-language/project.md). Eval stays local on [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md).
4. **Search hydration** — Fetch before navigate when a Find hit is not Resident stays on [browser residency](plan/browser-residency/project.md). [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) still points that beat there.
5. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
6. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
7. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).
8. **Modules** — This spec does not name a module or a search Actor. There is no search actor under `src/Server` today. The module cut is architecture.

### 5. Further Notes

1. **Duplicate identity** — Open on [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md). This spec locks that duplicates are removed. It does not name the identity.
2. **Page bound in cursor terms** — Open on [map](map.md) item 1 **Page bound in cursor terms**. This waits on [01 — Incremental search fill and resume](issues/01-incremental-search-fill-and-resume.md). This spec keeps the words "a screen plus about a page".
3. **Server phase and the same cursor** — Open on [map](map.md) item 2 **Server phase and the same cursor**. This waits on [01 — Incremental search fill and resume](issues/01-incremental-search-fill-and-resume.md).
4. **Search starts in trash** — The destination locks the trash rule. It does not say which Node means the search starts in trash.
5. **Move and file search** — Open on [map](map.md) item 6 **Move and file search**. This spec covers Find until that item is decided.
6. **Found Nodes on a Want** — Open on [map](map.md) item 3 **Found Nodes on a Want**. This waits on [02 — Want package of Nodes](issues/02-want-package-of-nodes.md). This spec says Want-fulfillment. It does not name package fields.
7. **Spec file names** — Open on [map](map.md) item 7 **Spec file names**.
8. **Research reports** — Reports linked from the research tickets are not accepted Answers. This spec does not treat them as locks.
9. **Map** — [map](map.md). Decisions so far is empty.

## 2. Query spec

### 1. Problem Statement

1. **Results under the query line** — A person writes an expression such as `= root descendants with name like "Bob"` and wants the results under that query line.
2. **Eval stays local** — Expression eval stays on the client Graph. [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) records that amendment.
3. **Server items still appear** — If the server finds N items, the query expression inserts them. What the server fulfills while eval stays local is open. See Further Notes item 1 **Query fulfillment**.

### 2. Solution

1. **Insert under the query line** — The expression inserts its results under the query line.
2. **Separate from Find** — This spec is not the Find dialog. Find is [§1 Search spec](#1-search-spec).
3. **Function limit** — The function defines its own limit. How this spec states that limit is open. See Further Notes item 2 **Function limit**.
4. **Trash** — Trash is skipped, except a trash function that works like root. The spelling of that function is open. See Further Notes item 3 **Trash function**.
5. **Want-fulfillment** — The insert of the Nodes the server finds is Want-fulfillment. How a found-Node list rides today's Want package is open. See Further Notes item 4 **Found Nodes on a Want**.
6. **Example is not a catalog row** — The expression `= root descendants with name like "Bob"` shows the kind of expression. It does not add words to the expression-language catalog.

### 3. User Stories

1. **Insert under the query line** — As a person, I want an expression such as `= root descendants with name like "Bob"` to insert results under the query line, so that those results appear under that line.
2. **Function limit** — As a person, I want the function to define its own limit, so that the query does not return millions of Nodes.
3. **Skip trash** — As a person, I want an ordinary query to skip trash, so that deleted Nodes stay out of the results.
4. **Trash function** — As a person, I want a trash function that works like root, so that a query can use trash the way it uses root.
5. **Server items inserted** — As a person, I want the query expression to insert the N items the server finds, so that the server answer appears under the query line.
6. **Eval stays local** — As a person, I want expression eval to stay on the client Graph, so that this spec keeps the local-eval amendment.

### 4. Out of Scope

1. **Find dialog** — Find is [§1 Search spec](#1-search-spec). Dialog UI is out of scope for this Query spec.
2. **Expression language** — Syntax, catalog, and eval stay on [expression language](plan/expression-language/project.md). This spec does not move eval to the Server.
3. **Search hydration** — Fetch before navigate when a hit is not Resident stays on [browser residency](plan/browser-residency/project.md).
4. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map.
5. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md).
6. **Selective client loading** — That done project stays [selective client loading](plan/selective-client-loading/project.md).
7. **Placement kind** — This spec does not say whether results under the query line are Owned Children, Refs, or another placement.
8. **Modules** — This spec does not name the module that fulfills the query on the Server.

### 5. Further Notes

1. **Query fulfillment** — Open on [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). Eval stays local. The insert is Want-fulfillment. What the server fulfills is the open question.
2. **Function limit** — Open on [map](map.md) item 4 **Function limit**. This spec locks that the function defines its own limit.
3. **Trash function** — Open on [map](map.md) item 5 **Trash function**. This spec locks that the function works like root. It does not spell the function.
4. **Found Nodes on a Want** — Open on [map](map.md) item 3 **Found Nodes on a Want**. This waits on [02 — Want package of Nodes](issues/02-want-package-of-nodes.md).
5. **Move and file search** — Open on [map](map.md) item 6 **Move and file search**.
6. **Spec file names** — Open on [map](map.md) item 7 **Spec file names**.
7. **Research reports** — Reports linked from the research tickets are not accepted Answers. This spec does not treat them as locks.
8. **Map** — [map](map.md). Decisions so far is empty.
9. **Query name** — In this project, Query means the expression that inserts results under the query line.
