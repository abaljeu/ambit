namespace Gambol.Shared

[<RequireQualifiedAccess>]
module internal LazyLoadReconciliationApply =

    let applyOps (graph: Graph) (ops: Op list) : Result<Graph, string> =
        let initial =
            { graph = graph
              history = History.empty
              revision = Revision.Zero }
        ops
        |> List.fold (fun result op ->
            result
            |> Result.bind (fun state ->
                match Op.apply op state with
                | ApplyResult.Changed next
                | ApplyResult.Unchanged next -> Ok next
                | ApplyResult.Invalid(_, error) -> Error error)) (Ok initial)
        |> Result.map (fun state -> state.graph)

    let markUnparsed (graph: Graph) nodeId =
        match graph.nodes.[nodeId].documentState with
        | Unparsed -> []
        | Current -> [ Op.SetDocumentState(nodeId, Current, Unparsed) ]

    let markAddedDocumentsUnparsed graph workspaceId added planned =
        let createdIds =
            planned
            |> List.choose (function
                | Op.NewSpecialNode(nodeId, (Workspace | Directory | File), _) ->
                    Some nodeId
                | _ -> None)
        let addedIds =
            added
            |> List.choose (fun info ->
                match LazyLoadReconciliationPath.resolveInfo graph workspaceId info with
                | Ok(Some(nodeId, _)) -> Some nodeId
                | _ -> None)
        createdIds @ addedIds
        |> List.distinct
        |> List.fold (fun result nodeId ->
            result
            |> Result.bind (fun (current, ops) ->
                let stateOps = markUnparsed current nodeId
                applyOps current stateOps
                |> Result.map (fun next -> next, ops @ stateOps))) (Ok(graph, []))
        |> Result.map (fun (finalGraph, stateOps) ->
            finalGraph, planned @ stateOps)
