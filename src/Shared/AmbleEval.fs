namespace Gambol.Shared

[<RequireQualifiedAccess>]
module AmbleEval =

    let evalStatement
        (focusNodeId: NodeId)
        (graph: Graph)
        (stmt: AmbleStatement)
        : Result<Op list, string> =
        match stmt with
        | Assign(name, _) ->
            NodeRenameOps.planRenameNode graph focusNodeId name
            |> Result.map fst
        | ExprStmt _ -> Error "not yet supported"
