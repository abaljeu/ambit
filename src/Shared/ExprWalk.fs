namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module ExprWalk =
    /// Children of a Node as Answers; Unloaded yields no Answers (miss, never an error).
    let childAnswers (graph: Graph) (node: Node) : ExprAnswer list =
        match node.childrenStatus with
        | Unloaded -> []
        | Loaded ->
            node.children
            |> List.choose (fun child ->
                Map.tryFind child.id graph.nodes
                |> Option.map ExprAnswer.Node)

    let tryGraphNode (graph: Graph) (input: ExprAnswer) : Node option =
        match input with
        | ExprAnswer.Text _ -> None
        | ExprAnswer.Node n -> Map.tryFind n.id graph.nodes

    let globMatch (pattern: string) (text: string) : bool =
        let rec loop pi ti =
            if pi >= pattern.Length then
                ti >= text.Length
            elif ti >= text.Length then
                pattern.[pi] = '*' && loop (pi + 1) ti
            else
                let pc = Char.ToLowerInvariant pattern.[pi]
                let tc = Char.ToLowerInvariant text.[ti]
                if pc = '*' then
                    loop (pi + 1) ti || loop pi (ti + 1)
                elif pc = tc then
                    loop (pi + 1) (ti + 1)
                else
                    false

        loop 0 0

    let private nameMatches glob (node: Node) =
        match Filename.tryValue node.name with
        | Some name -> globMatch glob name
        | None -> false

    let private isStructural (node: Node) =
        match node.kind with
        | Special (File | Directory | Workspace) -> true
        | _ -> false

    let private isDirOrWorkspace (node: Node) =
        match node.kind with
        | Special (Directory | Workspace) -> true
        | _ -> false

    let private ownedChildren (graph: Graph) (parent: Node) : Node list =
        match parent.childrenStatus with
        | Unloaded -> []
        | Loaded ->
            parent.children
            |> List.choose (fun child ->
                if child.ref <> Ownership.Owner then
                    None
                else
                    Map.tryFind child.id graph.nodes)

    let private toAnswer (node: Node) = ExprAnswer.Node node

    let rec private collectStructural glob graph (parent: Node) (acc: ExprAnswer list) =
        ownedChildren graph parent
        |> List.fold
            (fun acc node ->
                let acc =
                    if isStructural node && nameMatches glob node then
                        toAnswer node :: acc
                    else
                        acc

                if isDirOrWorkspace node then
                    acc
                else
                    collectStructural glob graph node acc)
            acc

    let structuralSearch graph glob input =
        match tryGraphNode graph input with
        | None -> []
        | Some node -> collectStructural glob graph node [] |> List.rev

    let rec private collectOwned graph (parent: Node) (acc: ExprAnswer list) =
        ownedChildren graph parent
        |> List.fold
            (fun acc node -> collectOwned graph node (toAnswer node :: acc))
            acc

    let treeAnswers graph input =
        match tryGraphNode graph input with
        | None -> []
        | Some node -> collectOwned graph node [] |> List.rev

    let rec private collectDesc graph seen (acc: ExprAnswer list) (parent: Node) =
        let visit (acc, seen) (child: ChildNode) =
            if Set.contains child.id seen then
                acc, seen
            else
                match Map.tryFind child.id graph.nodes with
                | None -> acc, seen
                | Some node ->
                    collectDesc
                        graph
                        (Set.add node.id seen)
                        (toAnswer node :: acc)
                        node

        match parent.childrenStatus with
        | Unloaded -> acc, seen
        | Loaded -> List.fold visit (acc, seen) parent.children

    let descendantAnswers graph input =
        match tryGraphNode graph input with
        | None -> []
        | Some node ->
            collectDesc graph (Set.singleton node.id) [] node
            |> fst
            |> List.rev

    let private enclosingAnswer graph predicate input =
        match tryGraphNode graph input with
        | None -> []
        | Some node ->
            GraphQuery.enclosing graph predicate node.id
            |> Option.bind (fun id -> Map.tryFind id graph.nodes)
            |> Option.map ExprAnswer.Node
            |> Option.toList

    let structuralUp graph input =
        enclosingAnswer graph isStructural input

    let directoryUp graph input =
        enclosingAnswer graph isDirOrWorkspace input

    let workspaceUp graph input =
        enclosingAnswer graph (fun (node: Node) ->
            match node.kind with
            | Special Workspace -> true
            | _ -> false) input

    let private navOf graph input =
        tryGraphNode graph input
        |> Option.map (fun (node: Node) -> Node.at graph (Some node.id))

    let private answersOf graph ids =
        ids
        |> List.choose (fun id ->
            Map.tryFind id graph.nodes |> Option.map toAnswer)

    let childAt graph index input =
        match navOf graph input with
        | None -> []
        | Some nav ->
            match index with
            | None -> answersOf graph (Node.childIds nav)
            | Some n ->
                nav |> Node.childNth n |> Node.current |> Option.toList
                |> answersOf graph

    let siblingAt graph offset input =
        match navOf graph input with
        | None -> []
        | Some nav ->
            match Node.current nav with
            | Some id when id = graph.root -> []
            | _ ->
                match offset with
                | None -> answersOf graph (Node.childIds (Node.owner nav))
                | Some n ->
                    nav
                    |> Node.siblingNth n
                    |> Node.current
                    |> Option.toList
                    |> answersOf graph

    let private keepInput graph predicate input =
        match tryGraphNode graph input with
        | Some node when predicate node -> [ toAnswer node ]
        | _ -> []

    let named graph glob input =
        keepInput graph (fun (node: Node) ->
            match node.kind, Filename.tryValue node.name with
            | Normal, Some name -> globMatch glob name
            | _ -> false) input

    let ws graph input =
        keepInput graph (fun (node: Node) ->
            match node.kind with
            | Special Workspace -> true
            | _ -> false) input

    let dir graph input =
        keepInput graph (fun (node: Node) ->
            match node.kind with
            | Special Directory -> true
            | _ -> false) input

    let file graph input =
        keepInput graph (fun (node: Node) ->
            match node.kind with
            | Special File -> true
            | _ -> false) input

    let normal graph input =
        keepInput graph (fun (node: Node) ->
            match node.kind with
            | Normal -> true
            | _ -> false) input

    let classMember graph token input =
        keepInput graph (fun (node: Node) ->
            CssClass.contains token node.cssClasses) input

    let containing graph (needle: string) input =
        match tryGraphNode graph input with
        | None -> []
        | Some node ->
            if node.text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 then
                [ toAnswer node ]
            else
                []

    let private isFileDirWorkspace (node: Node) =
        match node.kind with
        | Special (File | Directory | Workspace) -> true
        | _ -> false

    let rec private visitContent graph glob seen acc (node: Node) =
        if Set.contains node.id seen then
            acc, seen
        else
            let seen = Set.add node.id seen
            match node.kind, Filename.tryValue node.name with
            | Normal, None ->
                walkContentChildren graph glob seen acc node
            | Normal, Some name when globMatch glob name ->
                toAnswer node :: acc, seen
            | Normal, Some _ -> acc, seen
            | _ when isFileDirWorkspace node -> acc, seen
            | _ -> walkContentChildren graph glob seen acc node

    and walkContentChildren graph glob seen acc (parent: Node) =
        match parent.childrenStatus with
        | Unloaded -> acc, seen
        | Loaded ->
            parent.children
            |> List.fold
                (fun (acc, seen) (child: ChildNode) ->
                    match Map.tryFind child.id graph.nodes with
                    | None -> acc, seen
                    | Some node -> visitContent graph glob seen acc node)
                (acc, seen)

    let contentSearch graph glob input =
        match tryGraphNode graph input with
        | None -> []
        | Some node ->
            walkContentChildren graph glob Set.empty [] node
            |> fst
            |> List.rev
