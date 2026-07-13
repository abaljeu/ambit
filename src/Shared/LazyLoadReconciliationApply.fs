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

    let markerRelativePath (info: LazyLoadReconciliationPath.PathInfo) =
        if info.parts.IsEmpty then
            ".amb"
        else
            String.concat "/" info.parts + "/.amb"

    /// Only File stubs need deferred parse. Directory/Workspace tree stubs stay
    /// Current; `.amb` owners are parsed immediately when text is supplied.
    let markAddedDocumentsUnparsed graph workspaceId added planned =
        let createdFileIds =
            planned
            |> List.choose (function
                | Op.NewSpecialNode(nodeId, File, _) -> Some nodeId
                | _ -> None)
        let addedFileIds =
            added
            |> List.choose (fun info ->
                match LazyLoadReconciliationPath.resolveInfo graph workspaceId info with
                | Ok(Some(nodeId, File)) -> Some nodeId
                | _ -> None)
        createdFileIds @ addedFileIds
        |> List.distinct
        |> List.fold (fun result nodeId ->
            result
            |> Result.bind (fun (current, ops) ->
                let stateOps = markUnparsed current nodeId
                applyOps current stateOps
                |> Result.map (fun next -> next, ops @ stateOps))) (Ok(graph, []))
        |> Result.map (fun (finalGraph, stateOps) ->
            finalGraph, planned @ stateOps)

    let tryArtifactText
        (artifacts: Map<string, string>)
        (info: LazyLoadReconciliationPath.PathInfo)
        =
        Map.tryFind (markerRelativePath info) artifacts

    let parseDirInfoIfPresent
        graph
        workspaceId
        (artifacts: Map<string, string>)
        (info: LazyLoadReconciliationPath.PathInfo)
        =
        if not info.isDirInfo then
            Ok(graph, [])
        else
            match tryArtifactText artifacts info with
            | None -> Ok(graph, [])
            | Some text ->
                LazyLoadReconciliationPath.resolveInfo graph workspaceId info
                |> Result.bind (function
                    | None -> Ok(graph, [])
                    | Some(nodeId, _) ->
                        DocumentParseOps.planApplyArtifact
                            graph
                            nodeId
                            (markerRelativePath info)
                            text
                        |> Result.bind (fun parseOps ->
                            applyOps graph parseOps
                            |> Result.map (fun next -> next, parseOps)))

    let parseDirInfoInfos
        graph
        workspaceId
        (artifacts: Map<string, string>)
        (infos: LazyLoadReconciliationPath.PathInfo list)
        =
        infos
        |> List.fold (fun result info ->
            result
            |> Result.bind (fun (current, ops) ->
                parseDirInfoIfPresent current workspaceId artifacts info
                |> Result.map (fun (next, parseOps) ->
                    next, ops @ parseOps))) (Ok(graph, []))
