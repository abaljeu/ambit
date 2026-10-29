# 09 — Parent change checks Header
**Status**: 
One job: A parent change checks the parent Header. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [08 — Apply Replace when parent Header is present](plan/client-never-fails/issues/08-apply-replace-when-parent-header-present/08-apply-replace-when-parent-header-present-ticket.md)

## Inputs

### Working

[ ] `History.isProvenMissingOwner` in `src/Shared/History.fs` returns false when `GraphChildren.isLoaded` is false on the claimed owner.
[ ] The function is inside `validateOwnershipSemantics`. A missing Header on the claimed owner already returns false.
[ ] A root owner stays exempt in the branch `childNode.owner = graph.root`.
[ ] ``validateOwnershipLocated Ok when Ref owner parent is Unloaded`` in [[tests/Shared.Tests/HistoryTests.fs]] expects Ok while the owner child list is absent.
[ ] ``validateOwnershipLocated Ok when Ref owner defaulted to ROOT with Unloaded real owner`` in `tests/Shared.Tests/HistoryTests.fs` covers the root default.
[ ] ``validateOwnershipLocated Error when Ref owner parent is Loaded without Owner`` in `tests/Shared.Tests/HistoryTests.fs` expects `Error` of a string when the owner Header is present and the Owner edge is missing.
[ ] The Header lookup `Map.tryFind childNode.owner graph.nodes` is already the first check in that match.
[ ] [[src/Shared/History.fs]] — `isProvenMissingOwner`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/09-parent-change-checks-header-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: ``validateOwnershipLocated Ok when Ref owner defaulted to ROOT with Unloaded real owner``. The root exemption stays.
[ ] Closed: undo and redo structure facts. Those are 23 — Retarget history structure tests.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `isProvenMissingOwner` so a present parent Header is the check. Drop the `GraphChildren.isLoaded` branch.
3. [ ] Keep the root-owner exemption.
4. [ ] Change the two facts named in Outputs.

## Outputs

[ ] `src/Shared/History.fs`
[ ] `tests/Shared.Tests/HistoryTests.fs` — change ``validateOwnershipLocated Ok when Ref owner parent is Unloaded`` and ``validateOwnershipLocated Error when Ref owner parent is Loaded without Owner``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/09-parent-change-checks-header-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``validateOwnershipLocated Ok when Ref owner parent is Unloaded``
  - `tests/Shared.Tests`: ``validateOwnershipLocated Error when Ref owner parent is Loaded without Owner``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/09-parent-change-checks-header-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
