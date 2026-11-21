# 83 — NodePersisted takes over NodeStamped's work

Ticket: [[../issues/83-nodepersisted-takes-over-nodestamped-work.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0. The scan ran under `python3`. The printed sizes are `liveOf` at 2 lines and `rememberPushed` at 5 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.
- 2. **Unread live record** — Judgement. Smell Overcomplication. Alan removed `LivePersist.notifiedIds`. `LivePersist` still exists to carry `stamped`, and every caller passes `None`. `finishAppliedPostChange` discards that value with `Ok _`. The remaining work in `submitLiveOps` is the bound-stack check.

```fsharp
type private LivePersist = {
    stamped: PersistGraphOk option
}

let private liveOf stamped : LivePersist = {
    stamped = stamped
}
```

```fsharp
| Ok _ ->
    let stateToStore, ackEvents, persistMessage =
        preparePostChange newState confirmations
```

## 2. Spec

- 1. **Pushed-node skip** — Extra. `DbAgent.pushPersist` is `rememberPushed`. It records the pushed id on `opsNotifiedIds`, and `startFileDiff` skips that id. On staging that skip list stayed empty, because `live.notifiedIds` was always `[]`. One Edit of ROOT stores one `Notice.NodePersisted`. The ticket moves `noteStamped` onto the `NodePersisted` arm and deletes `baselineGraph`. It leaves this skip out.

```fsharp
/// The mailbox push already queued this node. A later file diff skips it.
let private rememberPushed (loaded: LoadedPersist) (nodeId: NodeId) =
    loaded.opsNotifiedIds.Value <-
        nodeId :: loaded.opsNotifiedIds.Value |> List.distinct
    PersistRequestStack.pushBoundNode loaded.stack nodeId
```

```fsharp
let ids =
    pending
    |> List.filter (fun id ->
        not (List.contains id skip) && not (unparsed id))
```

- 2. **Mapped-folder refresh** — Extra. Alan accepts that this refresh may break. The diff rewrites `doc/current/workspace-file-sync.md` so a persisted Edit's file time refreshes a mapped App folder. `UpdateWorkspaceDownload.accumulateAutoDownloadFromOps` still collects `Op.SetUpdateTime` from event ops. `SyncLogic.opsForDownload` returns those ops. `Notice.NodePersisted` carries a time and no ops. `SyncLogic.applyLoggedNotice` writes that time onto the resident graph.

```markdown
[x] A persisted Edit posts `Notice.NodePersisted` with the written file time. The Browser applies that time. [Mailbox](mailbox.md)
[x] The Browser reuses that time to refresh a mapped App folder.
```

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-051614.log` and printed `full suite finished`. Server.Tests passed 746, failed 0, skipped 0. Shared.Tests passed 1928, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`).
- 2. **Live graph** — `persist thread writes from the live graph` in `tests/Server.Tests/PersistThreadTests.fs` checks `Notice.NodePersisted` for the file id and that no `Notice.NodeStamped` is posted. It passed inside the server run.
- 3. **Success only** — `persist thread posts NodePersisted on success only` in the same file expects `[ Notice.NodePersisted ]` on success and `Notice.PersistFailed` on failure. It passed inside the server run.
- 4. **Door** — `CoreMailboxApi.postEvent appends an Ev that eventsSince returns` in `tests/Server.Tests/CoreMailboxDoorTests.fs` waits for `Notice.NodePersisted` only. The events after the stored one are `[ Notice.NodePersisted ]`. It passed inside the server run.
- 5. **Persist tip** — `persistTipSettled` in `tests/Server.Tests/StateEndpointTests.fs` ignores the stamped count. `StateEndpointTests` passed inside the server run.
- 6. **Admission count** — `direct core list does not repeat entry admission` in `tests/Server.Tests/OrderedEventStreamTests.fs` expects `before.events.Length + 3`. It passed inside the server run.
- 7. **Names** — No file under `src/` posts `Notice.NodeStamped`. No file under `src/` names `persistCollected` or `baselineGraph`.

## 5. Miss

- No findings.

## 6. Found elsewhere

- No findings.
