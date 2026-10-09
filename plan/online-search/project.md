# Online search

Stage: build
Summary: Two specs in one file. Find opens on a local list, and the globe requests the server when that list is under 200. A query expression inserts results under the query line. Query eval is remote.
Updated: 2026-10-09
Started: 2026-10-04
Actual: 10h

## Notes

- Map: [[map.md]]
- Specs: [[spec.md]] holds the Search spec and the Query spec. Both specs live in that file.
- Architecture: [[arch.md]]. Sequence is tracer-cut. The cap of 200, the globe on the search bar, Want nodes for hits, and `src/Server/SearchActor.fs` are locks. The quiet gap is not a Find lock. The Query Ref post stays a proposed design. Next and Page are not in this project.
- Frontier: [07 — Server completes the picture](issues/07-server-completes-the-picture.md) is coded. [21 — Globe requests the server](issues/21-globe-requests-the-server.md) is `defined` and blocked by that ticket. [11 — Server evaluates](issues/11-server-evaluates.md) is coded. Run on a `=` line queues ActorStart behind the edit and evals on the server Graph. The reply is Node ids on ActorStop. The Ref post stays [16 — Insert Refs under the query line](issues/16-insert-refs-under-the-query-line.md). [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap** is not started. [06 — Residence hits first](issues/06-residence-hits-first.md) is done. [08 — Duplicates on Node id](issues/08-duplicates-on-node-id.md) is cancelled.
- Coded pages: [Search](../../doc/current/search.md) and [Search Actor](../../doc/current/search-actor.md) mark the coded quiet-gap Start obsolete. The Find path on those pages is the globe. [Want nodes for hits](../../doc/current/want-nodes.md) is the Find reply ride.
- Committed claims: [Search Actor](../../doc/current/search-actor.md), [Query Actor](../../doc/current/query-actor.md), and [Want nodes for hits](../../doc/current/want-nodes.md). Those pages do not hold the Query Ref post. That post stays a proposed design.
- This project owns both specs. [Browser residency](plan/browser-residency/project.md), [search zoom select](plan/search-zoom-select/project.md), [expression language](plan/expression-language/project.md), and [selective client loading](plan/selective-client-loading/project.md) do not own them.

## Decisions so far

1. **One spec file** — Both specs live in [spec](spec.md). The Search spec and the Query spec stay separate sections of that file.
2. **No limit syntax** — A query function may stop under 200. The server stops at 200. This project does not add expression spelling for that stop.
3. **Actor file** — `src/Server/SearchActor.fs` is the home of the Search Actor and the Query Actor.
4. **Sequence** — tracer-cut.
5. **Node id** — The identity of a result is the NodeId. [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md). The server list lists each Node id once. See item 10 **Globe**.
6. **Remote query eval** — Query eval is remote. The Query Actor evaluates on the server. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) is not edited.
7. **Standing locks** — The cap of 200, the globe on the search bar, the shared Find and Move Actor, no paging, the trash rules, and Want nodes for hits stay locks. The short quiet gap is not a Find lock. The Query Ref post stays a proposed design.
8. **Shared search algorithm** — One search algorithm serves Find and Move. Step 1: the client algorithm stops at 200 hits. That work is [14 — Cap of 200](issues/14-cap-of-200.md) section 1 **Find and Move**. Step 2: the Search Actor reuses that algorithm. Today [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) take the search text, the zoom, and the Graph. The reply holds up to 200 hits. One reply and no continuation cursor stay a seam. The Find start is item 10 **Globe**. The query cap stays on [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap** and is blocked by [11 — Server evaluates](issues/11-server-evaluates.md) only.
9. **Root only** — Search does not take a focus. The server walk uses the root as the zoom. Actor Start supplies the root and the full server Graph. `zoomId`, `commandId`, and `graphIds` are the root. The shared `focusId` field holds that same root so ActorStop can pair with the start.
10. **Globe** — Alan, 2026-10-07. The Find dialog opens on a local search and shows N hits. When N is under 200, the globe on the search bar can request the server. When the search text is unchanged, the server reply replaces the client list. When the person edits the text, the search is local or server according to whether the globe is selected. This supersedes the quiet-gap Start. [08 — Duplicates on Node id](issues/08-duplicates-on-node-id.md) is superseded by [21 — Globe requests the server](issues/21-globe-requests-the-server.md). The server list lists each Node id once. The start does not carry shown Node ids.
11. **Want nodes** — Alan, 2026-10-07. A server reply with Find results includes the found Nodes as Want-fulfillment. Those hit Headers ride in the existing Want answer `nodes` list. [installWantAnswer](src/Shared/ResidentProjection.fs) merges that list. There is no new package. Move uses that same reply. The Query Ref post stays a proposed design. Claim home: [Want nodes for hits](../../doc/current/want-nodes.md).
