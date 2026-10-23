# 85 — File startup does not replay the event log

Ticket: [[../issues/85-file-startup-does-not-replay-the-event-log.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0. `python` is absent on this machine, so the scan ran under `python3`. The printed size is `checkpointAhead` at 3 lines. It sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-040839.log` and printed `full suite finished`. Server.Tests passed 746, failed 0, skipped 0. Shared.Tests passed 1928, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`).
- 2. **File startup** — `File startup does not replay the event log` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` appends a logged Change whose child is absent from the live graph, then opens a new file host. The restarted graph has no entry for that child id, and `getEventId` is id 3. It passed inside the server run.
- 3. **Soft-fail** — `soft-fail Edit is not in the graph after restart` in `tests/Server.Tests/FileAgentFailureTests.fs` posts the `soft-fail-probe` Edit, disposes the host, and opens a second host on the same directory. No node text is `soft-fail-probe`, and `state.eventId` is id 2. The `Bookkeeping.readEventId` assertion is gone. It passed inside the server run.
- 4. **Snapshot** — `Snapshot writes amb artifacts asynchronously after change` in `tests/Server.Tests/StateEndpointTests.fs` posts a Change whose text is `snapped` and reads that text from the workspace `.amb` file. The meta-file assertions are gone. It passed inside the server run.
- 5. **Reconciler** — `server reconciler applies planner ops through active agent` in `tests/Server.Tests/LazyLoadReconciliationServerTests.fs` waits until `home/.amb` exists, then reconciles `src/main.fs`. The meta-file assertions are gone. It passed inside the server run.
- 6. **Checkpoint** — `File empty EventLog recover exposes getEventId Graph checkpoint`, `File Graph greater than Log drops persist EventLog`, `File equal Log and Graph keeps EventLog`, and `File empty Graph checkpoint live append continues past Graph` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` are unchanged in the diff. Each sets the meta file with `Bookkeeping.writeEventId`. They passed inside the server run.

## 5. Miss

- No findings.

## 6. Found elsewhere

- 1. **File agent page** — Accept. `doc/current/file-agents.md` replaces the Edit checkpoint item with: the File agent reads the graph from the `.amb` files. A meta checkpoint past the log tip truncates the log. In every other case the event id becomes the log tip. The File agent does not write `SYSTEM/gambol.meta`. When `owningSpecials` is empty, `persistCollected` posts `Notice.NodeStamped`.
- 2. **Project list** — Reject. The ticket list in `plan/core-refinement/project.md` keeps each blocker clause after that blocker is on staging. [82 — The mailbox pushes an Edit's special nodes](../issues/82-the-mailbox-pushes-an-edits-special-nodes.md) still names its blocker. Status `coded` stays on [85 — File startup does not replay the event log](../issues/85-file-startup-does-not-replay-the-event-log.md). The list line keeps its blocker clause.
