# Online search

Labels: wayfinder:map

## 1. Destination

Search spec: the Find dialog opens on a local search and shows N hits. When N is under 200, the globe on the search bar can request the server. When the search text is unchanged, the server reply replaces the client list. An edit runs a local search or a server search according to the globe. The cap is 200. There is no paging. Search skips trash unless the search starts in trash.

Query spec: an expression such as `= root descendants with name like "Bob"` inserts results under the query line. The Query Actor evaluates on the server. A query function may stop under 200. The server stops at 200. Trash is skipped except the function `trash`, which works like `root`.

## 2. Notes

1. **Skills** — [[.agents/skills/wayfinder/SKILL.md]] and [[.agents/skills/research/SKILL.md]] for this map. A later session on a grilling ticket uses [[.agents/skills/grilling/SKILL.md]] and [[.agents/skills/domain-modeling/SKILL.md]]. This chart skipped those two skills. Alan said that grilling is already done.
2. **Two specs** — This project owns the Search spec and the Query spec. They stay separate.
3. **Ownership** — [Browser residency](plan/browser-residency/project.md) postpones Server Find and must not own this. Neighbors that must not own it: [search zoom select](plan/search-zoom-select/project.md), [expression language](plan/expression-language/project.md), and [selective client loading](plan/selective-client-loading/project.md).
4. **Residence-only Find** — Current Find is residence-only (`searchNodes` on the browser Graph; `searchPickSetRoot` has no effect). There is no search actor under `src/Server` today. [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) is postponed with Status `needs-info`.
5. **Remote query eval** — This project locks Query Actor eval on the server. [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) stays that project's text. This map does not edit it.
6. **Want-fulfillment** — A Find server reply includes the found Nodes on the existing Want answer `nodes` list. See Decisions so far item 11 **Want nodes**. A query expression inserts Refs. The Query Ref post stays a proposed design.
7. **Bound** — A short keystroke such as the letter e must not return millions of Nodes.
8. **Dialog UI** — Layout of the Find dialog is not in this map. The globe on the search bar is Decisions so far item 10 **Globe**.
9. **Query name** — In this map, Query means the expression that inserts results under the query line.
10. **Charting** — This session charts only. Alan later accepted the Answers on [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) and [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). Those gists are Decisions so far items 5 **Node id** and 6 **Remote query eval**.
11. **Research reports** — Findings for the three research tickets are linked from those tickets. This chart did not accept those Answers.

## 3. Decisions so far

1. **One spec file** — Alan, 2026-10-05. Both specs live in [spec](spec.md). The Search spec and the Query spec stay separate sections of that file.
2. **No limit syntax** — Alan, 2026-10-05. A query function may stop under 200. The server stops at 200. This project does not add expression spelling for that stop.
3. **Actor file** — Alan, 2026-10-05. `src/Server/SearchActor.fs` is the home of the Search Actor and the Query Actor.
4. **Sequence** — Alan, 2026-10-05. Sequence is tracer-cut. See [arch](arch.md).
5. **Node id** — Alan, 2026-10-05. The identity of a result is the NodeId. [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md). The server list lists each Node id once. See item 10 **Globe**.
6. **Remote query eval** — Alan, 2026-10-05. Query eval is remote. The Query Actor evaluates on the server. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) is not edited.
7. **Standing locks** — The cap of 200, the globe on the search bar, the shared Find and Move Actor, no paging, the trash rules, and Want nodes for hits stay locks. The short quiet gap is not a Find lock. The Query Ref post stays a proposed design.
8. **Shared search algorithm** — Alan, 2026-10-05. One search algorithm serves Find and Move. Step 1: the client algorithm stops at 200 hits. That work is [14 — Cap of 200](issues/14-cap-of-200.md) section 1 **Find and Move**. Step 2: the Search Actor reuses that algorithm. Today [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) take the search text, the zoom, and the Graph. The reply holds up to 200 hits. One reply and no continuation cursor stay a seam. The Find start is item 10 **Globe**. The query cap stays on [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap** and is blocked by [11 — Server evaluates](issues/11-server-evaluates.md) only.
9. **Root only** — Alan, 2026-10-07. Search does not take a focus. The server walk uses the root as the zoom. Actor Start supplies the root and the full server Graph. `zoomId`, `commandId`, and `graphIds` are the root. The shared `focusId` field holds that same root so ActorStop can pair with the start.
10. **Globe** — Alan, 2026-10-07. The Find dialog opens on a local search and shows N hits. When N is under 200, the globe on the search bar can request the server. When the search text is unchanged, the server reply replaces the client list. When the person edits the text, the search is local or server according to whether the globe is selected. This supersedes the quiet-gap Start. [08 — Duplicates on Node id](issues/08-duplicates-on-node-id.md) is superseded by [21 — Globe requests the server](issues/21-globe-requests-the-server.md). The server list lists each Node id once. The start does not carry shown Node ids.
11. **Want nodes** — Alan, 2026-10-07. A server reply with Find results includes the found Nodes as Want-fulfillment. Those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. There is no new package. Move uses that same reply. The Query Ref post stays a proposed design. [02 — Want package of Nodes](issues/02-want-package-of-nodes.md) stays in its current shape.

## 4. Not yet specified

None.

## 5. Out of scope

1. **Dialog layout** — Layout of the Find dialog is out of scope for this map. The globe on the search bar is in scope. See Decisions so far item 10 **Globe**.
2. **Implementation** — Coding the server search and the client phases is out of scope for this map. The map ends at the two specs.
3. **Server module map** — A search Actor and its module cut are out of scope for this map. That cut is architecture after the specs.
4. **Search hydration** — Fetch before navigate when a Find hit is not Resident is out of scope for this map. [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) still points that beat at [browser residency](plan/browser-residency/project.md).
5. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map. Resolution of that ticket stays there.
6. **Expression language** — Syntax and the catalog stay on [expression language](plan/expression-language/project.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) stays that project's text. This project does not edit it. Query eval for this project is remote. See Decisions so far item 6 **Remote query eval**.
7. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md). That project is done.
8. **Selective client loading** — The prior whole-Workspace slice stays on [selective client loading](plan/selective-client-loading/project.md). That project is done.
