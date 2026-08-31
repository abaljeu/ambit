# Find pick correction — implement

Updated: 2026-08-17

## Rule

Restore pre-`focusNode` Find zoom (hit if it has children, else structural parent); on that no-children parent fallback only, select the search target via `childSelectionAt`.

## Change

`ViewModelSearch.searchPickSetRoot` no longer calls `ViewModel.focusNode`. Framing matches prior zoom-in-style Find pick; leaf fallback uses `childSelectionAt` at the hit’s parent index instead of `firstChildSelection`.

## Acceptance

| AC | Status |
| --- | --- |
| 1 Has-children → zoom target, first-child selection | done |
| 2 No-children → zoom parent, selection = target | done |
| 3 Non-first leaf sibling selected correctly | done |
| 4 Shared non-leaf → zoom shared + owner ingress | done |
| 5 Zoom commands unchanged | done (Find path only) |
| 6 Tests updated; focusNode Find expectations dropped | done |

## Files

- `plan/search-zoom-select/spec.md`
- `src/Shared/ViewModelSearch.fs` — `searchPickSetRoot`
- `tests/Shared.Tests/ViewModelTests.fs` — searchPickSetRoot cases
- `plan/search-zoom-select/project.md`, `git.md`

## Tests

```text
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ViewModelTests.searchPickSetRoot"
# Passed: 5
```

- `searchPickSetRoot leaf fallback zooms parent and selects target`
- `searchPickSetRoot leaf fallback selects non-first sibling not first child`
- `searchPickSetRoot with children zooms target and selects first child`
- `searchPickSetRoot reframes outside prior zoom when hit is not under zoom root`
- `searchPickSetRoot seeds owner ingress for shared zoom-out`
