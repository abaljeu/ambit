namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module AmbleRun =

    let private isSpecialFocus (graph: Graph) (focusNodeId: NodeId) : bool =
        match Map.tryFind focusNodeId graph.nodes with
        | None -> false
        | Some node ->
            match node.kind with
            | Special _ -> true
            | Normal -> false

    let private replaceAllChildrenOp
        (graph: Graph)
        (parentId: NodeId)
        (newChildren: ChildNode list)
        : Op =
        let existing = graph.nodes.[parentId].children
        Op.Replace(parentId, 0, existing, newChildren)

    let private redletterClasses = CssClass.ofList [ "redletter" ]

    let private newErrorNodeOps (id: NodeId) (text: string) : Op list =
        [ Op.NewNode(id, text)
          Op.SetClasses(id, CssClass.empty, redletterClasses) ]

    let private planErrorTextNodes (graph: Graph) (focusNodeId: NodeId) (text: string) : Op list =
        let lines =
            text.Split([| "\r\n"; "\r"; "\n" |], StringSplitOptions.None)
            |> Array.toList

        if lines |> List.forall (fun line -> line.Trim().Length = 0) then
            []
        else
            let pairs = lines |> List.map (fun line -> NodeId.New(), line)
            let newOps = pairs |> List.collect (fun (id, line) -> newErrorNodeOps id line)
            let childNodes =
                pairs |> List.map (fun (id, _) -> { ref = Ownership.Owner; id = id })

            newOps @ [ replaceAllChildrenOp graph focusNodeId childNodes ]

    let private planReplaceFromSpecs
        (graph: Graph)
        (focusNodeId: NodeId)
        (specs: AmbleEval.NodeSpec list)
        : Op list =
        if specs.IsEmpty then
            []
        else
            let newNodeOps, childNodes =
                specs
                |> List.fold
                    (fun (ops, children) spec ->
                        match spec with
                        | AmbleEval.NodeSpec.RefSpec node ->
                            if Map.containsKey node.id graph.nodes then
                                ops,
                                children @ [ { ref = Ownership.Ref; id = node.id } ]
                            else
                                ops @ [ Op.NewNode(node.id, node.text) ],
                                children @ [ { ref = Ownership.Owner; id = node.id } ]
                        | AmbleEval.NodeSpec.NewSpec text ->
                            let id = NodeId.New()
                            ops @ [ Op.NewNode(id, text) ],
                            children @ [ { ref = Ownership.Owner; id = id } ])
                    ([], [])

            newNodeOps @ [ replaceAllChildrenOp graph focusNodeId childNodes ]

    let private planRenameOps
        (graph: Graph)
        (focusNodeId: NodeId)
        (nameOpt: string option)
        : Result<Op list, string> =
        match nameOpt with
        | None -> Ok []
        | Some name ->
            NodeRenameOps.planRenameNode graph focusNodeId name
            |> Result.map fst

    let private planEvalResult
        (graph: Graph)
        (focusNodeId: NodeId)
        (nameOpt: string option, specs: AmbleEval.NodeSpec list)
        : Result<Op list, string> =
        planRenameOps graph focusNodeId nameOpt
        |> Result.map (fun renameOps -> renameOps @ planReplaceFromSpecs graph focusNodeId specs)

    let run (focusNodeId: NodeId) (graph: Graph) (line: string) : Result<Op list, string> =
        if isSpecialFocus graph focusNodeId then
            Ok []
        else
            match AmbleParse.parse line with
            | Error _ -> Ok (planErrorTextNodes graph focusNodeId line)
            | Ok stmt ->
                match AmbleEval.evalStatement focusNodeId graph stmt with
                | Error _ -> Ok (planErrorTextNodes graph focusNodeId line)
                | Ok result -> planEvalResult graph focusNodeId result
