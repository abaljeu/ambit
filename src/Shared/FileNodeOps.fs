namespace Gambol.Shared

type FocusInsertPoint =
    { parentId: NodeId
      index: int }

[<RequireQualifiedAccess>]
module FileNodeOps =

    let private findOwnerChild
        (graph: Graph)
        (parentId: NodeId)
        (kind: SpecialKind)
        (name: string)
        : NodeId option =
        let lower = name.ToLowerInvariant()

        graph.nodes.[parentId].children
        |> List.tryPick (fun child ->
            if child.ref <> Ownership.Owner then
                None
            else
                graph.nodes
                |> Map.tryFind child.id
                |> Option.bind (fun node ->
                    match node.kind, node.name with
                    | Special k, Filename.Ok n when k = kind && n.ToLowerInvariant() = lower ->
                        Some node.id
                    | _ -> None))

    let private appendOwnedOp (parentId: NodeId) (childId: NodeId) (graph: Graph) : Op =
        let index = graph.nodes.[parentId].children.Length
        Op.Replace(parentId, index, [], [ { ref = Ownership.Owner; id = childId } ])

    let private applyOpToGraph (graph: Graph) (op: Op) : Graph =
        let state = { graph = graph; history = History.empty; revision = Revision.Zero }

        match Op.apply op state with
        | ApplyResult.Changed s -> s.graph
        | ApplyResult.Unchanged s -> s.graph
        | ApplyResult.Invalid(_, msg) -> failwith msg

    let planCreateFileInWorkspaces
        (graph: Graph)
        (target: ConcreteFileTarget)
        : Result<NodeId * Op list, string> =
        let fileId = NodeId.New()

        let rec walk
            (parentId: NodeId)
            (segs: (SpecialKind * string) list)
            (gAcc: Graph)
            (ops: Op list)
            : Result<NodeId * Op list, string> =
            match segs with
            | [] ->
                let idx = gAcc.nodes.[parentId].children.Length
                let ops2 =
                    ops
                    @ [ Op.NewSpecialNode(fileId, File, target.fileName)
                        Op.Replace(parentId, idx, [], [ { ref = Ownership.Owner; id = fileId } ]) ]
                Ok(fileId, ops2)
            | (kind, name) :: remaining ->
                match findOwnerChild gAcc parentId kind name with
                | Some childId -> walk childId remaining gAcc ops
                | None ->
                    let childId = NodeId.New()
                    let newOps =
                        [ Op.NewSpecialNode(childId, kind, name)
                          appendOwnedOp parentId childId gAcc ]
                    let gNext = newOps |> List.fold applyOpToGraph gAcc
                    walk childId remaining gNext (ops @ newOps)

        if List.isEmpty target.missingSegments then
            let parentId = target.parentId
            let idx = graph.nodes.[parentId].children.Length

            let ops =
                [ Op.NewSpecialNode(fileId, File, target.fileName)
                  Op.Replace(parentId, idx, [], [ { ref = Ownership.Owner; id = fileId } ]) ]

            Ok(fileId, ops)
        else
            walk target.parentId target.missingSegments graph ([] : Op list)

    let planInsertFileRefAtFocus
        (insert: FocusInsertPoint)
        (fileNodeId: NodeId)
        (graph: Graph)
        : Op list =
        if insert.index < 0 || insert.index > graph.nodes.[insert.parentId].children.Length then
            []
        else
            let newRef = { ref = Ownership.Ref; id = fileNodeId }

            let already =
                graph.nodes.[insert.parentId].children
                |> List.tryItem insert.index
                |> Option.exists (fun c -> c.ref = Ownership.Ref && c.id = fileNodeId)

            if already then
                []
            else
                [ Op.Replace(insert.parentId, insert.index, [], [ newRef ]) ]

    let planAddFileAtFocus
        (graph: Graph)
        (insert: FocusInsertPoint)
        (target: ConcreteFileTarget)
        : Result<NodeId * Op list, string> =
        planCreateFileInWorkspaces graph target
        |> Result.map (fun (fileId, createOps) ->
            let graph2 =
                createOps |> List.fold applyOpToGraph graph

            let insertOps = planInsertFileRefAtFocus insert fileId graph2
            fileId, createOps @ insertOps)
