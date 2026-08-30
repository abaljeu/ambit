namespace Gambol.Shared

open System
open System.Text.RegularExpressions

[<RequireQualifiedAccess>]
module ExprWalk =
    let private childrenWhere
        (graph: Graph)
        (keep: ChildNode -> bool)
        (node: Node)
        : Node list =
        match node.childrenStatus with
        | Unloaded -> []
        | Loaded ->
            node.children
            |> List.choose (fun child ->
                if keep child then
                    Map.tryFind child.id graph.nodes
                else
                    None)

    /// Children of a Node as Answers; Unloaded yields no Answers (miss, never an error).
    let childAnswers (graph: Graph) (node: Node) : ExprEval.Stream =
        childrenWhere graph (fun _ -> true) node
        |> List.map ExprAnswer.Node
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
        childrenWhere graph (fun child -> child.ref = Ownership.Owner) parent

    let private answersWhere graph keep input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            childrenWhere graph keep node
            |> List.map (fun n -> ExprAnswer.Node n)
            |> ExprEval.ofList

    /// Immediate Owned Children of the input; Unloaded and Text are a miss.
    let ownedAnswers graph input =
        answersWhere graph (fun child -> child.ref = Ownership.Owner) input

    /// Immediate Ref Children of the input; Unloaded and Text are a miss.
    let refAnswers graph input =
        answersWhere graph (fun child -> child.ref = Ownership.Ref) input

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

    let rec private stepOuter inner graph (frames: Node list list) =
        match frames with
        | [] -> None
        | [] :: rest -> stepOuter inner graph rest
        | (node :: siblings) :: rest ->
            match ExprEval.pull (inner (toAnswer node)) with
            | Some _ -> Some(toAnswer node, siblings :: rest)
            | None ->
                let kids = ownedChildren graph node
                stepOuter inner graph (kids :: siblings :: rest)

    let outerAnswers graph inner input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            streamOf (stepOuter inner graph) [ ownedChildren graph node ]

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

    /// Dual filter: a Text input is tested directly, a Node input through `node.text`.
    /// Either way the input Answer is what the filter yields.
    let private dualFilter graph (matches: string -> bool) input =
        match input with
        | ExprAnswer.Text text ->
            if matches text then ExprEval.singleton input else ExprEval.empty
        | ExprAnswer.Node _ ->
            keepInput graph (fun (node: Node) -> matches node.text) input

    let containing graph (needle: string) input =
        dualFilter
            graph
            (fun text -> text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
            input

    let private tryRegex ignoreCase (pattern: string) =
        try
            let options =
                if ignoreCase then RegexOptions.IgnoreCase
                else RegexOptions.None
            Some(Regex(pattern, options))
        with
        | _ -> None

    let private matchRe graph ignoreCase pattern input =
        match tryRegex ignoreCase pattern with
        | None -> ExprEval.empty
        | Some regex -> dualFilter graph regex.IsMatch input

    let re graph pattern input = matchRe graph false pattern input

    let rei graph pattern input = matchRe graph true pattern input

    /// `name`: the Filename of a Node, Ok only. Empty and Invalid are a miss.
    let nameText graph input =
        match tryGraphNode graph input with
        | None -> ExprEval.empty
        | Some node ->
            Filename.tryValue node.name
            |> Option.map ExprAnswer.Text
            |> ExprEval.ofOption

    /// Length of the prefix or suffix to keep: never past the ends of the string.
    let private clampLength (n: int) (text: string) =
        if n < 1 then 0
        elif n > text.Length then text.Length
        else n

    let leftText (n: int) input =
        match input with
        | ExprAnswer.Text text ->
            ExprEval.singleton (ExprAnswer.Text(text.Substring(0, clampLength n text)))
        | ExprAnswer.Node _ -> ExprEval.empty

    let rightText (n: int) input =
        match input with
        | ExprAnswer.Text text ->
            let kept = clampLength n text
            ExprEval.singleton (ExprAnswer.Text(text.Substring(text.Length - kept)))
        | ExprAnswer.Node _ -> ExprEval.empty

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
