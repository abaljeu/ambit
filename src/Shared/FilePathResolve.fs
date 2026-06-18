namespace Gambol.Shared

open System

type ConcreteFileTarget =
    { parentId: NodeId
      fileName: string
      missingSegments: (SpecialKind * string) list }

[<RequireQualifiedAccess>]
module FilePathResolve =

    let private globChars = [| '*'; '?' |]

    let private hasGlob (s: string) : bool =
        s.IndexOfAny globChars >= 0

    let private isConcreteFileName (name: string) : bool =
        not (hasGlob name)
        && match Filename.create name with
           | Filename.Ok _ -> true
           | _ -> false

    let private isConcreteDirStep (step: ExprStep) : bool =
        match step with
        | DirStep name -> isConcreteFileName name
        | _ -> false

    let private isConcretePathSteps (steps: ExprStep list) : bool =
        match steps with
        | [] -> false
        | steps ->
            let dirSteps, fileSteps = List.splitAt (steps.Length - 1) steps

            List.forall isConcreteDirStep dirSteps
            && match fileSteps with
               | [ FileStep name ] -> isConcreteFileName name
               | _ -> false

    let private relativePathToExpr (query: string) : PathExpr option =
        if not (query.Contains '/') then
            None
        else
            let segments =
                query.Split('/', StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun s -> s.Trim())
                |> Array.filter (fun s -> s.Length > 0)

            if segments.Length < 2 || not (Array.forall isConcreteFileName segments) then
                None
            else
                let dirSteps =
                    segments.[0 .. segments.Length - 2]
                    |> Array.map (fun s -> DirStep s)
                    |> Array.toList

                let fileStep = FileStep segments.[segments.Length - 1]
                Some(Path(WorkspaceRoot, dirSteps @ [ fileStep ]))

    let private mapContextToWorkspace (expr: PathExpr) : PathExpr =
        match expr with
        | Path(Context, steps) -> Path(WorkspaceRoot, steps)
        | AnchorOnly Context -> AnchorOnly WorkspaceRoot
        | expr -> expr

    let queryToExpr (query: string) : PathExpr option =
        let mapBareFile =
            function
            | Path(Context, [ FileStep name ]) -> Path(WorkspaceRoot, [ FileStep name ])
            | expr -> mapContextToWorkspace expr

        match RefExpr.parse query with
        | Ok expr -> Some(mapBareFile expr)
        | Error _ ->
            if isConcreteFileName query then
                Some(Path(WorkspaceRoot, [ FileStep query ]))
            else
                relativePathToExpr query

    let private fileNodes (graph: Graph) (results: NodeSearchResult list) : NodeSearchResult list =
        results
        |> List.filter (fun r ->
            match graph.nodes.[r.nodeId].kind with
            | Special File -> true
            | _ -> false)

    let private parentPathExpr (anchor: ExprAnchor) (dirSteps: ExprStep list) : PathExpr =
        if List.isEmpty dirSteps then
            AnchorOnly anchor
        else
            Path(anchor, dirSteps)

    let private isWorkspaceOrDirectory (graph: Graph) (nodeId: NodeId) : bool =
        match graph.nodes.[nodeId].kind with
        | Special Workspace
        | Special Directory -> true
        | _ -> false

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

    let private resolveMissingChain
        (graph: Graph)
        (startParentId: NodeId)
        (dirSteps: ExprStep list)
        : NodeId * (SpecialKind * string) list =
        let rec walk parentId steps missing =
            match steps with
            | [] -> parentId, List.rev missing
            | DirStep name :: rest ->
                match findOwnerChild graph parentId Directory name with
                | Some dirId -> walk dirId rest missing
                | None -> walk parentId rest ((Directory, name) :: missing)
            | _ -> parentId, List.rev missing

        walk startParentId dirSteps []

    let private resolveNamedWorkspaceChain
        (graph: Graph)
        (label: string)
        (dirSteps: ExprStep list)
        : NodeId option * (SpecialKind * string) list =
        match findOwnerChild graph Graph.workspacesId Workspace label with
        | Some wsId ->
            let parentId, missing = resolveMissingChain graph wsId dirSteps
            Some parentId, missing
        | None ->
            let dirMissing =
                dirSteps
                |> List.choose (function
                    | DirStep n -> Some(Directory, n)
                    | _ -> None)
            (Some Graph.workspacesId, [ Workspace, label ] @ dirMissing)

    let private resolveWorkspaceRootChain
        (graph: Graph)
        (workspaceRoot: NodeId)
        (dirSteps: ExprStep list)
        : NodeId * (SpecialKind * string) list =
        if List.isEmpty dirSteps then
            workspaceRoot, []
        else
            resolveMissingChain graph workspaceRoot dirSteps

    let tryResolveConcreteTarget
        (contextNode: NodeId)
        (graph: Graph)
        (query: string)
        : ConcreteFileTarget option =
        match queryToExpr query with
        | None -> None
        | Some expr ->
            let pathParts =
                match expr with
                | AnchorOnly _ -> None
                | Path(b, s) when not (List.isEmpty s) -> Some(b, s)
                | _ -> None

            match pathParts with
            | None -> None
            | Some(pathBase, steps) ->
                if not (isConcretePathSteps steps) then
                    None
                else
                    let dirSteps, fileSteps = List.splitAt (steps.Length - 1) steps

                    match fileSteps with
                    | [ FileStep fileName ] when isConcreteFileName fileName ->
                        let ctx = RefExpr.refContext contextNode graph
                        let parentExpr = parentPathExpr pathBase dirSteps

                        if not (List.isEmpty (fileNodes graph (RefExpr.match_ ctx graph expr))) then
                            None
                        else
                            match RefExpr.match_ ctx graph parentExpr with
                            | hits when hits.Length > 1 -> None
                            | [ hit ] when isWorkspaceOrDirectory graph hit.nodeId ->
                                Some
                                    { parentId = hit.nodeId
                                      fileName = fileName
                                      missingSegments = [] }
                            | [] ->
                                match pathBase with
                                | NamedWorkspace label ->
                                    let parentIdOpt, missing =
                                        resolveNamedWorkspaceChain graph label dirSteps

                                    parentIdOpt
                                    |> Option.map (fun parentId ->
                                        { parentId = parentId
                                          fileName = fileName
                                          missingSegments = missing })
                                | WorkspaceRoot ->
                                    match ctx.workspaceRoot with
                                    | None -> None
                                    | Some wsId ->
                                        let parentId, missing =
                                            resolveWorkspaceRootChain graph wsId dirSteps

                                        Some
                                            { parentId = parentId
                                              fileName = fileName
                                              missingSegments = missing }
                                | Structural
                                | CurrentDir
                                | Context
                                | GlobalRoot
                                | Tagged -> None
                            | hit :: _ -> None
                    | _ -> None

    let isNewEnabled (contextNode: NodeId) (graph: Graph) (query: string) : bool =
        tryResolveConcreteTarget contextNode graph query |> Option.isSome
