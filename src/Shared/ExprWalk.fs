namespace Gambol.Shared

open System
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module ExprWalk =
    /// Children of a Node as Answers; Unloaded yields no Answers (miss, never an error).
    let childAnswers (graph: Graph) (node: Node) : ExprEval.Stream =
        match node.childrenStatus with
        | Unloaded -> ExprEval.empty
        | Loaded ->
            node.children
            |> List.choose (fun child ->
                Map.tryFind child.id graph.nodes
                |> Option.map ExprAnswer.Node)
            |> ExprEval.ofList

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

    let private loadedChildren (parent: Node) =
        match parent.childrenStatus with
        | Unloaded -> []
        | Loaded -> parent.children

    let private toAnswer (node: Node) = ExprAnswer.Node node

    let rec private streamOf step frames : ExprEval.Stream =
        ExprEval.delay (fun () ->
            match step frames with
            | None -> None
            | Some(answer, next) -> Some(answer, streamOf step next))

    let rec private streamSeen step seen frames : ExprEval.Stream =
        ExprEval.delay (fun () ->
            match step seen frames with
            | None -> None
            | Some(answer, seen, next) ->
                Some(answer, streamSeen step seen next))

    let rec private stepStructural glob graph (frames: Node list list) =
        match frames with
        | [] -> None
        | [] :: rest -> stepStructural glob graph rest
        | (node :: siblings) :: rest ->
            let kids =
                if isDirOrWorkspace node then
                    []
                else
                    ownedChildren graph node
            let frames = kids :: siblings :: rest
            if isStructural node && nameMatches glob node then
                Some(toAnswer node, frames)
            else
                stepStructural glob graph frames

    let structuralSearch graph glob input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            streamOf (stepStructural glob graph) [ ownedChildren graph node ]

    let rec private stepOwned graph (frames: Node list list) =
        match frames with
        | [] -> None
        | [] :: rest -> stepOwned graph rest
        | (node :: siblings) :: rest ->
            let kids = ownedChildren graph node
            Some(toAnswer node, kids :: siblings :: rest)

    let treeAnswers graph input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node -> streamOf (stepOwned graph) [ ownedChildren graph node ]

    let rec private stepDesc graph seen (frames: ChildNode list list) =
        match frames with
        | [] -> None
        | [] :: rest -> stepDesc graph seen rest
        | (child :: siblings) :: rest ->
            let frames = siblings :: rest
            if Set.contains child.id seen then
                stepDesc graph seen frames
            else
                match Map.tryFind child.id graph.nodes with
                | None -> stepDesc graph seen frames
                | Some node ->
                    let seen = Set.add node.id seen
                    Some(toAnswer node, seen, loadedChildren node :: frames)

    let descendantAnswers graph input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            streamSeen (stepDesc graph) (Set.singleton node.id) [ loadedChildren node ]

    let private enclosingAnswer graph predicate input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            GraphQuery.enclosing graph predicate node.id
            |> Option.bind (fun id -> Map.tryFind id graph.nodes)
            |> Option.map ExprAnswer.Node
            |> ExprEval.ofOption

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
        |> ExprEval.ofList

    let childAt graph index input =
        match navOf graph input with
        | None -> ExprEval.empty
        | Some nav ->
            match index with
            | None -> answersOf graph (Node.childIds nav)
            | Some n ->
                nav |> Node.childNth n |> Node.current |> Option.toList
                |> answersOf graph

    let siblingAt graph offset input =
        match navOf graph input with
        | None -> ExprEval.empty
        | Some nav ->
            match Node.current nav with
            | Some id when id = graph.root -> ExprEval.empty
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
        | Some node when predicate node -> ExprEval.singleton (toAnswer node)
        | _ -> ExprEval.empty

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

    let section graph input =
        keepInput graph (fun (node: Node) ->
            match node.kind, Filename.tryValue node.name with
            | Normal, Some _ -> true
            | _ -> false) input

    let classMember graph token input =
        keepInput graph (fun (node: Node) ->
            CssClass.contains token node.cssClasses) input

    let containing graph (needle: string) input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            if node.text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0 then
                ExprEval.singleton (toAnswer node)
            else
                ExprEval.empty

    let private tryRegex ignoreCase (pattern: string) =
        try
            let options =
                if ignoreCase then RegexOptions.IgnoreCase
                else RegexOptions.None
            Some(Regex(pattern, options))
        with
        | _ -> None

    let private headerRe graph ignoreCase pattern input =
        match tryRegex ignoreCase pattern with
        | None -> ExprEval.empty
        | Some regex ->
            keepInput graph (fun (node: Node) -> regex.IsMatch node.text) input

    let re graph pattern input = headerRe graph false pattern input

    let rei graph pattern input = headerRe graph true pattern input

    let private isFileDirWorkspace (node: Node) =
        match node.kind with
        | Special (File | Directory | Workspace) -> true
        | _ -> false

    let rec private stepContent glob graph seen (frames: ChildNode list list) =
        match frames with
        | [] -> None
        | [] :: rest -> stepContent glob graph seen rest
        | (child :: siblings) :: rest ->
            let frames = siblings :: rest
            if Set.contains child.id seen then
                stepContent glob graph seen frames
            else
                match Map.tryFind child.id graph.nodes with
                | None -> stepContent glob graph seen frames
                | Some node ->
                    let seen = Set.add node.id seen
                    match node.kind, Filename.tryValue node.name with
                    | Normal, None ->
                        stepContent glob graph seen (loadedChildren node :: frames)
                    | Normal, Some name when globMatch glob name ->
                        Some(toAnswer node, seen, frames)
                    | Normal, Some _ ->
                        stepContent glob graph seen frames
                    | _ when isFileDirWorkspace node ->
                        stepContent glob graph seen frames
                    | _ ->
                        stepContent glob graph seen (loadedChildren node :: frames)

    let contentSearch graph glob input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            streamSeen (stepContent glob graph) Set.empty [ loadedChildren node ]
