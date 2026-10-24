# 98 — Core marks carry the Edit's id — finish

Handoff after the previous agent died on `resource_exhausted` after the green edit. No commit.

## What changed

`Notice.NodePersisting` and `Notice.NodeUploading` mark Unpersisted without putting the Notice's logged id on `changedVersion`. The Edit-id stamp stays on apply, FileAgent, DbAgent, and `markForPersist`.

### This finish (green handler)

- `src/Server/Core/CoreMailboxLoad.fs`
  - `writeUnpersisted` calls `GraphMutate.setPersistState` with `PersistState.Unpersisted` and returns `Result<Graph, string>`.
  - `Notice.NodePersisting` and `writePullingFileUnpersisted` (`Notice.NodeUploading`) bind that Result.
  - `unpersistedAtLoggedId` is gone from source. No leftover callers.

### Already in the working tree from ticket 98

- `src/Shared/ChangeAmendment.fs` — `applyForCommand` / `applyForGraphOnly` take `changedVersion` first.
- `src/Server/Core/FileAgent.fs` and `src/Server/Core/DbAgent.fs` — pass `event.id`.
- `src/Server/Core/CoreEventDispatch.fs` — `markForPersist` writes `GraphMutate.setUnpersistedAt` at the completed Edit id.

### Tests added by the previous agent (kept)

- `tests/Server.Tests/CoreLoopNoticeTests.fs`
  - `fileStateAtEdit`
  - `NodePersisting leaves the Edit's changedVersion` (Edit id 5, Notice id 12)
  - `NodeUploading leaves the Edit's changedVersion` (Edit id 5, Notice id 12)
- `tests/Server.Tests/PersistApplyTests.fs`
  - `FileAgent applyEvent marks a File Node at the later Edit id`
- `NodePersisting matches the Unpersisted Set` still compares Notice apply to `Graph.setPersistState`. It did not assert the Notice id, so it needed no rewrite.
- `tests/Server.Tests/MailboxExpandTests.fs` — `loadNode of a plain Node replies Error` now asserts the rejected `loadNode` left no `EventBody.Load`. Full-list equality raced with `Notice.NodePersisted` of ROOT from the New's persist push.

Undo/Redo version stamping was Extra on the review. Left alone.

## SageFs

- Daemon `0.6.919`. Healthy.
- Reused session `e6f1f6a2` (`tests/Server.Tests`). Hard-reset with `rebuild=true` via `scripts/sagefs-hard-reset.sh e6f1f6a2 true`. Rebuild succeeded. Source InSync after that reset.
- REPL (from the rebuilt files): `GraphMutate.setPersistState Unpersisted` left `changedVersion` at Edit id 5. The NodeUploading compose (Unpersisted then PullingFile) did the same. `setUnpersistedAt` at Notice id 12 would have stamped 12.
- `CoreMailboxLoad.applyNotice` is internal, so the REPL used the same `setPersistState` path `writeUnpersisted` now calls. xUnit covers `applyNotice`.
- Left running: `e6f1f6a2` (previous agent created it) and `967c9074` (existed, name-polluted). This agent created no session and stopped none.

## Focused tests

`scripts/agent-test.sh --filter` of the seven named Server tests:

- Failed: 0
- Passed: 7
- Skipped: 0
- Total: 7
- Duration: 323 ms

The prior aborted run had Expected 5 Actual 12 on the two Notice tests. This run is after the green edit.

## Full suite

`scripts/agent-test.sh --full` under lease `14ca375ecc1242758cf7bdcedbd9afc1` (agent `ticket-98-finish`). The script prints xUnit totals, not a SageFs `TRUST ran=` line. Those totals are the ran-count.

First run (pid 44566, `/tmp/ambit-full-suite-20261023-214115.log`): Server.Tests Failed 1, Passed 749, Total 750. Shared.Tests did not start (`set -e`). The failure was `loadNode of a plain Node replies Error` comparing the whole log while ROOT's persist appended `Notice.NodePersisted`.

Second run (pid 44954, `/tmp/ambit-full-suite-20261023-214713.log`) after the loadNode assertion change:

- Server.Tests: Failed 0, Passed 750, Skipped 0, Total 750, Duration 3 m 24 s
- Shared.Tests: Failed 0, Passed 1932, Skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`), Total 1933, Duration 13 s
- `full suite finished`

Lease released.

## Leftover risks

- Undo/Redo still stamp `event.id` through `Op.persistMarkVersion`. Review listed that as Extra.
- Unrelated uncommitted markdown: `context/CONTEXT.md`, `context/build-context.md`, `context/review-context.md`.
- Ticket 98 issue file, review file, this report, and the MailboxExpandTests assertion change are uncommitted with the code.
- No commit. No push.
