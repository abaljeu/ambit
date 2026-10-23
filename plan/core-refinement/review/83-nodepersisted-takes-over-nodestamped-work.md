# 83 — NodePersisted takes over NodeStamped's work

Ticket: [[../issues/83-nodepersisted-takes-over-nodestamped-work.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0. `python` is absent on this machine, so the scan ran under `python3`. The printed sizes are `liveOf` at 3 lines and `rememberPushed` at 5 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.
- 2. **Dead notified ids** — Judgement. Smell Overcomplication. `LivePersist.notifiedIds` is always `[]`. The append in `commitPostChange` adds nothing. `rememberPushed` is the write that fills `opsNotifiedIds`.

```fsharp
let private liveOf stamped : LivePersist = {
    stamped = stamped
    notifiedIds = []
}
```

```fsharp
if not graphOnly && not (List.isEmpty fresh) then
    rememberWorkspaces loaded stateToStore.graph fresh
    loaded.opsNotifiedIds.Value <-
        live.notifiedIds @ loaded.opsNotifiedIds.Value
        |> List.distinct
```

## 2. Spec

- 1. **Pushed-node skip** — Extra. `DbAgent.pushPersist` is `rememberPushed`. It records the pushed id on `opsNotifiedIds` and `startFileDiff` skips that id. `commitPostChange` no longer calls `requestFileDiff`. One Edit of ROOT stores one `Notice.NodePersisted`. The ticket moves `noteStamped` onto the `NodePersisted` arm and deletes `baselineGraph`. It leaves this skip out.

```fsharp
/// The mailbox push already queued this node. A later file diff skips it.
let private rememberPushed (loaded: LoadedPersist) (nodeId: NodeId) =
    loaded.opsNotifiedIds.Value <-
        nodeId :: loaded.opsNotifiedIds.Value |> List.distinct
    PersistRequestStack.pushBoundNode loaded.stack nodeId
```

- 2. **Mapped-folder refresh** — Extra. The diff rewrites `doc/current/workspace-file-sync.md` so a persisted Edit's file time refreshes a mapped App folder. `UpdateWorkspaceDownload.accumulateAutoDownloadFromOps` still collects `Op.SetUpdateTime` from event ops. `Notice.NodePersisted` carries a time and no ops. `SyncLogic.applyLoggedNotice` writes that time onto the resident graph. The same diff edits `doc/current/file-agents.md`, `doc/current/mailbox.md`, and `doc/current/parse-persist.md`.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-044905.log` and stopped after Server.Tests. Server.Tests passed 745, failed 1, skipped 0. The failure is `direct core list does not repeat entry admission`: expected `before.events.Length + 6`, actual `before.events.Length + 3`. Shared.Tests was not reached. A separate run with `AMBIT_FULL_SUITE=1` wrote `/tmp/ambit-shared-suite-20261023-045547.log` and passed 1927, failed 1, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`). That failure is `second git waits instead of failing on index.lock`: `add did not create index.lock`. A second run of that filter passed. The diff does not touch `GitRun`.
- 2. **Live graph** — `persist thread writes from the live graph` in `tests/Server.Tests/PersistThreadTests.fs` checks `Notice.NodePersisted` for the file id and that no `Notice.NodeStamped` is posted. It passed inside the server run.
- 3. **Success only** — `persist thread posts NodePersisted on success only` in the same file expects `[ Notice.NodePersisted ]` on success and `Notice.PersistFailed` on failure. It passed inside the server run.
- 4. **Door** — `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` in `tests/Server.Tests/CoreMailboxDoorTests.fs` waits for `Notice.NodePersisted` only. The events after the stored one are `[ Notice.NodePersisted ]`. It passed inside the server run.
- 5. **Persist tip** — `persistTipSettled` in `tests/Server.Tests/StateEndpointTests.fs` no longer requires `persisted = stamped`. `StateEndpointTests` passed inside the server run.
- 6. **Names** — No file under `src/` posts `Notice.NodeStamped`. No file under `src/` names `persistCollected` or `baselineGraph`.

## 5. Miss

- 1. **Admission event count** — Failing test `direct core list does not repeat entry admission` in `tests/Server.Tests/OrderedEventStreamTests.fs`. Expected `before.events.Length + 6`, actual `before.events.Length + 3`. The host is a file agent. Three `Op.NewNode` changes have no owning special node. `FileAgent.persistCollected` used to post `Notice.NodeStamped` for each of those changes. That post is gone, so the log grows by the three Changes.

## 6. Found elsewhere

- No findings.
