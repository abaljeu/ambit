# Undo Slice 7 measurement

See [[undo-implementation-plan.md]] Slice 7, [[undo-spec.md]], and [[implement-undo-slice-1.md]].

Local Windows Debug timings. Not a production SLA. No SiteMap, validation, encoding, network, or persistence optimization was done.

## Scenario

The same 2,000-Node paste-shaped Change as Slice 1: 2,000 `NewNode` Ops plus one `Replace` under Workspaces. The delivered Undo path plans an ordinary inverse through ClientHistory, then applies it. The inverse has one `Replace` Op and no create Ops.

Slice 1 baseline: `Change.undo` of that paste was 2,175.085 ms with K = 2,000 create-Op rebuild opportunities. This run still measured that leftover destructive path at 3,174.517 ms. It is not the delivered inverse.

## Graph rebuild

Per-created-Node `Graph.fromNodes` is absent on the delivered inverse path. The inverse has `ops=1` and no `NewNode` or `NewSpecialNode`. `ResidentProjection.applyChange` and `History.applyChange` apply that `Replace` once. `Graph.replace` may call `Graph.fromNodes` once for a non-append `Replace`. That is one rebuild for the inverse Change, not one rebuild per created Node.

Reachable Graph equality holds for Undo and Redo of this paste, and in [[tests/Shared.Tests/HistoryTests.fs]] nested paste / NewSpecialNode / split cases.

## Phases

Isolated Shared repeats (three runs, then the budgeted run):

| Phase | What was timed | ms (budgeted run) | Isolated repeats |
| --- | --- | --- | --- |
| Inverse planning | `ClientHistory.undo` / `Change.inverse` | 3.146 | 3.019, 3.096, 3.179 |
| Projected apply | `ResidentProjection.applyChange` of the inverse | 45.740 | 45.577, 45.732, 46.427 |
| SiteMap reconciliation | `ViewModel.reconcileSiteMapFrom` at Workspaces after Undo | 4.220 | 3.921, 3.898, 3.924 |
| Encoding | `encodeChangeBatch` of the inverse | 23.012 | 30.351, 28.529, 23.401 |
| Server apply | `History.applyChange` of the inverse (same apply FileAgent uses) | 70.075 | 71.414, 69.756, 71.442 |
| Persistence | FileAgent disk persist + ChangeLog append | unmeasured as a separate clock | nested in total response |
| ACK encoding | `encodeChangeBatchAck` of the inverse | 8.233 | 11.023, 9.872, 8.523 |
| Total response | File-backend POST `/ambit/changes` of the inverse after the paste | 491.621 | one File-backend sample |

Persistence was not split out of FileAgent. The 491.621 ms total includes decode, validation, Server apply, persistence, stamp overlay, ACK encoding, and HTTP.

## Inverse budget

A stable projected-apply budget of 300 ms was added on the delivered inverse path. That is the same local Debug ceiling as the existing 2,000-Node bulk apply test. Isolated projected apply was 45.6–46.4 ms. No wall-time budget was added for encoding, SiteMap, persistence, or total response.
