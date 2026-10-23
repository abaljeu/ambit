# 85 — File startup does not replay the event log

Ticket: [[../issues/85-file-startup-does-not-replay-the-event-log.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0 with no printed lines. `python` is absent on this machine, so the scan ran under `python3`. The diff is the document step: `doc/current/file-agents.md` and `plan/core-refinement/doc/85-file-startup-does-not-replay-the-event-log.md`. Checked against `.agents/rules/writing-markdown.md`, `.agents/rules/refer-by-name.md`, and `doc/current/CONTEXT.md`. The Job line that the File agent leaves `SYSTEM/gambol.meta` unwritten is the accepted Found elsewhere wording. `doc/current/CONTEXT.md` allows that negative when it is a non-obvious protocol assertion.

## 2. Spec

The document diff is the range against `origin/staging`. `src/Server/Core/FileAgent.fs` on `origin/staging` is the code checked. This branch leaves that file unchanged.

- 1. **Graph from the files** — `doc/current/file-agents.md` says the File agent reads the graph from the `.amb` files. `FileAgent.loadOrFail` calls `DocumentLoader.tryLoadState`. `DocumentPersistChange.readAllDocuments` reads the directory `.amb` texts and `DocumentAssembly.assembleFromArtifactsBounded` builds the graph. Matches.
- 2. **Checkpoint past the tip** — The page says a meta checkpoint past the log tip truncates the log. `FileAgent.reconcileLoaded` calls `EventLogFile.truncate` and stores `EventLog.afterCheckpoint` when `state.eventId` is greater than `EventLog.tip`. That id is `Bookkeeping.readEventId` from `SYSTEM/gambol.meta`. The loaded graph stays. Matches.
- 3. **Every other case** — The page says in every other case the event id becomes the log tip. The other branch of `reconcileLoaded` sets `state.eventId` to `EventLog.tip` and leaves the graph and the log. Matches.
- 4. **Meta file** — The page says the File agent leaves `SYSTEM/gambol.meta` unwritten. `FileAgent.persistHandlers` sets `noteStamped` to `ignore`. `FileAgent.fs` has no `Bookkeeping.writeEventId` call. No file under `src/` calls `Bookkeeping.writeEventId`. Matches.
- 5. **Empty owning specials** — The page says when `DocumentOpImpact.owningSpecials` is empty, `FileAgent.persistCollected` posts `Notice.NodeStamped`. That is the `List.isEmpty` branch in `persistCollected`. Matches.
- 6. **Link file** — `plan/core-refinement/doc/85-file-startup-does-not-replay-the-event-log.md` points Current and Found elsewhere at `doc/current/file-agents.md`. User is empty. No `doc/user` page changed. `doc/user/guide/file-mapping.md` still describes mapped-file edit and parse. This ticket adds no new way to operate Ambit.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-044756.log` and printed `full suite finished`. Server.Tests passed 746, failed 0, skipped 0. Shared.Tests passed 1928, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`).
- 2. **File startup** — `File startup does not replay the event log` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` appends a logged Change whose child is absent from the live graph, then opens a new file host. The restarted graph has no entry for that child id, and `getEventId` is id 3. The suite passed.
- 3. **Soft-fail** — `soft-fail Edit is not in the graph after restart` in `tests/Server.Tests/FileAgentFailureTests.fs` posts the `soft-fail-probe` Edit, disposes the host, and opens a second host on the same directory. No node text is `soft-fail-probe`, and `state.eventId` is id 2. The `Bookkeeping.readEventId` assertion is gone. The suite passed.
- 4. **Snapshot** — `Snapshot writes amb artifacts asynchronously after change` in `tests/Server.Tests/StateEndpointTests.fs` posts a Change whose text is `snapped` and reads that text from the workspace `.amb` file. The meta-file assertions are gone. The suite passed.
- 5. **Reconciler** — `server reconciler applies planner ops through active agent` in `tests/Server.Tests/LazyLoadReconciliationServerTests.fs` waits until `home/.amb` exists, then reconciles `src/main.fs`. The meta-file assertions are gone. The suite passed.
- 6. **Checkpoint** — `File empty EventLog recover exposes getEventId Graph checkpoint`, `File Graph greater than Log drops persist EventLog`, `File equal Log and Graph keeps EventLog`, and `File empty Graph checkpoint live append continues past Graph` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` are unchanged in the document diff. Each sets the meta file with `Bookkeeping.writeEventId`. The suite passed.
- 7. **Document step** — The document step names no test.

## 5. Miss

- No findings.

## 6. Found elsewhere

- 1. **File agent page** — Accept. Written on `doc/current/file-agents.md`: the File agent reads the graph from the `.amb` files. A meta checkpoint past the log tip truncates the log. In every other case the event id becomes the log tip. The File agent does not write `SYSTEM/gambol.meta`. When `owningSpecials` is empty, `persistCollected` posts `Notice.NodeStamped`. Those lines match `FileAgent` on staging.
- 2. **Project list** — Reject. The ticket list in `plan/core-refinement/project.md` keeps each blocker clause after that blocker is on staging. [82 — The mailbox pushes an Edit's special nodes](../issues/82-the-mailbox-pushes-an-edits-special-nodes.md) still names its blocker. Status `coded` stays on [85 — File startup does not replay the event log](../issues/85-file-startup-does-not-replay-the-event-log.md). The list line keeps its blocker clause. The document step left that list as it is.
