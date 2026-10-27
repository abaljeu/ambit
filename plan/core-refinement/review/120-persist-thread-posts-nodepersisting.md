# 120 — Persist thread posts NodePersisting

Ticket: [[../issues/120-ordinary-edit-posts-nodeunpersisted.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Standards scan printed no lines** — `python` is not on PATH. `python3 scripts/standards-scan.py --diff 2d8af951^` exited 0 and printed no lines. The `--diff` flag succeeded. No limit break and no GLOSSARY.md PHRASE line.
- 2. **Ticket links leave the project root** — Hard. [Writing markdown](.agents/rules/writing-markdown.md) Links: a file outside the same directory uses a path relative to the project root. The changed Problem line links `../cqrs-messages.md`. The changed Change lines link `../../../src/Server/Core/PersistThread.fs`, `../../../src/Server/Core/CoreMailboxLoop.fs`, `../../../src/Server/Database.fs`, and `../../../src/Server/Core/CoreMailboxFilesAxis.fs`.
- 3. **One checkbox holds several facts** — Hard. Writing markdown Lists: a list of things is an unnumbered list. Writing markdown Facts: one fact per item. The Problem paragraph names `NodePersisting` and `Notice.NodeUnpersisted`. The `CoreMailboxLoop.fs` checkbox states the `NodePersisting` arm and the `NodeUnpersisted` arm. The `Database.fs` checkbox states both arms. The `PersistThreadTests.fs` checkbox names `persist thread posts NodePersisted on success only`, `persist thread posts the version it read`, and the failed write. The `CoreLoopNoticeTests.fs` checkbox names `NodePersisting changes no node state` and `applyNotice NodeUnpersisted marks Unpersisted`. The `DbAgentNoticeTests.fs` checkbox names `NodePersisting writes no persist row` and `NodeUnpersisted writes persist state to the database`.
- 4. **Negative statements** — Hard. Writing markdown Positivity: skip negative statements. The Problem says `NodePersisting` changes no node state. The Change items say the `NodePersisting` arm changes nothing and writes nothing. The soft-fail Tests item says the test does not assert `eventId`.
- 5. **Duplicated postEvent sequence** — Judgement. Smell Duplicated Code in [Smell baseline](plan/context/SMELLS.md). `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` matches Change, then `Notice.NodePersisting`, then `Notice.NodePersisted` on the reversed log, and matches `Notice.NodePersisting` then `Notice.NodePersisted` again on `eventsSince`. Quote: `Assert.Equal(3, ordered.Length)` and the two list patterns in [Core mailbox door tests](tests/Server.Tests/CoreMailboxDoorTests.fs).

## 2. Spec

- 1. **Stack pull persist effect** — For Alan. Spec §2 table row `NodePersisting` and §2.4 **Stack pull** say `NodePersisting` sets Unpersisted. The ticket and Arch §1 say `NodePersisting` changes no node state. [Message shape](plan/core-refinement/cqrs-messages.md) says the persist effect is none. The diff follows the ticket: `PersistThread.runRequest` posts `Notice.NodePersisting` before `persistNode` when the node will be written, `CoreMailboxLoop.applyNotice` leaves that arm unchanged, and `Database.writeNoticeRow` writes no row. Alan decides which persist effect stands.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/check-test.sh 3620` is mode 644 and printed `Permission denied`. `bash scripts/check-test.sh 3620` printed `DONE` and `/tmp/ambit-full-suite-20261027-192359.log`. The log has no `full suite finished` line. `scripts/agent-test.sh --full` stopped after Server because that project failed. Server.Tests passed 701, failed 20, skipped 1 (`DocumentPersistenceTests.upload structure persistence preserves existing file mtime`), total 722, duration 2 m 53 s. The Shared command from that script, `dotnet test tests/Shared.Tests -nodeReuse:false -p:AMBIT_FULL_SUITE=1`, passed 1905, failed 0, skipped 3 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`, `WorkspaceUploadStructureTests.reuses existing owned path without duplicating`, `WorkspaceUploadStructureTests.planServerFilePresentOps transitions only matching absent files`), total 1908, duration 24 s.
- 2. **persist thread posts NodePersisted on success only** — [Persist thread tests](tests/Server.Tests/PersistThreadTests.fs) expects `Notice.NodePersisting` then `Notice.NodePersisted` on success, and `Notice.NodePersisting` then `Notice.PersistFailed` on a failed write. It passed inside the server run.
- 3. **persist thread posts the version it read** — That same file expects `Notice.NodePersisting` then `Notice.NodePersisted` with the version `currentState` returned. It passed inside the server run.
- 4. **NodePersisting changes no node state** — [Core loop notice tests](tests/Server.Tests/CoreLoopNoticeTests.fs) calls `CoreMailboxLoop.applyNotice` and expects the same document state, `PersistState.Persisted`, `ParseState.Parsed`, and the same update time. It passed inside the server run.
- 5. **applyNotice NodeUnpersisted marks Unpersisted** — That same file still expects `PersistState.Unpersisted`. It passed inside the server run.
- 6. **NodePersisting writes no persist row** — [Db agent notice tests](tests/Server.Tests/DbAgentNoticeTests.fs) expects persist state, parse state, `changedVersion`, and update time to stay equal to the row before the notice. It passed inside the server run.
- 7. **NodeUnpersisted writes persist state to the database** — That same file still expects `PersistState.Unpersisted`. It passed inside the server run.
- 8. **CoreMailboxApi.postEvent appends an Ev that eventsSince returns** — The door test expects Change, then `Notice.NodePersisting`, then `Notice.NodePersisted`. It also runs `Assert.Equal(3, ordered.Length)`. That count is outside Outputs. The test passed inside the server run.
- 9. **new special node's first file write posts NodePersisting** — [File agent failure tests](tests/Server.Tests/FileAgentFailureTests.fs) expects `Notice.NodePersisting` for that workspace before `Notice.NodePersisted`. It passed inside the server run.
- 10. **soft-fail Edit is not in the graph after restart** — That same file checks that no node text is `soft-fail-probe`. The `Assert.Equal(EventId.fromJson 2, state.eventId)` line is gone. It passed inside the server run.
- 11. **Exact event id and exact event list failures** — Twenty server tests failed. Nineteen assert `EventId.fromJson` of a fixed integer. `db bootstrap duplicate returns stored Change and rejects no-op` asserts the stored labels `["Change", "NodePersisted ROOT"]`, and the actual list also contains `Notice.NodePersisting`. The integer failures, expected then actual: `New server uses snapshot + log replay` 4 then 6; `POST with stale base revision and valid SetText succeeds` 4 then 6; `POST same-parent structural collision amends and succeeds` 6 then 9; `DB rows persist after agent cache reset` 2 then 3; `DB is present in db mode` 2 then 3; `POST changes twice bumps revision to 2` 4 then 6; `DB restart keeps inverse Change in EventLog` 4 then 6; `POST same submissionId twice is idempotent` 2 then 3; `DB rows survive second server startup without DB reset` 2 then 3; `DB restart replays log when projection is cleared` 4 then 6; `POST changes persists in GET state` 4 then 6; `POST changes batch with bad second change keeps earlier items` 2 then 3; `POST changes SetText changes child text and bumps revision` 4 then 6; `DB restart preserves NodeIds` 2 then 3; `POST changes NewNode+Replace adds child to root` 2 then 3; `DB restart keeps duplicate submissionId idempotent` 2 then 3; `POST duplicate submissionId with stale revision stays idempotent` 4 then 6; `POST unrelated structural edits with stale revision both succeed` 6 then 9; `POST unrelated attribute edits with stale revision both succeed` 8 then 12. The nineteen live in [State endpoint tests](tests/Server.Tests/StateEndpointTests.fs). The label list lives in [Database projection contract tests](tests/Server.Tests/DatabaseProjectionContractTests.fs).

## 5. Miss

- 1. **Ticket links leave the project root** — Hard. Point the message-shape link at `plan/core-refinement/cqrs-messages.md`. Point each source link at its `src/` path from the project root.
- 2. **One checkbox holds several facts** — Hard. Give the Problem notices, each Change arm, and each named test its own list item.
- 3. **Negative statements** — Hard. State the positive effect: `NodePersisting` leaves node state as it is, `writeNoticeRow` leaves the row unwritten, and the soft-fail test checks that no node text is `soft-fail-probe`.
- 4. **Exact event id and exact event list failures** — The twenty tests in Tests item 11 failed. Each State endpoint failure asserts an exact event id. The bootstrap test asserts an exact stored-event label list and the actual labels include `Notice.NodePersisting`.
- 5. **postEvent counts the log** — `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` checks Outputs and also checks `Assert.Equal(3, ordered.Length)`.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
