namespace Gambol.Shared

[<RequireQualifiedAccess>]
module ExprDialog =
    let tryHits
        (query: string)
        (zoomRoot: NodeId)
        (graph: Graph)
        : NodeSearchResult list option =
        let trimmed = query.Trim()
        if not (trimmed.StartsWith("=")) then
            None
        else
            let source = trimmed.Substring(1).Trim()
            let input = ExprAnswer.Node graph.nodes.[zoomRoot]
            match ExprCompile.evalOutcome graph input source with
            | ExprCompile.ParseFailed _
            | ExprCompile.TypeFailed _ -> Some []
            | ExprCompile.Hits(ExprAnswerType.Text, _) -> Some []
            | ExprCompile.Hits(_, []) -> Some []
            | ExprCompile.Hits(ExprAnswerType.Node, answers) ->
                answers
                |> List.choose (function
                    | ExprAnswer.Node n ->
                        Some
                            { nodeId = n.id
                              text = n.text
                              name = n.name }
                    | _ -> None)
                |> Some
