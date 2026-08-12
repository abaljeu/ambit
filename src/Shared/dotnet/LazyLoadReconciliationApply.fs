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
        | NoServerFile ->
            [ Op.SetDocumentState(nodeId, NoServerFile, Unparsed) ]

    let private markParentCurrent (graph: Graph) nodeId =
        match Map.tryFind nodeId graph.nodes with
        | Some { kind = Special (Directory | Workspace)
                 documentState = Unparsed } ->
            [ Op.SetDocumentState(nodeId, Unparsed, Current) ]
        | _ -> []

    let private foldDocumentStateOps markFn graph nodeIds =
        nodeIds
        |> List.fold (fun result nodeId ->
            result
            |> Result.bind (fun (current, ops) ->
                let stateOps = markFn current nodeId
                applyOps current stateOps
                |> Result.map (fun next -> next, ops @ stateOps)))
            (Ok(graph, []))

    let private addedStubIds graph workspaceId added planned =
        let createdStubIds kind =
            planned
            |> List.choose (function
                | Op.NewSpecialNode(nodeId, k, _) when k = kind -> Some nodeId
                | _ -> None)
        let resolvedStubIds kind =
            added
            |> List.choose (fun info ->
                match LazyLoadReconciliationPath.resolveInfo graph workspaceId info with
                | Ok(Some(nodeId, k)) when k = kind ->
                    match Map.tryFind nodeId graph.nodes with
                    | Some node when node.documentState = NoServerFile -> Some nodeId
                    | _ -> None
                | _ -> None)
        [ File; Directory ]
        |> List.collect (fun kind ->
            createdStubIds kind @ resolvedStubIds kind)
        |> List.distinct

    let markerRelativePath (info: LazyLoadReconciliationPath.PathInfo) =
        if info.parts.IsEmpty then
            ".amb"
        else
            String.concat "/" info.parts + "/.amb"

    /// File/Directory stubs → Unparsed; parents that gained those members → Current.
    /// `planned` is only for stub discovery (NewSpecialNode ids); not re-emitted.
    let markAddedDocumentsUnparsed graph workspaceId added planned =
        let stubIds = addedStubIds graph workspaceId added planned
        foldDocumentStateOps markUnparsed graph stubIds
        |> Result.bind (fun (withStubs, stubOps) ->
            let parentIds =
                stubIds
                |> List.choose (fun id ->
                    Map.tryFind id withStubs.ownerParentByChild)
                |> List.distinct
            foldDocumentStateOps markParentCurrent withStubs parentIds
            |> Result.map (fun (finalGraph, parentOps) ->
                finalGraph, stubOps @ parentOps))

    let tryArtifactText
        (artifacts: Map<string, string>)
        (info: LazyLoadReconciliationPath.PathInfo)
        =
        Map.tryFind (markerRelativePath info) artifacts

    /// Prior artifact bytes for warm reconcile: export current graph when it
    /// already projects outline text. Empty/missing → cold None (first load).
    let previousArtifactText (graph: Graph) (nodeId: NodeId) (relativePath: string) =
        match DocumentFormat.writeArtifact graph nodeId relativePath None with
        | Ok prev when prev.Length > 0 -> Some prev
        | _ -> None

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
                        let relativePath = markerRelativePath info
                        let docState = graph.nodes.[nodeId].documentState
                        let previousText =
                            match docState with
                            | Unparsed
                            | NoServerFile -> None
                            | Current ->
                                previousArtifactText graph nodeId relativePath
                        let markCurrent =
                            match docState with
                            | Unparsed ->
                                [ Op.SetDocumentState(nodeId, Unparsed, Current) ]
                            | NoServerFile ->
                                [ Op.SetDocumentState(
                                    nodeId,
                                    NoServerFile,
                                    Current) ]
                            | Current -> []
                        DocumentParseOps.planApplyArtifact
                            graph
                            nodeId
                            relativePath
                            text
                            previousText
                        |> Result.bind (fun parseOps ->
                            let ops = markCurrent @ parseOps
                            applyOps graph ops
                            |> Result.map (fun next -> next, ops)))

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
