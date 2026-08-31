# Find pick: zoom to owner, select target — implement

Updated: 2026-08-17

## Change

`ViewModelSearch.searchPickSetRoot` now routes through `ViewModel.focusNode`: zoom root becomes the target's owner parent (or ref-parent fallback), selection is the picked target at that parent, mode `Selecting`. No leaf / has-Children Zoom-in split.

## Acceptance

| AC | Status |
| --- | --- |
| 1 Zoom root = owner parent (or tryFocusNodeOccurrence fallback) | done |
| 2 Focused selection = picked target | done |
| 3 Non-leaf targets not used as zoom root for Children alone | done |
| 4 Shared Owned+Ref → Owned parent, select target | done |
| 6 Zoom-in / Zoom-out / Zoom-owner unchanged | done (Find path only) |
| 7 Shared.Tests rewritten for owner-then-select | done |

## Files

- `src/Shared/ViewModelSearch.fs` — `searchPickSetRoot`
- `tests/Shared.Tests/ViewModelTests.fs` — searchPickSetRoot cases

## Tests

```text
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ViewModelTests.searchPickSetRoot|FullyQualifiedName~ViewModelTests.focusNode|FullyQualifiedName~ViewModelTests.tryReframeZoomAtOwnerParent|FullyQualifiedName~ViewModelTests.tryFocusNodeOccurrence"
# Passed: 9
```
