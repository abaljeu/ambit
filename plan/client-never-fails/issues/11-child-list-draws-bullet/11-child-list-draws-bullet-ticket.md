# 11 — Child list draws the Bullet
**Status**: 
One job: The outliner sees a chevron when a child list has ids, a solid circle when that list is empty, and a slashed circle when the Node is Unparsed. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [10 — Workspace change checks Header](plan/client-never-fails/issues/10-workspace-change-checks-header/10-workspace-change-checks-header-ticket.md)

## Inputs

### Working

[ ] `ViewModelChildrenIndicator.rowChildrenIndicator: Graph -> Node -> RowChildrenIndicator` in `src/Shared/ViewModelChildrenIndicator.fs`.
[ ] Unparsed returns `SlashedHollowCircle`. `None` returns `HollowCircle`. `Some []` returns `SolidCircle`. `Some (_ :: _)` returns `FoldChevron`.
[ ] The function uses `GraphChildren.tryGet` for the child list.
[ ] ``rowChildrenIndicator is HollowCircle when children are Unloaded`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] uses `unload` and expects `HollowCircle`.
[ ] ``rowChildrenIndicator is SolidCircle for Loaded Parsed empty children`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `SolidCircle` for an empty list.
[ ] ``rowChildrenIndicator is FoldChevron when Loaded with children`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `FoldChevron` when the list has ids.
[ ] ``rowChildrenIndicator is SlashedHollowCircle when document is Unparsed leaf`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects the slash for Unparsed.
[ ] `GraphChildren.tryGet` is the match this function already uses.
[ ] [[src/Shared/ViewModelChildrenIndicator.fs]] — keep the child-list match. Unparsed stays slashed.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/11-child-list-draws-bullet-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the residency children line. That line is 12 — Drop the residency children line.
[ ] Closed: the slashed Unparsed fact. It already matches.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Keep `rowChildrenIndicator` on the child list: chevron when the list has ids, solid circle when it is empty, hollow circle when it is absent. Unparsed stays slashed.
3. [ ] Change the three facts named in Outputs.

## Outputs

[ ] `src/Shared/ViewModelChildrenIndicator.fs`
[ ] `tests/Shared.Tests/ViewModelRowStateTests.fs` — change the hollow, solid, and chevron facts

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/11-child-list-draws-bullet-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``rowChildrenIndicator is HollowCircle when children are Unloaded``
  - `tests/Shared.Tests`: ``rowChildrenIndicator is SolidCircle for Loaded Parsed empty children``
  - `tests/Shared.Tests`: ``rowChildrenIndicator is FoldChevron when Loaded with children``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/11-child-list-draws-bullet-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
