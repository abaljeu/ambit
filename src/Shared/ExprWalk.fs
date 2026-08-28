namespace Gambol.Shared

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
