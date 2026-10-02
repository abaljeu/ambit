# Code review — InMsg on the mailbox queue

Range: uncommitted working tree vs `HEAD`, plus untracked [Core loop InMsg tests](../../../tests/Server.Tests/CoreLoopInMsgTests.fs). Spec: [06 — Explicit parse command on a File (Load)](../issues/06-explicit-parse-command-load-file.md), [core-refinement architecture](../arch.md) §5, §9, and §10 Core loop, [core-refinement map](../map.md) decisions 15 and 16. This report is not approval. Ticket Status stays `coded`.

## Standards

1. **Hard** — [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md): "Don't use Exceptions. Use Error types." `dispatchItem` adds a `try/with` around `applyInMsg`. `writeAxis` then drops `Result` (`Error _ -> ()`), so `GraphMutate` failures such as "node not found" never leave the loop.

```fsharp
let private dispatchItem
    (context: MailboxContext)
    (item: QueueSum)
    =
    match item with
    | QueueSum.Core msg -> dispatch context msg
    | QueueSum.In msg ->
        try
            applyInMsg context.persist msg
        with ex ->
            try
                context.onError "InMsg" "" ex
            with _ ->
                ()
```

```fsharp
let private writeAxis
    (persist: PersistHandlers)
    (mutate: Graph -> Result<Graph, string>)
    =
    match persist.getState () with
    | Error _ -> ()
    | Ok state ->
        match mutate state.graph with
        | Error _ -> ()
        | Ok graph -> persist.replaceGraph graph
```

2. **Hard** — [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md): "Follow language norms. Match existing style." `CoreMsg` in the same file is `type internal`. New `InMsg` is public. `InternalsVisibleTo` already exposes `internal` to the test assembly.

```fsharp
type InMsg =
    | ParseFinished of nodeId: NodeId
    | SnapshotDone of nodeId: NodeId * graph: Graph option
```

3. **Judgement — Mysterious Name** — `isAxisOp` reads as every axis op. The body is only `Op.SetDocumentState`.

```fsharp
let private isAxisOp (op: Op) =
    match op with
    | Op.SetDocumentState _ -> true
    | _ -> false
```

4. **Judgement — Mysterious Name** — `snapshotNode` reads as the snapshotted node. It returns the first child of the workspaces node, or `Graph.workspacesId` when that list is empty.

```fsharp
let private snapshotNode (graph: Graph) =
    match Graph.children graph Graph.workspacesId with
    | child :: _ -> child.id
    | [] -> Graph.workspacesId
```

5. **Judgement — Duplicated Code** — the same graph replace is filled on both agents (`DbAgent` `persistHandlers` and `FileAgent` `persistHandlers`):

```fsharp
replaceGraph = fun graph ->
    loaded.state.Value <-
        { loaded.state.Value with graph = graph }
```

Mechanical scan: printed function sizes stay inside the documented limits. No scan finding.

## Spec

1. **Partial** — Ticket 06: "`Op.SetDocumentState` is not the writer of the parsed axis. `Op.SetPersistState` is not a writer." The parse thread drops `SetDocumentState` before `postOps`. Other parse callers still post it, and `Node.withDocumentState` still writes the parsed axis. [core-refinement architecture](../arch.md) §10 Core loop item 9 is the same rule and is still open. There is no `Op.SetPersistState`.

2. **Wrong** — `SnapshotDone` sets Persisted only when the graph is `Some`. [core-refinement architecture](../arch.md) §10: "`SnapshotDone` sets that node Persisted through `GraphMutate.setPersistState`". [core-refinement map](../map.md) decision 16: "One completion: snapshot finished and Persisted is set". §5 item 5: "packaged with setting Persisted." A failed live snapshot still posts `SnapshotDone` with `None` and leaves `PersistState` unchanged. `CoreLoopInMsgTests` locks that in.

3. **Wrong** — The db agent sets the node with `snapshotNode`: the first child of `Graph.workspacesId`, or `workspacesId` if there is none. `setPersistState` rejects the workspaces container, and `writeAxis` ignores the error, so that completion sets no Persisted. With children, only the first workspace is marked. The live snapshot writes the whole graph. [core-refinement architecture](../arch.md) §10: "That `NodeId` and the snapshot `Graph option`. Snapshot finished and that node is Persisted." The spec does not name this node.

4. **Wrong** — `finishParse` runs before the content post, and `postContent` discards the post result, so the node can be Parsed when `planParseFile`'s ops never apply. Ticket 06: "When this File parse finishes, the parse thread adds InMsg ParseFinished for that node".

5. **Unnamed type** — `QueueSum`. Spec: "That sum has no public name." The diff adds `type internal QueueSum`.

6. **Unnamed type** — `PersistHandlers.replaceGraph`. The spec does not mention `replaceGraph`. It names `GraphMutate.setParseState` and `GraphMutate.setPersistState`.

7. **Unnamed type** — `ParseThreadDeps.finishParse`. The spec does not mention `finishParse`. It says the parse thread adds `ParseFinished` through the private function on the mailbox.

8. **Unnamed type** — `PersistFilling.bindSnapshot` now takes `InMsg -> unit`. The spec does not mention `bindSnapshot`.

9. **Unnamed type** — `MailboxHost.postItem` posts `QueueSum`. The spec does not mention a door that takes the queue sum. It names a private `InMsg` add and a public `CoreMsg` add.

## Summary

Standards: 5 findings (worst: `try/with` around `applyInMsg` and `writeAxis` dropping `Result`). Spec: 9 findings (worst: `SnapshotDone` does not set Persisted for `None`, and `snapshotNode` can mark a node the axis write rejects).
