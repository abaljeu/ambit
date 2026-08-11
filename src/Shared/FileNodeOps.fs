namespace Gambol.Shared

type FocusInsertPoint =
    { parentId: NodeId
      index: int }

[<RequireQualifiedAccess>]
module FileNodeOps =

    let private baseNameFromQuery (query: string) (defaultName: string) : string =
        let trimmed = query.Trim()
        if System.String.IsNullOrWhiteSpace trimmed then
            defaultName
        else
            match Filename.create trimmed with
            | Filename.Ok name -> name
            | _ -> defaultName

    let private appendOwnedOp (parentId: NodeId) (childId: NodeId) (index: int) : Op =
        Op.Replace(parentId, index, [], [ { ref = Ownership.Owner; id = childId } ])

    let private planCreateOwnedSpecial
        (graph: Graph)
        (focusId: NodeId)
        (kind: SpecialKind)
        (baseName: string)
        : NodeId * Op list =
        match Graph.resolveOwnedFileDirectoryInsert graph focusId with
        | None -> focusId, []
        | Some(parentId, index) ->
            let childId = NodeId.New()
            let name =
                GraphQuery.unusedOwnedName graph parentId baseName Set.empty
            let ops =
                [ Op.NewSpecialNode(childId, kind, name)
                  appendOwnedOp parentId childId index ]
            childId, ops

    let planCreateWorkspace (graph: Graph) (query: string) : NodeId * Op list =
        let childId = NodeId.New()
        let name =
            GraphQuery.unusedOwnedName
                graph
                Graph.workspacesId
                (baseNameFromQuery query "workspace")
                Set.empty
        let index = Graph.fileTreeInsertIndex graph Graph.workspacesId
        childId,
        [ Op.NewSpecialNode(childId, Workspace, name)
          appendOwnedOp Graph.workspacesId childId index ]

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
                |> Option.exists (fun c ->
                    Node.childOwnership graph insert.parentId c = Ownership.Ref
                    && c.id = fileNodeId)

            if already then
                []
            else
                [ Op.Replace(insert.parentId, insert.index, [], [ newRef ]) ]
