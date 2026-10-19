# 58 — Remove dead snapshot code

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

The server writes individual document files. It does not write a whole-graph snapshot. `flushSnapshot` returns `Ok ()`. `FileAgent.snapshotDone` ignores its arguments. The `Graph` on `InMsg.SnapshotDone` is not read by a live writer. The follow-up file diff in `DbAgent` is live and misnamed. Trace: [Message shape](../cqrs-messages.md) §6 **SnapshotDone**.

## What to build

Delete the confirmed-dead snapshot pieces. Rename the live follow-up file diff. Do not change which files persist writes. Do not add the launch-version check. That stays [46 — Record the persist version](46-record-the-persist-version.md). Upload, import, parse, Load, undo, and persist stay working.

### 1. Dead code and names

1. [ ] **Flush** — Remove `flushSnapshot` from `DbAgent`, `FileAgent`, `PersistFilling`, `MailboxHost`, and `CoreMailboxApi`. `SavePrep.syncDataDir` does not take a flush. When `DbStatus` is not `Ok`, save still reads the event id. `prepareGitSave` and `flushForGit` in `RouteRegistration.fs` stop passing a flush. Tests that only called `flushSnapshot` to get `Ok ()` drop that call.
2. [ ] **Graph** — `InMsg.SnapshotDone` carries the node id and no `Graph`. `PersistThread.finishWhenBound` posts that id. `CoreMailboxLoad.applyInMsg` still sets `PersistState.Persisted` from the node id. Remove the `graphEquals` copy in `DbAgent.handleSnapshotDone`. `DocumentPersistPath.stampNode` still stamps the graph the writer returns. If [46 — Record the persist version](46-record-the-persist-version.md) has already replaced this case with `NodePersisted`, do not put a `Graph` back on it.
3. [ ] **File no-op** — `FileAgent` does not supply snapshot bookkeeping. The database filling keeps the diff-count hook, with no `Graph`.
4. [ ] **Rename** — `requestSnapshot` becomes `requestFileDiff`. `startSnapshot` becomes `startFileDiff`. `enqueueSnapshot` becomes `enqueueFileDiff`. `snapshotWorkspaces` becomes `diffWorkspaces`. `snapshotIds` becomes `diffIds`. `snapshotMarks` becomes `diffMarksLeft`. `snapshotNeeded` becomes `diffNeeded`. `snapshotInProgress` becomes `diffInProgress`. `persistedGraph` becomes `baselineGraph`. `snapshotPost` becomes `persistFinished`. `bindSnapshot` becomes `bindPersistFinished`. `PersistHandlers.snapshotDone` becomes `noteDiffDone` of `NodeId`. `opsNotifiedIds` keeps its name. The diff still calls `DocumentPersistChange.persistGraphChange` for the same nodes.

### Acceptance Criteria

1. [ ] **No flush** — `flushSnapshot` is gone. Git save still returns an event id.
2. [ ] **Persisted** — A successful file write still sets that node `PersistState.Persisted` from the node id. The persist-finished signal has no `Graph`.
3. [ ] **Diff name** — The follow-up workspace diff still writes differing document files. The snapshot names in item 4 are gone.
4. [ ] **Still working** — Upload, import, parse, Load, undo, and persist still complete.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Remove dead snapshot code](../arch.md#remove-dead-snapshot-code)
- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [46 — Record the persist version](46-record-the-persist-version.md)
- [51 — Stand the persist stack](51-stand-the-persist-stack.md)
- [GLOSSARY](../../GLOSSARY.md)
