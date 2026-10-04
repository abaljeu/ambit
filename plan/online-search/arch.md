# Online search architecture

Spec: [spec](spec.md)
Updated: 2026-10-04
Sequence: unsettled

The Search spec and the Query spec stay separate. Dialog UI stays out of scope. Story paths below record locked outcomes only. They name an existing module only where a lock already keeps that module. They do not name a new Server module. [map](map.md) Decisions so far is empty. Open questions stay in §5 Unsettled.

No committed module element is in §2 Module map. This architecture adds no claim under [doc/current](doc/current/).

## 1. Story paths

1. **Residence hits first**
   1. [ ] **Find dialog** — [Search dialog](src/Client/SearchDialog.fs) shows the hits the Browser already has, from the existing incremental client search.

2. **Server completes the picture**
   1. [ ] **Server completion** — After those hits, the server completes the picture. The module is unsettled. See §5 item 1 **Server module for Find**.

3. **Duplicates removed**
   1. [ ] **Second phase drops duplicates** — The server phase leaves out a hit the client already showed. The identity is unsettled. See §5 item 4 **Duplicate identity**.

4. **Screen plus a page**
   1. [ ] **Bound** — Find stays bounded to a screen plus about a page. A short keystroke such as the letter e does not return millions of Nodes. The cursor terms are unsettled. See §5 item 10 **Page bound in cursor terms**.

5. **Resume when the dialog asks**
   1. [ ] **Dialog asks** — More hits arrive only when the dialog asks for more. How the server phase continues that search is unsettled. See §5 item 3 **Server phase cursor**.

6. **Skip trash**
   1. [ ] **Ordinary Find skips trash** — Find skips trash.

7. **Start in trash**
   1. [ ] **Trash when the search starts there** — Find includes trash when the search starts in trash. Which Node that start is is unsettled. See §5 item 8 **Search starts in trash**.

8. **Dialog shows server hits**
   1. [ ] **Dialog shows N** — If the server finds N items, the Find dialog shows them. How those items ride Want is unsettled. See §5 item 2 **Want package for hits**.

9. **Insert under the query line**
   1. [ ] **Query line** — An expression such as `= root descendants with name like "Bob"` inserts results under the query line. The module is unsettled. See §5 item 5 **Query server fulfillment**.

10. **Function limit**
    1. [ ] **Function's own limit** — The function defines its own limit, so the query does not return millions of Nodes. How the spec states that limit is unsettled. See §5 item 6 **Function limit**.

11. **Query skips trash**
    1. [ ] **Ordinary query skips trash** — An ordinary query skips trash.

12. **Trash function**
    1. [ ] **Trash works like root** — A trash function works like root. The spelling is unsettled. See §5 item 7 **Trash function**.

13. **Server items inserted**
    1. [ ] **Insert N** — The query expression inserts the N items the server finds under the query line. What the server fulfills is unsettled. See §5 item 5 **Query server fulfillment**.

14. **Eval stays local**
    1. [ ] **Local eval** — Expression eval stays on the client Graph. Lock: [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md).

### 15. Shared segments

1. **Server N reaches the person**
   1. [ ] **Server-side support** — The related command has server-side support. Which commands besides Find and the query expression are related is unsettled. See §5 item 9 **Move and file search**.
   2. [ ] **Person sees N** — Find shows N in the dialog. The query expression inserts N under the query line. Both are Want-fulfillment. The package is unsettled. See §5 item 2 **Want package for hits**.

### 16. Test seam

1. **No committed test seam** — No interface is committed as the narrowest point every path crosses. The question is §5 item 12 **Test seam**.

## 2. Module map

1. **No committed module delta**
   The locks name Want-fulfillment and local eval. They name no new module and no change to an existing module's Interface. There is no search actor under `src/Server` today.
   File: none

   1. **State**
      1. **None** — No State is committed for this project.
   2. **Interface**
      1. **None** — No Interface is committed for this project.
   3. **Uses**
      1. **None** — No Uses list is committed for this project.

## 3. Seams

1. **No committed seam**
   1. **None** — No new seam is committed. Dialog UI stays out of scope. A Server door, a Want field for hits, and a duplicate key stay in §5 Unsettled.

## 4. Alternative considered

1. **No arrangement chosen** — The story paths are locked outcomes. They are not a module arrangement. This section does not compare a second design.

## 5. Unsettled

1. **Server module for Find** — Which module fulfills Find on the Server? The locks do not name one. There is no search actor under `src/Server` today.
2. **Want package for hits** — How a found-Node list rides today's Want. [map](map.md) item 3 **Found Nodes on a Want**. This waits on [02 — Want package of Nodes](issues/02-want-package-of-nodes.md). Existing Want doors include [Want compose](src/Shared/Want.fs), [wantAnswer](src/Shared/ResidentProjection.fs), [installWantAnswer](src/Shared/ResidentProjection.fs), and [postPoll](src/Server/Api.fs). This architecture does not assign Find or Query to those doors.
3. **Server phase cursor** — How the server phase continues the incremental search the dialog already resumes. [map](map.md) item 2 **Server phase and the same cursor**. This waits on [01 — Incremental search fill and resume](issues/01-incremental-search-fill-and-resume.md).
4. **Duplicate identity** — What makes a server hit the same hit the client already showed? [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md).
5. **Query server fulfillment** — What does the server fulfill for a query while eval stays on the client Graph, and which module inserts those Nodes under the query line? [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md).
6. **Function limit** — How a query function defines its own limit. [map](map.md) item 4 **Function limit**.
7. **Trash function** — The spelling of the trash function that works like root. [map](map.md) item 5 **Trash function**.
8. **Search starts in trash** — Which Node means the search starts in trash?
9. **Move and file search** — Whether Move or file search is a related command these specs cover. [map](map.md) item 6 **Move and file search**.
10. **Page bound in cursor terms** — How the Search spec states "a screen plus about a page" in the terms of the existing incremental cursor. [map](map.md) item 1 **Page bound in cursor terms**.
11. **Result placement** — Whether results under the query line are Owned Children, Refs, or another placement.
12. **Test seam** — Which interface every committed path crosses, so tests have one seam.
13. **Sequence** — Whether the implementation sequence is tracer-cut, module-build, or expand-contract. The story paths do not settle this.
14. **Spec file names** — The file names of the Search spec and the Query spec. [map](map.md) item 7 **Spec file names**. Both specs stay in [spec](spec.md) until that item is named.
