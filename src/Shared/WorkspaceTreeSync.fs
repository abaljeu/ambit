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

    let private applyOpsToGraph (graph: Graph) (localOps: Op list) : Graph =
        localOps
        |> List.fold (fun g op ->
            match Op.apply op { graph = g; history = History.empty; revision = Revision.Zero } with
            | ApplyResult.Changed s -> s.graph
            | ApplyResult.Unchanged s -> s.graph
            | ApplyResult.Invalid(_, msg) -> failwith msg) graph

    type private LevelPlan =
        { ops: Op list
          created: int
          reused: int
          renamed: int
          notes: string list
          graph: Graph
          dirIdsByKey: Map<string, NodeId> }

    let private planOneLevel (graph: Graph) (dirNodeId: NodeId) (diskEntries: DiskTreeEntry list) : LevelPlan =
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
        let mutable dirIdsByKey = Map.empty

        let applyLocalOps localOps =
            workingGraph <- applyOpsToGraph workingGraph localOps
            ops <- ops @ localOps

        let registerDir (key: string) (nodeId: NodeId) =
            dirIdsByKey <- dirIdsByKey |> Map.add key nodeId

        for entry in diskEntries |> List.filter (fun e -> not (shouldSkipEntry e.name)) do
            let key = entry.name.ToLowerInvariant()

            match Map.tryFind key owned with
            | Some (nodeId, ownedKind, _) when ownedKind = Special entry.kind ->
                reused <- reused + 1
                if entry.kind = Directory then registerDir key nodeId
            | Some (nodeId, _, node) ->
                let renameOps, newName = renameCollidingNodeOps workingGraph node nodeId
                if not renameOps.IsEmpty then
                    applyLocalOps renameOps
                    renamed <- renamed + 1
                    notes <- notes @ [ sprintf "Renamed %s to %s (disk kind wins)" entry.name newName ]

                let childId, createOps = createStubOps workingGraph dirNodeId entry.kind entry.name
                applyLocalOps createOps
                created <- created + 1
                if entry.kind = Directory then registerDir key childId
            | None ->
                let childId, createOps = createStubOps workingGraph dirNodeId entry.kind entry.name
                applyLocalOps createOps
                created <- created + 1
                if entry.kind = Directory then registerDir key childId

        { ops = ops
          created = created
          reused = reused
          renamed = renamed
          notes = notes
          graph = workingGraph
          dirIdsByKey = dirIdsByKey }

    let private statusForSummary (summary: WorkspaceTreeSyncSummary) : StatusMessage option =
        let statusText =
            if summary.created = 0 && summary.reused = 0 && summary.renamed = 0 then
                "Sync: no changes"
            else
                sprintf "Sync: %d created, %d reused, %d renamed" summary.created summary.reused summary.renamed

        if summary.renamed > 0 then
            Some(StatusMessage.warn (statusText + " — " + String.concat "; " summary.notes))
        elif summary.created > 0 then
            Some(StatusMessage.info statusText)
        else
            Some(StatusMessage.info statusText)

    /// Plan ops for one directory level (immediate children only).
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
                let level = planOneLevel graph dirNodeId diskEntries
                let summary =
                    { created = level.created
                      reused = level.reused
                      renamed = level.renamed
                      notes = level.notes }
                Ok
                    { ops = level.ops
                      summary = summary
                      status = statusForSummary summary }
            | _ -> Error "sync target must be a Workspace or Directory"

    let rec private planRecursiveLevel
        (graph: Graph)
        (dirNodeId: NodeId)
        (branches: DiskTreeBranch list)
        : Result<LevelPlan, string> =
        match Map.tryFind dirNodeId graph.nodes with
        | None -> Error "sync directory node not found"
        | Some dirNode ->
            match dirNode.kind with
            | Special (Workspace | Directory) ->
                let entries = branches |> List.map (fun b -> b.entry)
                let level = planOneLevel graph dirNodeId entries

                let rec foldBranches (acc: LevelPlan) (remaining: DiskTreeBranch list) : Result<LevelPlan, string> =
                    match remaining with
                    | [] -> Ok acc
                    | branch :: rest ->
                        if branch.entry.kind <> Directory || List.isEmpty branch.children then
                            foldBranches acc rest
                        else
                            let key = branch.entry.name.ToLowerInvariant()

                            match Map.tryFind key acc.dirIdsByKey with
                            | None -> foldBranches acc rest
                            | Some childDirId ->
                                planRecursiveLevel acc.graph childDirId branch.children
                                |> Result.bind (fun nested ->
                                    foldBranches
                                        { acc with
                                            ops = acc.ops @ nested.ops
                                            created = acc.created + nested.created
                                            reused = acc.reused + nested.reused
                                            renamed = acc.renamed + nested.renamed
                                            notes = acc.notes @ nested.notes
                                            graph = nested.graph }
                                        rest)

                foldBranches level branches
            | _ -> Error "sync target must be a Workspace or Directory"

    /// Plan ops for a workspace/directory and all nested disk directories (stubs only).
    let planRecursiveSync
        (graph: Graph)
        (dirNodeId: NodeId)
        (branches: DiskTreeBranch list)
        : Result<WorkspaceTreeSyncPlan, string> =
        match planRecursiveLevel graph dirNodeId branches with
        | Error err -> Error err
        | Ok level ->
            let summary =
                { created = level.created
                  reused = level.reused
                  renamed = level.renamed
                  notes = level.notes }
            Ok
                { ops = level.ops
                  summary = summary
                  status = statusForSummary summary }
