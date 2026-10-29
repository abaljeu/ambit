# 03 — See a moved Node
**Status**: 
One job: The outliner sees a Node another tab moves into an open section as a placeholder Bullet in color #444444. The story See a moved Node in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [02 — Survive a Server crash](plan/client-never-fails/issues/02-survive-server-crash/02-survive-server-crash-ticket.md)

## Inputs

### Working

[ ] ``ResidentProjection.installWantAnswer: Map<NodeId, ChildNode list> -> Node list -> Graph -> Result<Graph, string>`` in `src/Shared/ResidentProjection.fs`.
[ ] ``GraphMutate.replace`` returns `Result<Graph, string>` in `src/Shared/GraphMutate.fs`. Today a missing child id returns `Error` of a string `new child not found`.
[ ] ``ViewModelDomPlan.planPatchDOM: VM -> VM -> Set<SiteId> -> RowMutation list`` in `src/Shared/ViewModelDomPlan.fs`.
[ ] `RowView.Layout.buildRowElement` in `src/Client/RowView.fs` reads `model.graph.nodes.[nodeId]`.
[ ] Bullet color today is `.amb-fold-toggle` in `src/Server/wwwroot/style.css` (`#000` and `#44f`).
[ ] `installWantAnswer` returns `Error` of a string `dangling edge` when an edge target id is missing from `mergedNodes`.
[ ] `GraphMutate.replace` returns `Error` of a string `new child not found` when `newChildren` names an id outside `graph.nodes`. The parent `isLoaded` gate stays for ticket 08 — Apply Replace when parent Header is present.
[ ] `planPatchDOM` binds `newModel.graph.nodes.[entry.nodeId]` on the upsert path.
[ ] `ViewModelSiteMap.walkReconciled` already makes a row from a child id with no Header lookup. This ticket leaves that file as it is.
[ ] ``installWantAnswer refuses a dangling edge`` in [[tests/Shared.Tests/WantTests.fs]] expects `Error` of a string `dangling edge`.
[ ] ``applySyncResponse refuses dangling Want edges`` in [[tests/Shared.Tests/SyncLogicTests.fs]] expects the same `Error` of a string.
[ ] ``planPatchDOM expand inserts correct child count`` in [[tests/Shared.Tests/ViewModelTests.fs]] covers `CreateRow` when Headers are present.
[ ] `GraphBuild.fromNodes` in `src/Shared/GraphBuild.fs` already keeps a child id when `nodes` has no Header for it.
[ ] `ViewModelDomPlan.selectionClassPatches` already uses `Map.tryFind` and treats a missing Header as no patch.
[ ] [[src/Shared/ResidentProjection.fs]] — `installWantAnswer` returns `Ok` when a child id has no Header.
[ ] [[src/Shared/GraphMutate.fs]] — `replace` returns `Ok` for a new child id with no Header. The parent loaded gate stays.
[ ] [[src/Shared/ViewModelDomPlan.fs]] — `planPatchDOM` uses `Map.tryFind`. A missing Header is a placeholder row.
[ ] [[src/Shared/ViewModelChildrenIndicator.fs]] — an Absent Header has a Bullet path that can take the #444444 color.
[ ] [[src/Client/RowView.fs]] — `buildRowElement` draws that row.
[ ] [[src/Server/wwwroot/style.css]] — the placeholder Bullet uses color #444444.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `ChildrenStatus` removal, Want filters, and the parent `isLoaded` gate inside `replace`.
[ ] Closed: `src/Shared/ViewModelSiteMap.fs`. Child rows already come from child ids.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `installWantAnswer` so a child id with no Header returns `Ok` and stays on the edge.
3. [ ] Change `GraphMutate.replace` so a new child id with no Header returns `Ok`. Leave the parent `GraphChildren.isLoaded` gate in place.
4. [ ] Change `planPatchDOM` and `rowChildrenIndicator` so a missing Header is a value and becomes a row.
5. [ ] Change `RowView.Layout.buildRowElement` and `style.css` so that row's Bullet is color #444444.
6. [ ] Change the two refusal facts and add the two facts named in Outputs.

## Outputs

[ ] `src/Shared/ResidentProjection.fs`
[ ] `src/Shared/GraphMutate.fs`
[ ] `src/Shared/ViewModelDomPlan.fs`
[ ] `src/Shared/ViewModelChildrenIndicator.fs`
[ ] `src/Client/RowView.fs`
[ ] `src/Server/wwwroot/style.css`
[ ] `tests/Shared.Tests/WantTests.fs` — change ``installWantAnswer refuses a dangling edge``
[ ] `tests/Shared.Tests/SyncLogicTests.fs` — change ``applySyncResponse refuses dangling Want edges``
[ ] `tests/Shared.Tests/ModelTests.fs` — add ``Graph.replace accepts new child id with no Header``
[ ] `tests/Shared.Tests/ViewModelTests.fs` — add ``planPatchDOM CreateRow for visible Absent child id``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/03-see-moved-node-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when the parent loaded gate in `replace` must change. That gate is ticket 08 — Apply Replace when parent Header is present.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``installWantAnswer refuses a dangling edge``
  - `tests/Shared.Tests`: ``applySyncResponse refuses dangling Want edges``
  - `tests/Shared.Tests`: ``Graph.replace accepts new child id with no Header``
  - `tests/Shared.Tests`: ``planPatchDOM CreateRow for visible Absent child id``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/03-see-moved-node-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
