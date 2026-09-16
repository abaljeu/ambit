# Fix bootstrap duplicate xmin

Project: [[../project.md|Single event source]]. Fact: [[../../../tests/Server.Tests/DatabaseProjectionContractTests.fs|db bootstrap duplicate returns stored Change and rejects no-op]].

## 1. Cause

1.1 **Duplicate is known before the graph write.** After the first post, EventLog holds the submission. The second post hits `applyOneChange` as a known `submissionId`. It does not apply Ops again. Revision stays 1. Event count stays 1. Empty no-op is `Unchanged` and does not write `graph`.

1.2 **Projection write still runs.** [[../../../src/Server/Core/CoreEventDispatch.fs|CoreEventDispatch]] `postEvent` admitted, prepared, then called `persist.postChange` before EventLog commit. [[../../../src/Server/Core/DbAgent.fs|DbAgent]] `processPostChange` treated the duplicate confirmation as `fresh` and always called `persistGraphProjection`. That path always `UpsertGraph`. Postgres `xmin` on `graph` moves even when revision and nodes do not change.

1.3 **The +2 xmin delta is one rewrite.** Probe after the duplicate (before the no-op) already showed `xmin` moved by 2. The first post's `appendEvent` consumed one xid without touching `graph`. The duplicate's one `UpsertGraph` then set `xmin` to a xid two steps later. The no-op did not write. The assert is a no-write check. Do not weaken it.

1.4 **FileAgent already skipped the write.** [[../../../src/Server/Core/FileAgent.fs|FileAgent]] `applyBatch` does not add a known `submissionId` to `fresh` and only persists when `changed`. DbAgent did not.

## 2. Files

1. [[../../../src/Server/Core/CoreEventDispatch.fs|CoreEventDispatch.fs]] — `tryStored` before `persist`. Known `submissionId` returns the stored Ev and does not call `postChange`.
2. [[../../../src/Server/Core/DbAgent.fs|DbAgent.fs]] — when `applyBatch` leaves revision unchanged, ack the stored confirmations and skip `persistGraphProjection`.
3. [[../../../tests/Server.Tests/DatabaseProjectionContractTests.fs|DatabaseProjectionContractTests.fs]] — not edited. Leftover `Change.id` is overwritten in persist. The fact was correct.

## 3. Commands and outcomes

1. `scripts/gitstatus.sh` — working tree already had Core files dirty from earlier clusters. No commit. No push.
2. `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~db bootstrap duplicate returns stored Change"` — red. `Assert.Equal` xmin `27953` vs `27955` (same +2 as prior runs). Duplicate ack and Unchanged reject already passed.
3. Same filter with `[DEBUG-a4f2]` probes — duplicate log: `applyOneChange duplicate`, then `persistGraphProjection rev=1`, then `xmin first=27971 afterDup=27973`. No-op log: `applyOneChange Unchanged`, `persist Error Unchanged submission is rejected.` No second projection write.
4. After the fix, `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~db bootstrap duplicate returns stored Change|FullyQualifiedName~startup sweep is a no-op without graph singleton"` — green. Passed 2, failed 0.

Debug probes removed. Scratch `tmp/xmin-bootstrap-loop.sh` deleted after the green run.
