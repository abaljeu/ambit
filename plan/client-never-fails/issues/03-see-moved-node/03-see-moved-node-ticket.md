# 03 — See a moved Node
**Status**: coded
One job: The outliner sees a Node another tab moves into an open section as a placeholder Bullet in color #444444. The story See a moved Node in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: none

## Inputs

### Working

[x] ``ResidentProjection.installWantAnswer: Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>`` in `src/Shared/ResidentProjection.fs`.
[x] ``GraphMutate.replace`` returns `Result<Graph, string>` in `src/Shared/GraphMutate.fs`. Today a missing child id returns `Error` of a string `new child not found`.
[x] ``ViewModelDomPlan.planPatchDOM: VM -> VM -> Set<SiteId> -> RowMutation list`` in `src/Shared/ViewModelDomPlan.fs`.
[x] `RowView.Layout.buildRowElement` in `src/Client/RowView.fs` reads `model.graph.nodes.[nodeId]`.
[x] Bullet color today is `.amb-fold-toggle` in `src/Server/wwwroot/style.css` (`#000` and `#44f`).
[x] `installWantAnswer` returns `Error` of a string `dangling edge` when an edge target id is missing from `mergedNodes`.
[x] `GraphMutate.replace` returns `Error` of a string `new child not found` when `newChildren` names an id outside `graph.nodes`. The parent `isLoaded` gate stays for ticket 08 — Apply Replace when parent Header is present.
[x] `planPatchDOM` binds `newModel.graph.nodes.[entry.nodeId]` on the upsert path.
[x] `ViewModelSiteMap.walkReconciled` already makes a row from a child id with no Header lookup. This ticket leaves that file as it is.
[x] ``installWantAnswer refuses a dangling edge`` in [[tests/Shared.Tests/WantTests.fs]] expects `Error` of a string `dangling edge`.
[x] ``applySyncResponse refuses dangling Want edges`` in [[tests/Shared.Tests/SyncLogicTests.fs]] expects the same `Error` of a string.
[x] ``planPatchDOM expand inserts correct child count`` in [[tests/Shared.Tests/ViewModelTests.fs]] covers `CreateRow` when Headers are present.
[x] `GraphBuild.fromNodes` in `src/Shared/GraphBuild.fs` already keeps a child id when `nodes` has no Header for it.
[x] `ViewModelDomPlan.selectionClassPatches` already uses `Map.tryFind` and treats a missing Header as no patch.
[x] [[src/Shared/ResidentProjection.fs]] — `installWantAnswer` returns `Ok` when a child id has no Header.
[x] [[src/Shared/GraphMutate.fs]] — `replace` returns `Ok` for a new child id with no Header. The parent loaded gate stays.
[x] [[src/Shared/ViewModelDomPlan.fs]] — `planPatchDOM` uses `Map.tryFind`. A missing Header is a placeholder row.
[x] [[src/Shared/ViewModelChildrenIndicator.fs]] — an Absent Header has a Bullet path that can take the #444444 color.
[x] [[src/Client/RowView.fs]] — `buildRowElement` draws that row.
[x] [[src/Server/wwwroot/style.css]] — the placeholder Bullet uses color #444444.

### Reference

[x] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[x] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[x] Closed: `ChildrenStatus` removal, Want filters, and the parent `isLoaded` gate inside `replace`.
[x] Closed: `src/Shared/ViewModelSiteMap.fs`. Child rows already come from child ids.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `installWantAnswer` so a child id with no Header returns `Ok` and stays on the edge.
3. [x] Change `GraphMutate.replace` so a new child id with no Header returns `Ok`. Leave the parent `GraphChildren.isLoaded` gate in place.
4. [x] Change `planPatchDOM` and `rowChildrenIndicator` so a missing Header is a value and becomes a row.
5. [x] Change `RowView.Layout.buildRowElement` and `style.css` so that row's Bullet is color #444444.
6. [x] Change the two refusal facts and add the two facts named in Outputs.

## Outputs

[x] `src/Shared/ResidentProjection.fs`
[x] `src/Shared/GraphMutate.fs`
[x] `src/Shared/ViewModelDomPlan.fs`
[x] `src/Shared/ViewModelChildrenIndicator.fs`
[x] `src/Client/RowView.fs`
[x] `src/Server/wwwroot/style.css`
[x] `tests/Shared.Tests/WantTests.fs` — change ``installWantAnswer refuses a dangling edge``
[x] `tests/Shared.Tests/SyncLogicTests.fs` — change ``applySyncResponse refuses dangling Want edges``
[x] `tests/Shared.Tests/ModelTests.fs` — add ``Graph.replace accepts new child id with no Header``
[x] `tests/Shared.Tests/ViewModelTests.fs` — add ``planPatchDOM CreateRow for visible Absent child id``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/03-see-moved-node-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when the parent loaded gate in `replace` must change. That gate is ticket 08 — Apply Replace when parent Header is present.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``installWantAnswer refuses a dangling edge``
  - `tests/Shared.Tests`: ``applySyncResponse refuses dangling Want edges``
  - `tests/Shared.Tests`: ``Graph.replace accepts new child id with no Header``
  - `tests/Shared.Tests`: ``planPatchDOM CreateRow for visible Absent child id``
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/03-see-moved-node-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
