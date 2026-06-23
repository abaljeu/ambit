namespace Gambol.Shared

[<RequireQualifiedAccess>]
module AmbleRun =

    let private isSpecialFocus (graph: Graph) (focusNodeId: NodeId) : bool =
        match Map.tryFind focusNodeId graph.nodes with
        | None -> false
        | Some node ->
            match node.kind with
            | Special _ -> true
            | Normal -> false

    let run (focusNodeId: NodeId) (graph: Graph) (line: string) : Result<Op list, string> =
        if isSpecialFocus graph focusNodeId then
            Ok []
        else
            AmbleParse.parse line
            |> Result.bind (AmbleEval.evalStatement focusNodeId graph)
