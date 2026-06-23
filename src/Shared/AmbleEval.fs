namespace Gambol.Shared

[<RequireQualifiedAccess>]
module AmbleEval =
    type NodeSpec =
        | RefSpec of Node
        | NewSpec of string

    let evalRefExpr (focusNodeId: NodeId) (graph: Graph) (expr: PathExpr) : Node list =
        let ctx = RefExpr.refContext focusNodeId graph
        RefExpr.match_ ctx graph expr
        |> List.choose (fun r -> Map.tryFind r.nodeId graph.nodes)

    let private nodeSpecsOf (nodes: Node list) : NodeSpec list =
        nodes |> List.map RefSpec

    let private evalText (focusNodeId: NodeId) (graph: Graph) (args: AmbleExpr list)
        : Result<NodeSpec list, string> =
        match args with
        | [ AmbleExpr.Ref pathExpr ] ->
            evalRefExpr focusNodeId graph pathExpr
            |> List.map (fun node -> NewSpec node.text)
            |> Ok
        | _ -> Error "text expects one ref argument"

    let evalExpr
        (focusNodeId: NodeId)
        (graph: Graph)
        (expr: AmbleExpr)
        : Result<NodeSpec list, string> =
        match expr with
        | AmbleExpr.Ref pathExpr -> Ok (evalRefExpr focusNodeId graph pathExpr |> nodeSpecsOf)
        | FunCall("text", args) -> evalText focusNodeId graph args
        | _ -> Error "Expression type not implemented"

    let evalStatement
        (focusNodeId: NodeId)
        (graph: Graph)
        (stmt: AmbleStatement)
        : Result<string option * NodeSpec list, string> =
        match stmt with
        | Assign(name, expr) ->
            evalExpr focusNodeId graph expr
            |> Result.map (fun nodes -> Some name, nodes)
        | ExprStmt expr ->
            evalExpr focusNodeId graph expr
            |> Result.map (fun nodes -> None, nodes)
