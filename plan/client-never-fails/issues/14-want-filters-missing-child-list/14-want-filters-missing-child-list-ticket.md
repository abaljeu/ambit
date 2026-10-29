# 14 — Want filters a missing child list
**Status**: 
One job: Poll's Want keeps an id whose child list is absent. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [13 — Delete the childrenStatus wire](plan/client-never-fails/issues/13-delete-children-status-wire/13-delete-children-status-wire-ticket.md)

## Inputs

### Working

[ ] `Want.compose` in `src/Shared/Want.fs` filters with `not (GraphChildren.isLoaded graph id)`.
[ ] `GraphChildren.isLoaded` is true when `childMap` contains the id.
[ ] `IncludedDescendantIds.throughChildrenOfExpandedNodes` builds the walk. `plusChildrenOfEach` runs twice.
[ ] An absent child-list key and `not isLoaded` name the same ids today.
[ ] ``compose lists Included that miss Children first`` in [[tests/Shared.Tests/WantTests.fs]] lists an id after `unload`.
[ ] `GraphChildren.tryGet` returns `None` for an absent child list.
[ ] [[src/Shared/Want.fs]] — `compose`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/14-want-filters-missing-child-list-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the Header-absent filter. That filter is 26 — Poll for nearby Absent Nodes.
[ ] Closed: the four older compose facts. They stay until that Poll ticket.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `Want.compose` so the filter is `GraphChildren.tryGet graph id = None`. Delete the `GraphChildren.isLoaded` call.
3. [ ] Add the fact named in Outputs.

## Outputs

[ ] `src/Shared/Want.fs`
[ ] `tests/Shared.Tests/WantTests.fs` — add ``compose keeps an id whose child list is absent``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/14-want-filters-missing-child-list-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``compose keeps an id whose child list is absent``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/14-want-filters-missing-child-list-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
