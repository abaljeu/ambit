namespace Gambol.Shared

type FocusInsertPoint =
    { parentId: NodeId
      index: int }

[<RequireQualifiedAccess>]
module FileNodeOps =

    let private applyOpToGraph (graph: Graph) (op: Op) : Graph =
        let state = { graph = graph; history = History.empty; revision = Revision.Zero }

        match Op.apply op state with
        | ApplyResult.Changed s -> s.graph
        | ApplyResult.Unchanged s -> s.graph
        | ApplyResult.Invalid(_, msg) -> failwith msg

    let private siblingOwnerNamesLower (graph: Graph) (parentId: NodeId) : Set<string> =
        graph.nodes.[parentId].children
        |> List.choose (fun child ->
            if child.ref <> Ownership.Owner then
                None
            else
                graph.nodes
                |> Map.tryFind child.id
                |> Option.bind (fun node ->
                    match node.name with
                    | Filename.Ok n -> Some(n.ToLowerInvariant())
                    | _ -> None))
        |> Set.ofList

    let private unusedOwnedName (graph: Graph) (parentId: NodeId) (baseName: string) : string =
        let taken = siblingOwnerNamesLower graph parentId

        let rec loop (i: int) =
            let candidate =
                if i = 0 then baseName else sprintf "%s%d" baseName i
            if Set.contains (candidate.ToLowerInvariant()) taken then
                loop (i + 1)
            else
                candidate

        loop 0

    let private baseNameFromQuery (query: string) (defaultName: string) : string =
        let trimmed = query.Trim()
        if System.String.IsNullOrWhiteSpace trimmed then
            defaultName
        else
            match Filename.create trimmed with
            | Filename.Ok name -> name
            | _ -> defaultName

    let private appendOwnedOp (parentId: NodeId) (childId: NodeId) (graph: Graph) : Op =
        let index = Graph.fileTreeInsertIndex graph parentId
        Op.Replace(parentId, index, [], [ { ref = Ownership.Owner; id = childId } ])

    let rec nearestValidOwnerParent (graph: Graph) (nodeId: NodeId) : NodeId =
        if nodeId = Graph.rootId then
            Graph.rootId
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> Graph.rootId
            | Some node ->
                match node.kind with
                | Special (Workspace | Directory) -> nodeId
                | _ -> nearestValidOwnerParent graph node.owner

    let private isValidOwnedParent (graph: Graph) (parentId: NodeId) : bool =
        match Map.tryFind parentId graph.nodes with
        | Some { kind = Special (Workspace | Directory) } -> true
        | _ -> false

    let private planCreateOwnedSpecial
        (graph: Graph)
        (parentId: NodeId)
        (kind: SpecialKind)
        (baseName: string)
        : NodeId * Op list =
        let childId = NodeId.New()
        let name = unusedOwnedName graph parentId baseName
        let ops =
            [ Op.NewSpecialNode(childId, kind, name)
              appendOwnedOp parentId childId graph ]
        childId, ops

    let planCreateWorkspace (graph: Graph) (query: string) : NodeId * Op list =
        planCreateOwnedSpecial graph Graph.workspacesId Workspace (baseNameFromQuery query "workspace")

    let planCreateOwnedFile (graph: Graph) (parentId: NodeId) (query: string) : NodeId * Op list =
        planCreateOwnedSpecial graph parentId File (baseNameFromQuery query "file.txt")

    let planCreateOwnedDirectory (graph: Graph) (parentId: NodeId) (query: string) : NodeId * Op list =
        planCreateOwnedSpecial graph parentId Directory (baseNameFromQuery query "folder")

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

    let planCreateOwnedFileAtFocus
        (graph: Graph)
        (insert: FocusInsertPoint)
        (query: string)
        : NodeId * Op list =
        let ownerParent =
            if isValidOwnedParent graph insert.parentId then
                insert.parentId
            else
                nearestValidOwnerParent graph insert.parentId

        let childId, createOps = planCreateOwnedFile graph ownerParent query

        if ownerParent = insert.parentId then
            childId, createOps
        else
            let refOps = planInsertFileRefAtFocus insert childId graph
            childId, createOps @ refOps

    let planCreateOwnedDirectoryAtFocus
        (graph: Graph)
        (insert: FocusInsertPoint)
        (query: string)
        : NodeId * Op list =
        let ownerParent =
            if isValidOwnedParent graph insert.parentId then
                insert.parentId
            else
                nearestValidOwnerParent graph insert.parentId

        let childId, createOps = planCreateOwnedDirectory graph ownerParent query

        if ownerParent = insert.parentId then
            childId, createOps
        else
            let refOps = planInsertFileRefAtFocus insert childId graph
            childId, createOps @ refOps
