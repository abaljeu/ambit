# Online search

Labels: wayfinder:map

## 1. Destination

Search spec: two-phase Want-fulfillment of the existing incremental client search (the client shows what it already has, then the server completes the picture, duplicates removed), bounded to a screen plus about a page and resumed only when the dialog asks for more, skipping trash unless the search starts in trash.

Query spec: an expression such as `= root descendants with name like "Bob"` inserts results under the query line, the function defines its own limit, and trash is skipped except a trash function that works like root.

## 2. Notes

1. **Skills** — [[.agents/skills/wayfinder/SKILL.md]] and [[.agents/skills/research/SKILL.md]] for this map. A later session on a grilling ticket uses [[.agents/skills/grilling/SKILL.md]] and [[.agents/skills/domain-modeling/SKILL.md]]. This chart skipped those two skills. Alan said that grilling is already done.
2. **Two specs** — This project owns the Search spec and the Query spec. They stay separate.
3. **Ownership** — [Browser residency](plan/browser-residency/project.md) postpones Server Find and must not own this. Neighbors that must not own it: [search zoom select](plan/search-zoom-select/project.md), [expression language](plan/expression-language/project.md), and [selective client loading](plan/selective-client-loading/project.md).
4. **Residence-only Find** — Current Find is residence-only (`searchNodes` on the browser Graph; `searchPickSetRoot` has no effect). There is no search actor under `src/Server` today. [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) is postponed with Status `needs-info`.
5. **Eval stays local** — [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) was amended so eval stays local.
6. **Want-fulfillment** — Each related command gets server-side support. If the server finds N items, the Find dialog shows them. A query expression inserts them. That is Want-fulfillment.
7. **Bound** — A short keystroke such as the letter e must not return millions of Nodes.
8. **Dialog UI** — The UI of the dialog is not in this map.
9. **Query name** — In this map, Query means the expression that inserts results under the query line.
10. **Charting** — This session charts only. It does not resolve a decision ticket. Decisions so far stays empty until a later session accepts an Answer.

## 3. Decisions so far

<!-- the index — one numbered line per resolved ticket: enough to judge relevance, then zoom the link for the detail the ticket holds -->

## 4. Not yet specified

1. **Page bound in cursor terms** — How the Search spec states "a screen plus about a page" in the terms of the existing incremental cursor. This waits on [01 — Incremental search fill and resume](issues/01-incremental-search-fill-and-resume.md).
2. **Server phase and the same cursor** — How the server phase continues the incremental search the dialog already resumes. This waits on [01 — Incremental search fill and resume](issues/01-incremental-search-fill-and-resume.md).
3. **Found Nodes on a Want** — How a found-Node list rides today's Want package. This waits on [02 — Want package of Nodes](issues/02-want-package-of-nodes.md).
4. **Function limit** — How a query function defines its own limit in the Query spec.
5. **Trash function** — The spelling of the trash function that works like root, and how the Query spec states that function.
6. **Move and file search** — Whether Move or file search is a related command these two specs cover.
7. **Spec file names** — The file names of the Search spec and the Query spec in this project.

## 5. Out of scope

1. **Dialog UI** — Find dialog layout and controls are out of scope for this map.
2. **Implementation** — Coding the server search and the client phases is out of scope for this map. The map ends at the two specs.
3. **Server module map** — A search Actor and its module cut are out of scope for this map. That cut is architecture after the specs.
4. **Search hydration** — Fetch before navigate when a Find hit is not Resident is out of scope for this map. [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) still points that beat at [browser residency](plan/browser-residency/project.md).
5. **Browser-residency Server Find** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) stays postponed on that map. Resolution of that ticket stays there.
6. **Expression language** — Syntax, catalog, and eval stay on [expression language](plan/expression-language/project.md). Eval stays local on [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md).
7. **Search zoom select** — Zoom framing on a Find pick stays on [search zoom select](plan/search-zoom-select/project.md). That project is done.
8. **Selective client loading** — The prior whole-Workspace slice stays on [selective client loading](plan/selective-client-loading/project.md). That project is done.
