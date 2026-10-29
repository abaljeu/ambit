# 06 — Send child list on want answer
**Status**: 
One job: The Browser receives each returned Node's Header and its child list of ids, and a child id with no Header stays in that list. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [05 — Fill a placeholder](plan/client-never-fails/issues/05-fill-placeholder/05-fill-placeholder-ticket.md)

## Inputs

### Working

[ ] `ResidentProjection.wantAnswer` and `ResidentProjection.visibleClosureWantAnswer` in `src/Shared/ResidentProjection.fs`.
[ ] `SyncResponse.childMap` is `Map<NodeId, ChildNode list>` in `src/Shared/ApiResponses.fs`.
[ ] `wantAnswer` builds edges with `GraphChildren.tryGet`. A parent with no child-list key is omitted.
[ ] A complete edge omits a parent when a child Header is missing. This ticket keeps that child id on the list.
[ ] ``wantAnswer omits an edge with an absent target Node`` in [[tests/Shared.Tests/WantTests.fs]] expects the edge to be omitted.
[ ] ``wantAnswer returns wanted edges and pointed-at Nodes`` in `tests/Shared.Tests/WantTests.fs` covers a complete edge.
[ ] ``visibleClosureWantAnswer installs through installWantAnswer`` in `tests/Shared.Tests/WantTests.fs` installs the closure answer.
[ ] `GraphChildren.tryGet` in `src/Shared/Model.fs` is the child-list read.
[ ] [[src/Shared/ResidentProjection.fs]] — `wantAnswer` and `visibleClosureWantAnswer`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/06-send-child-list-on-want-answer-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `src/Server/Api.fs` and `UniversalResponse`. Those are 07 — Send child list on command response.
[ ] Closed: the `ChildrenStatus` type.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `wantAnswer` so each returned Node includes its Header and its child list of ids, and a child id with no Header stays in the list.
3. [ ] Change `visibleClosureWantAnswer` the same way.
4. [ ] Change the omit fact and add the closure fact named in Outputs.

## Outputs

[ ] `src/Shared/ResidentProjection.fs`
[ ] `tests/Shared.Tests/WantTests.fs` — change ``wantAnswer omits an edge with an absent target Node``; add ``visibleClosureWantAnswer keeps a child id with no Header``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/06-send-child-list-on-want-answer-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``wantAnswer omits an edge with an absent target Node``
  - `tests/Shared.Tests`: ``visibleClosureWantAnswer keeps a child id with no Header``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/06-send-child-list-on-want-answer-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
