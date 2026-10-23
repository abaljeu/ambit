# 82 — The mailbox pushes an Edit's special nodes

Ticket: [[../issues/82-the-mailbox-pushes-an-edits-special-nodes.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Duplicated mark** — Judgement. Smell Duplicated Code. `CoreEventDispatch.persistedNode` repeats `PersistThread.persistedNode`. `CoreEventDispatch.markForPersist` repeats `CoreMailboxLoad.writeAxis` and drops the `Error`.

```fsharp
let private persistedNode (graph: Graph) (nodeId: NodeId) =
    match Map.tryFind nodeId graph.nodes with
    | Some node when
        node.documentState.persistState = PersistState.Persisted ->
        true
    | _ -> false

let private markForPersist
    (context: Context)
    (graph: Graph)
    (nodeId: NodeId)
    =
    if not (persistedNode graph nodeId) then
        graph
    else
        match
            GraphMutate.setPersistState
                nodeId
                PersistState.Unpersisted
                graph
        with
        | Error _ -> graph
        | Ok next ->
            context.persist.replaceGraph
                (Gambol.Shared.Notice.NodePersisting nodeId)
                next
            next
```

```fsharp
let private persistedNode (state: State) (nodeId: NodeId) : bool =
    match Map.tryFind nodeId state.graph.nodes with
    | Some node when
        node.documentState.persistState = PersistState.Persisted ->
        true
    | _ -> false
```

```fsharp
let private writeAxis
    (persist: PersistHandlers)
    (notice: Notice)
    (mutate: Graph -> Result<Graph, string>)
    : Result<unit, string> =
    match persist.getState () with
    | Error err -> Error err
    | Ok state ->
        match mutate state.graph with
        | Error err -> Error err
        | Ok graph ->
            persist.replaceGraph notice graph
            Ok ()
```

- 2. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0. `python` is absent on this machine, so the scan ran under `python3`. The printed sizes are `persistedNode` at 7 lines, `markForPersist` at 21 lines, `pushChangeSpecials` at 22 lines, `submitLiveOps` at 5 lines, `persistLiveChange` at 5 lines, `persistPostChange` at 14 lines, `opAnchor` at 11 lines, `ownerOf` at 5 lines, and `owningSpecials` at 5 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.

## 2. Spec

- 1. **Database-only notices** — Extra. A non-graph-only Change whose owning special is ROOT pushes that node when `DbAgent.create` has no data directory. The persist thread then appends `Notice.NodePersisted` and `Notice.NodeStamped`. The ticket and spec leave that database-only notice pair out. `tests/Server.Tests/DatabaseProjectionContractTests.fs` records the pair as two extra rows in `events`.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-031208.log` and stopped after Server.Tests. Server.Tests passed 744, failed 1, skipped 0. The failure is `db bootstrap duplicate returns stored Change and rejects no-op`: `events` count expected 3, actual 5. Shared.Tests then ran with `AMBIT_FULL_SUITE=1` and passed 1928, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`).
- 2. **Edit push** — `An Edit pushes its owning special node` in `tests/Server.Tests/CoreMailboxDoorTests.fs` posts an Edit of a File Node's text through `TestBackend.cannedHandlers` with a recording `pushPersist`. The recorded id is that File Node. It passed inside the server run.
- 3. **Graph-only** — `A graph-only change pushes nothing` posts that same Edit through `postGraphOnly` and records no id. It passed inside the server run.
- 4. **Mailbox mark** — `new special node writes its file without NodePersisting` in `tests/Server.Tests/FileAgentFailureTests.fs` is unchanged. The file is written and the log has `Notice.NodePersisted` for that workspace with no `Notice.NodePersisting`. It passed inside the server run.
- 5. **Persist thread** — `PersistThreadTests` passed inside the server run. `PersistThread.fs` defines no `owningSpecials` or `opAnchor`. No file under `src/` names `openForWrite`, `pendingNotify`, or `takePendingNotify`.

## 5. Miss

- 1. **Bootstrap event count** — Failing test `db bootstrap duplicate returns stored Change and rejects no-op`. Expected `events` count 3, actual 5. The Change is `Op.NewNode` plus `Op.Replace` on `Graph.rootId`. That root is a Workspace, so `DocumentOpImpact.owningSpecials` returns it, `CoreEventDispatch.pushChangeSpecials` pushes it, and the database-only persist thread appends `Notice.NodePersisted` and `Notice.NodeStamped`.

## 6. Found elsewhere

- No findings.
