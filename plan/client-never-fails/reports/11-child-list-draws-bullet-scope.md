# 11 — Child list draws the Bullet scope

Slice: rowChildrenIndicator already matches on GraphChildren.tryGet. This ticket names that rule in the three facts.

## Signatures

[ ] `ViewModelChildrenIndicator.rowChildrenIndicator: Graph -> Node -> RowChildrenIndicator` in `src/Shared/ViewModelChildrenIndicator.fs`.
[ ] Unparsed returns `SlashedHollowCircle`. `None` returns `HollowCircle`. `Some []` returns `SolidCircle`. `Some (_ :: _)` returns `FoldChevron`.

## Match sites

[ ] The function uses `GraphChildren.tryGet` for the child list.

## Existing tests

[ ] ``rowChildrenIndicator is HollowCircle when children are Unloaded`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] uses `unload` and expects `HollowCircle`.
[ ] ``rowChildrenIndicator is SolidCircle for Loaded Parsed empty children`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `SolidCircle` for an empty list.
[ ] ``rowChildrenIndicator is FoldChevron when Loaded with children`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `FoldChevron` when the list has ids.
[ ] ``rowChildrenIndicator is SlashedHollowCircle when document is Unparsed leaf`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects the slash for Unparsed.

## Copy-from

[ ] `GraphChildren.tryGet` is the match this function already uses.

## write-ticket

### Source files

[ ] [[src/Shared/ViewModelChildrenIndicator.fs]] — keep the child-list match. Unparsed stays slashed.

### Tests

[ ] Change the hollow, solid, and chevron facts so their names and assertions speak the child list: absent list, empty list, and list with ids.

## Size

[ ] Source files: 1, and only if the match is other than the child list. Otherwise 0.
[ ] Tests added or changed: 3. Under the limit of 4.
