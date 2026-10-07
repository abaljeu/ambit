# Online search

Labels: wayfinder:map

## 1. Destination

Search spec: two-phase Want-fulfillment of the existing incremental client search (the client shows what it already has, then the server completes the picture, duplicates removed on Node id), capped at 200, with no paging, skipping trash unless the search starts in trash.

Query spec: an expression such as `= root descendants with name like "Bob"` inserts results under the query line. The Query Actor evaluates on the server. A query function may stop under 200. The server stops at 200. Trash is skipped except the function `trash`, which works like `root`.

## 2. Notes

1. **Skills** — [[.agents/skills/wayfinder/SKILL.md]] and [[.agents/skills/research/SKILL.md]] for this map. A later session on a grilling ticket uses [[.agents/skills/grilling/SKILL.md]] and [[.agents/skills/domain-modeling/SKILL.md]]. This chart skipped those two skills. Alan said that grilling is already done.
2. **Two specs** — This project owns the Search spec and the Query spec. They stay separate.
3. **Ownership** — [Browser residency](plan/browser-residency/project.md) postpones Server Find and must not own this. Neighbors that must not own it: [search zoom select](plan/search-zoom-select/project.md), [expression language](plan/expression-language/project.md), and [selective client loading](plan/selective-client-loading/project.md).
4. **Residence-only Find** — Current Find is residence-only (`searchNodes` on the browser Graph; `searchPickSetRoot` has no effect). There is no search actor under `src/Server` today. [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) is postponed with Status `needs-info`.
5. **Remote query eval** — This project locks Query Actor eval on the server. [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) stays that project's text. This map does not edit it.
6. **Want-fulfillment** — Each related command gets server-side support. If the server finds N items, the Find dialog shows them. A query expression inserts them. That is Want-fulfillment.
7. **Bound** — A short keystroke such as the letter e must not return millions of Nodes.
8. **Dialog UI** — The UI of the dialog is not in this map.
9. **Query name** — In this map, Query means the expression that inserts results under the query line.
10. **Charting** — This session charts only. Alan later accepted the Answers on [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) and [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). Those gists are Decisions so far items 5 **Node id** and 6 **Remote query eval**.
11. **Research reports** — Findings for the three research tickets are linked from those tickets. This chart did not accept those Answers.

## 3. Decisions so far

1. **One spec file** — Alan, 2026-10-05. Both specs live in [spec](spec.md). The Search spec and the Query spec stay separate sections of that file.
2. **No limit syntax** — Alan, 2026-10-05. A query function may stop under 200. The server stops at 200. This project does not add expression spelling for that stop.
3. **Actor file** — Alan, 2026-10-05. `src/Server/SearchActor.fs` is the home of the Search Actor and the Query Actor.
4. **Sequence** — Alan, 2026-10-05. Sequence is tracer-cut. See [arch](arch.md).
5. **Node id** — Alan, 2026-10-05. The identity of a result is the NodeId. A client hit and a server hit are the same hit when they share a NodeId. [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md).
6. **Remote query eval** — Alan, 2026-10-05. Query eval is remote. The Query Actor evaluates on the server. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) is not edited.
7. **Standing locks** — The cap of 200, the short quiet gap, the shared Find and Move Actor, no paging, and the trash rules stay locks. Want nodes for hits and the Ref post stay proposed designs.
8. **Shared search algorithm** — Alan, 2026-10-05. One search algorithm serves Find and Move. Step 1: the client algorithm stops at 200 hits. That work is [14 — Cap of 200](issues/14-cap-of-200.md) section 1 **Find and Move**. Step 2: the Search Actor reuses that algorithm. Today [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) take the search text, the zoom, and the Graph. [07 — Server completes the picture](issues/07-server-completes-the-picture.md) is blocked by that client stop. The reply holds up to 200 hits. One reply and no continuation cursor stay a seam. The query cap stays on [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap** and is blocked by [11 — Server evaluates](issues/11-server-evaluates.md) only.
9. **Root only** — Alan, 2026-10-07. Search does not take a focus. The server walk uses the root as the zoom. Actor Start supplies the root and the full server Graph. `zoomId`, `commandId`, and `graphIds` are the root. The shared `focusId` field holds that same root so ActorStop can pair with the start.

## 4. Not yet specified

1. **Found Nodes on a Want** — How a found-Node list rides today's Want package stays a proposed design in [arch](arch.md) §2 item 3 **Want nodes for hits**. This waits on [02 — Want package of Nodes](issues/02-want-package-of-nodes.md). This map does not accept that ticket's Answer.

## 5. Out of scope

1. **Dialog UI** — Find dialog layout and controls are out of scope for this map.
2. **Implementation** — Coding the server search and the client phases is out of scope for this map. The map ends at the two specs.
3. **Server module map** — A search Actor and its module cut are out of scope for this map. That cut is architecture after the specs.
4. **Search hydration** — Fetch before navigate when a Find hit is not Resident is out of scope for this map. [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) still points that beat at [browser residency](plan/browser-residency/project.md).
5. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map. Resolution of that ticket stays there.
6. **Expression language** — Syntax and the catalog stay on [expression language](plan/expression-language/project.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) stays that project's text. This project does not edit it. Query eval for this project is remote. See Decisions so far item 6 **Remote query eval**.
7. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md). That project is done.
8. **Selective client loading** — The prior whole-Workspace slice stays on [selective client loading](plan/selective-client-loading/project.md). That project is done.
