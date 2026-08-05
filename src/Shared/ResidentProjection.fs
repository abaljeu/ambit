namespace Gambol.Shared

/// Authoritative Sync install: ordered Change tail plus optional resident packages.
type SyncResponse =
    { changes: Change list
      /// Complete Workspace / child-list snapshots at the response revision.
      packages: Node list }

/// Projected Graph transitions for a resident (possibly partial) client Graph.
[<RequireQualifiedAccess>]
module ResidentProjection =

    /// Apply one Op under Loaded rules: header facts only when Resident;
    /// structural Replace only when the parent child list is Loaded.
    let applyOp (op: Op) (state: State) : ApplyResult =
        match op with
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetDocumentState(nodeId, _, _)
        | Op.SetUpdateTime(nodeId, _, _) ->
            if Map.containsKey nodeId state.graph.nodes then
                Op.apply op state
            else
                ApplyResult.Unchanged state
        | Op.Replace(parentId, _, _, _) ->
            match Map.tryFind parentId state.graph.nodes with
            | Some parent when parent.childrenStatus = Loaded ->
                Op.apply op state
            | _ ->
                ApplyResult.Unchanged state
        | Op.NewNode _
        | Op.NewSpecialNode _ ->
            Op.apply op state

    let applyChange (change: Change) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match applyOp op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            change.ops
            |> List.fold
                (fun acc op ->
                    match acc with
                    | Error err -> Error err
                    | Ok (s, changed) -> step (s, changed) op)
                (Ok(state, false))

        match result with
        | Error (ApplyResult.Invalid(_, message)) ->
            ApplyResult.Invalid(state, message)
        | Error err -> err
        | Ok (s, false) -> ApplyResult.Unchanged s
        | Ok (s, true) -> ApplyResult.Changed s

    /// Merge authoritative package Nodes and rebuild Loaded-only indexes.
    let installPackages (packages: Node list) (graph: Graph) : Graph =
        if List.isEmpty packages then
            graph
        else
            let merged =
                packages
                |> List.fold
                    (fun nodes node -> Map.add node.id node nodes)
                    graph.nodes
            Graph.fromNodes graph.root merged
