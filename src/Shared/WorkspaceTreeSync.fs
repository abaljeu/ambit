namespace Gambol.Shared

[<RequireQualifiedAccess>]
module WorkspaceTreeSync =

    let shouldSkipEntry (name: string) : bool =
        name = ".git"
        || name = ".amb"
        || name.StartsWith(".", System.StringComparison.Ordinal)

    let private nameLower (name: Filename) : string option =
        match name with
        | Filename.Ok n -> Some(n.ToLowerInvariant())
        | _ -> None

    let private ownedSpecialChildren (graph: Graph) (parentId: NodeId) =
        graph.nodes.[parentId].children
        |> List.choose (fun child ->
            if child.ref <> Ownership.Owner then
                None
            else
                graph.nodes
                |> Map.tryFind child.id
                |> Option.bind (fun node ->
                    match node.kind with
                    | Special (Directory | File) as kind ->
                        nameLower node.name
                        |> Option.map (fun key -> key, child.id, kind, node)
                    | _ -> None))

    let private unusedOwnedName (graph: Graph) (parentId: NodeId) (baseName: string) : string =
        let taken =
            ownedSpecialChildren graph parentId
            |> List.map (fun (key, _, _, _) -> key)
            |> Set.ofList

        let rec loop i =
            let candidate = if i = 0 then baseName else sprintf "%s%d" baseName i
            if Set.contains (candidate.ToLowerInvariant()) taken then loop (i + 1)
            else candidate

        loop 0

    let private appendOwnedOp (graph: Graph) (parentId: NodeId) (childId: NodeId) : Op =
        let index = Graph.fileTreeInsertIndex graph parentId
        Op.Replace(parentId, index, [], [ { ref = Ownership.Owner; id = childId } ])

    let private createStubOps
        (graph: Graph)
        (parentId: NodeId)
        (kind: SpecialKind)
        (name: string)
        : NodeId * Op list =
        let childId = NodeId.New()
        let ops =
            [ Op.NewSpecialNode(childId, kind, name)
              appendOwnedOp graph parentId childId ]
        childId, ops

    let private renameCollidingNodeOps (graph: Graph) (node: Node) (nodeId: NodeId) : Op list * string =
        match node.name with
        | Filename.Ok oldName ->
            let suffix =
                match node.kind with
                | Special File -> "-was-file"
                | Special Directory -> "-was-dir"
                | _ -> "-was-special"
            let newName = unusedOwnedName graph node.owner (oldName + suffix)
            [ Op.SetName(nodeId, oldName, newName) ], newName
        | _ -> [], ""

    let planShallowSync
        (graph: Graph)
        (dirNodeId: NodeId)
        (diskEntries: DiskTreeEntry list)
        : Result<WorkspaceTreeSyncPlan, string> =
        match Map.tryFind dirNodeId graph.nodes with
        | None -> Error "sync directory node not found"
        | Some dirNode ->
            match dirNode.kind with
            | Special (Workspace | Directory) ->
                let owned =
                    ownedSpecialChildren graph dirNodeId
                    |> List.map (fun (key, id, kind, node) -> key, (id, kind, node))
                    |> Map.ofList

                let mutable created = 0
                let mutable reused = 0
                let mutable renamed = 0
                let mutable notes = []
                let mutable ops = []
                let mutable workingGraph = graph

                let applyLocalOps localOps =
                    workingGraph <-
                        localOps
                        |> List.fold (fun g op ->
                            match Op.apply op { graph = g; history = History.empty; revision = Revision.Zero } with
                            | ApplyResult.Changed s -> s.graph
                            | ApplyResult.Unchanged s -> s.graph
                            | ApplyResult.Invalid(_, msg) -> failwith msg) workingGraph

                    ops <- ops @ localOps

                for entry in diskEntries |> List.filter (fun e -> not (shouldSkipEntry e.name)) do
                    let key = entry.name.ToLowerInvariant()

                    match Map.tryFind key owned with
                    | Some (_, ownedKind, _) when ownedKind = Special entry.kind ->
                        reused <- reused + 1
                    | Some (nodeId, _, node) ->
                        let renameOps, newName = renameCollidingNodeOps workingGraph node nodeId
                        if not renameOps.IsEmpty then
                            applyLocalOps renameOps
                            renamed <- renamed + 1
                            notes <- notes @ [ sprintf "Renamed %s to %s (disk kind wins)" entry.name newName ]

                        let _, createOps =
                            createStubOps workingGraph dirNodeId entry.kind entry.name

                        applyLocalOps createOps
                        created <- created + 1
                    | None ->
                        let _, createOps =
                            createStubOps workingGraph dirNodeId entry.kind entry.name

                        applyLocalOps createOps
                        created <- created + 1

                let summary =
                    { created = created
                      reused = reused
                      renamed = renamed
                      notes = notes }

                let statusText =
                    if created = 0 && reused = 0 && renamed = 0 then
                        "Sync: no changes"
                    else
                        sprintf "Sync: %d created, %d reused, %d renamed" created reused renamed

                let status =
                    if renamed > 0 then
                        Some(StatusMessage.warn (statusText + " — " + String.concat "; " notes))
                    elif created > 0 then
                        Some(StatusMessage.info statusText)
                    else
                        Some(StatusMessage.info statusText)

                Ok { ops = ops; summary = summary; status = status }
            | _ -> Error "sync target must be a Workspace or Directory"
