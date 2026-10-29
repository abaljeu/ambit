# 15 — Load target omits the Workspace
**Status**: 
One job: Each Load target the Browser builds carries a target id and leaves `includeWorkspace` false. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [14 — Want filters a missing child list](plan/client-never-fails/issues/14-want-filters-missing-child-list/14-want-filters-missing-child-list-ticket.md)

## Inputs

### Working

[ ] `UpdateHelpers.loadTargetIntent` in `src/Client/UpdateHelpers.fs` sets `includeWorkspace` true when the target Header is present and `Graph.isLoaded` is false.
[ ] `LoadTarget` is `{ targetId; includeWorkspace }` in `src/Shared/ApiResponses.fs`.
[ ] `tryStartLoadFetch` maps selection ids through `loadTargetIntent`.
[ ] The field stays on the record until 30 — Delete includeWorkspace.
[ ] No current fact calls `loadTargetIntent`. `includeWorkspace = true` still appears in [[tests/Shared.Tests/LoadCaptureTests.fs]] as a request fixture.
[ ] `tryStartLoadFetch` in `src/Client/UpdateHelpers.fs` is the public caller.
[ ] [[src/Client/UpdateHelpers.fs]] — `loadTargetIntent`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/15-load-target-omits-workspace-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: deleting `includeWorkspace` from `LoadTarget`, encode, decode, and `wantAnswerForTargets`. Those edits are 30 — Delete includeWorkspace.
[ ] Closed: the fixtures that still pass `includeWorkspace = true`. Those fixtures move in 27 — Retarget Load capture Workspace flag through 29 — Retarget remaining Load Workspace flag.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `loadTargetIntent` so every target has `includeWorkspace` false. Delete the `Graph.isLoaded` call.
3. [ ] Add the fact named in Outputs.

## Outputs

[ ] `src/Client/UpdateHelpers.fs`
[ ] `tests/Server.Tests/LoadSaveCommandClientTests.fs` — add ``tryStartLoadFetch sets includeWorkspace false``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/15-load-target-omits-workspace-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``tryStartLoadFetch sets includeWorkspace false``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/15-load-target-omits-workspace-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
