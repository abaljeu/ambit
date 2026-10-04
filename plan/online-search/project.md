# Online search

Stage: arch
Summary: Two specs: Search is two-phase Want-fulfillment of Find, capped at 200, and a query expression inserts results under the query line.
Updated: 2026-10-04
Started: 2026-10-04

## Notes

- Map: [[map.md]]
- Specs: [[spec.md]] holds the Search spec and the Query spec. File names for a split stay open on the map.
- Architecture: [[arch.md]]. The cap of 200 is a lock. The Want ride and the Ref post stay proposed designs. Next and Page are not in this project.
- This project owns both specs. [Browser residency](plan/browser-residency/project.md), [search zoom select](plan/search-zoom-select/project.md), [expression language](plan/expression-language/project.md), and [selective client loading](plan/selective-client-loading/project.md) do not own them.
