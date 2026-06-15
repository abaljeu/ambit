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
        | NameStep name -> isConcreteFileName name
        | _ -> false

    let queryToExpr (query: string) : PathExpr option =
        match RefExpr.parse query with
        | Ok expr -> Some expr
        | Error _ ->
            if isConcreteFileName query then
                Some(Path(WorkspaceRoot, [ NameStep query ]))
            else
                None

    let private fileNodes (graph: Graph) (results: NodeSearchResult list) : NodeSearchResult list =
        results
        |> List.filter (fun r ->
            match graph.nodes.[r.nodeId].kind with
            | Special File -> true
            | _ -> false)

    let private parentPathExpr (pathBase: ExprBase) (dirSteps: ExprStep list) : PathExpr =
        if List.isEmpty dirSteps then
            BaseOnly pathBase
        else
            Path(pathBase, dirSteps)

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
            | NameStep name :: rest ->
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
                    | NameStep n -> Some(Directory, n)
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
                | BaseOnly _ -> None
                | Path(b, s) when not (List.isEmpty s) -> Some(b, s)
                | _ -> None

            match pathParts with
            | None -> None
            | Some(pathBase, steps) ->
                if not (List.forall isConcreteDirStep steps) then
                    None
                else
                    let dirSteps, fileSteps = List.splitAt (steps.Length - 1) steps

                    match fileSteps with
                    | [ NameStep fileName ] when isConcreteFileName fileName ->
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
                                | FileRoot
                                | FileDir -> None
                            | hit :: _ -> None
                    | _ -> None

    let isNewEnabled (contextNode: NodeId) (graph: Graph) (query: string) : bool =
        tryResolveConcreteTarget contextNode graph query |> Option.isSome
