# 09 — Parent change checks Header scope

Slice: History.isProvenMissingOwner treats a claimed owner as present when that owner's Header is present. The isLoaded exemption goes.

## Signatures

[ ] `History.isProvenMissingOwner` in `src/Shared/History.fs` returns false when `GraphChildren.isLoaded` is false on the claimed owner.

## Match sites

[ ] The function is inside `validateOwnershipSemantics`. A missing Header on the claimed owner already returns false.
[ ] A root owner stays exempt in the branch `childNode.owner = graph.root`.

## Existing tests

[ ] ``validateOwnershipLocated Ok when Ref owner parent is Unloaded`` in [[tests/Shared.Tests/HistoryTests.fs]] expects Ok while the owner child list is absent.
[ ] ``validateOwnershipLocated Ok when Ref owner defaulted to ROOT with Unloaded real owner`` in `tests/Shared.Tests/HistoryTests.fs` covers the root default.
[ ] ``validateOwnershipLocated Error when Ref owner parent is Loaded without Owner`` in `tests/Shared.Tests/HistoryTests.fs` expects `Error` of a string when the owner Header is present and the Owner edge is missing.

## Copy-from

[ ] The Header lookup `Map.tryFind childNode.owner graph.nodes` is already the first check in that match.

## write-ticket

### Source files

[ ] [[src/Shared/History.fs]] — `isProvenMissingOwner`.

### Tests

[ ] Change ``validateOwnershipLocated Ok when Ref owner parent is Unloaded`` so a present owner Header is checked as a present parent.
[ ] Change ``validateOwnershipLocated Error when Ref owner parent is Loaded without Owner`` so the assertion uses the Header and the missing Owner edge.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 2. Under the limit of 4.
