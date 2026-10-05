# Online search

Stage: build
Summary: Two specs in one file: Search is two-phase Want-fulfillment of Find, capped at 200, and a query expression inserts results under the query line. Query eval is remote.
Updated: 2026-10-05
Started: 2026-10-04
Actual: 1h

## Notes

- Map: [[map.md]]
- Specs: [[spec.md]] holds the Search spec and the Query spec. Both specs live in that file.
- Architecture: [[arch.md]]. Sequence is tracer-cut. The cap of 200, the short quiet gap, and `src/Server/SearchActor.fs` are locks. The Want ride and the Ref post stay proposed designs. Next and Page are not in this project.
- Frontier: [14 — Cap of 200](issues/14-cap-of-200.md) section 1 **Find and Move** is implemented. The client search stops at 200. Section 2 **Query cap** is not started. Next is [07 — Server completes the picture](issues/07-server-completes-the-picture.md) after review approves section 1. [06 — Residence hits first](issues/06-residence-hits-first.md) is done.
- Committed claims: [Search Actor](../../doc/current/search-actor.md) and [Query Actor](../../doc/current/query-actor.md). Those pages do not hold proposed designs.
- This project owns both specs. [Browser residency](plan/browser-residency/project.md), [search zoom select](plan/search-zoom-select/project.md), [expression language](plan/expression-language/project.md), and [selective client loading](plan/selective-client-loading/project.md) do not own them.

## Decisions so far

1. **One spec file** — Both specs live in [spec](spec.md). The Search spec and the Query spec stay separate sections of that file.
2. **No limit syntax** — A query function may stop under 200. The server stops at 200. This project does not add expression spelling for that stop.
3. **Actor file** — `src/Server/SearchActor.fs` is the home of the Search Actor and the Query Actor.
4. **Sequence** — tracer-cut.
5. **Node id** — The identity of a result is the NodeId. A client hit and a server hit are the same hit when they share a NodeId. [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md).
6. **Remote query eval** — Query eval is remote. The Query Actor evaluates on the server. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md). [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) is not edited.
7. **Standing locks** — The cap of 200, the short quiet gap, the shared Find and Move Actor, no paging, and the trash rules stay locks. Want nodes for hits and the Ref post stay proposed designs.
8. **Shared search algorithm** — One search algorithm serves Find and Move. Step 1: the client algorithm stops at 200 hits. That work is [14 — Cap of 200](issues/14-cap-of-200.md) section 1 **Find and Move**. Step 2: the Search Actor reuses that algorithm. Today [startSearch](src/Shared/ViewModelSearch.fs) and [takeResults](src/Shared/ViewModelSearch.fs) take the search text, the zoom, and the Graph. Actor Start supplies root, focus, and the full server Graph. The Search walk may ignore focus. [07 — Server completes the picture](issues/07-server-completes-the-picture.md) is blocked by that client stop. The reply holds up to 200 hits. One reply and no continuation cursor stay a seam. The query cap stays on [14 — Cap of 200](issues/14-cap-of-200.md) section 2 **Query cap** and is blocked by [11 — Server evaluates](issues/11-server-evaluates.md) only.
