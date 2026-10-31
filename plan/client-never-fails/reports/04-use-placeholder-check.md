pass

## 04 — Use a placeholder

Branch `dev`. A missing Header leaves the mode unchanged. Expand still calls `expandEntry`. `Want.compose` stays as it is.

## Files

- `src/Shared/ViewModelRowState.fs`
- `src/Client/UpdateOps.fs`
- `tests/Shared.Tests/ViewModelRowStateTests.fs`
- `tests/Shared.Tests/WantTests.fs`
- `tests/Shared.Tests/ViewModelTests.fs`

## Facts

- ``isEntrySelected is true for a placeholder SiteEntry with no Header`` builds a child whose id is absent from `graph.nodes`, places that SiteEntry in a `singleSelection` range, and asserts `isEntrySelected` is true.
- ``expandEntry leaves a placeholder row expanded`` calls `expandEntry` on a collapsed row whose id has no child-list key and asserts that row's `expanded` is true.
- ``compose wants a placeholder id with no child list after parent expand`` expands that id, then asserts `Want.compose` lists it.
- ``startEditInstanceAtPos leaves Selecting when Header is absent`` uses a known non-root instance whose id has no Header and asserts the returned mode is Selecting.

## Commands

- `scripts/agent-test.sh` — Passed 235, Failed 0, in `tests/Shared.Tests`.
- `scripts/agent-test.sh --filter "FullyQualifiedName~isEntrySelected is true for a placeholder SiteEntry with no Header" shared` — Passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~expandEntry leaves a placeholder row expanded" shared` — Passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~compose wants a placeholder id with no child list after parent expand" shared` — Passed 1.
- `scripts/agent-test.sh --filter "FullyQualifiedName~startEditInstanceAtPos leaves Selecting when Header is absent" shared` — Passed 1.
