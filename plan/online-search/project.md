# Online search

Stage: slice
Summary: Two specs in one file: Search is two-phase Want-fulfillment of Find, capped at 200, and a query expression inserts results under the query line. Query eval is remote.
Updated: 2026-10-05
Started: 2026-10-04

## Notes

- Map: [[map.md]]
- Specs: [[spec.md]] holds the Search spec and the Query spec. Both specs live in that file.
- Architecture: [[arch.md]]. Sequence is tracer-cut. The cap of 200, the short quiet gap, and `src/Server/SearchActor.fs` are locks. The Want ride and the Ref post stay proposed designs. Next and Page are not in this project.
- Committed claims: [Search Actor](../../doc/current/search-actor.md) and [Query Actor](../../doc/current/query-actor.md). Those pages do not hold proposed designs.
- This project owns both specs. [Browser residency](plan/browser-residency/project.md), [search zoom select](plan/search-zoom-select/project.md), [expression language](plan/expression-language/project.md), and [selective client loading](plan/selective-client-loading/project.md) do not own them.

## Decisions so far

1. **One spec file** — Both specs live in [spec](spec.md). The Search spec and the Query spec stay separate sections of that file.
2. **No limit syntax** — A query function may stop under 200. The server stops at 200. This project does not add expression spelling for that stop.
3. **Actor file** — `src/Server/SearchActor.fs` is the home of the Search Actor and the Query Actor.
4. **Sequence** — tracer-cut.
5. **Node id** — A client hit and a server hit are the same hit when they share a Node id (`NodeId`). [04 — Duplicate hit identity](issues/04-duplicate-hit-identity.md) stays in its current shape.
6. **Remote query eval** — The Query Actor evaluates on the server. [05 — Query fulfillment while eval stays local](issues/05-query-fulfillment-while-eval-stays-local.md) stays in its current shape. [14 — Server-side search](plan/expression-language/issues/14-server-side-search.md) is not edited.
7. **Standing locks** — The cap of 200, the short quiet gap, the shared Find and Move Actor, no paging, and the trash rules stay locks. Want nodes for hits and the Ref post stay proposed designs.
