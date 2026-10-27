namespace Gambol.Shared

[<RequireQualifiedAccess>]
module internal LazyLoadReconciliationApply =

    /// Same apply as mailbox postGraphOnly: guardUnparsed false, command Parse.
    let applyOps (graph: Graph) (ops: Op list) : Result<Graph, string> =
        if ops.IsEmpty then
            Ok graph
        else
            let state =
                { graph = graph
                  eventId = EventId.zero }
            match
                ChangeAmendment.applyForGraphOnly
                    EventId.zero
                    "Parse"
                    ops
                    state
            with
            | ApplyResult.Changed next, _, _
            | ApplyResult.Unchanged next, _, _ -> Ok next.graph
            | ApplyResult.Invalid(_, error), _, _ -> Error error

    /// Current or PullingFile. Already Unparsed is not a new mark.
    let decidesUnparsedMark (graph: Graph) nodeId =
        match Node.documentStateEnum graph.nodes.[nodeId] with
        | Unparsed -> false
        | Current
        | PullingFile -> true

    /// No SetDocumentState. Callers record the id.
    let markUnparsed (graph: Graph) nodeId =
        decidesUnparsedMark graph nodeId |> ignore
        []

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
                    | Some node when Node.documentStateEnum node = PullingFile ->
                        Some nodeId
                    | _ -> None
                | _ -> None)
        [ File; Directory ]
        |> List.collect (fun kind ->
            createdStubIds kind @ resolvedStubIds kind)
        |> List.distinct

    let directoryFileRelativePath (info: LazyLoadReconciliationPath.PathInfo) =
        if info.parts.IsEmpty then
            ".amb"
        else
            String.concat "/" info.parts + "/.amb"

    /// File and Directory stubs stay Unparsed.
    /// `planned` is only for stub discovery (NewSpecialNode ids); not re-emitted.
    let markAddedDocumentsUnparsed graph workspaceId added planned =
        let stubIds = addedStubIds graph workspaceId added planned
        foldDocumentStateOps markUnparsed graph stubIds

    let tryArtifactText
        (artifacts: Map<string, string>)
        (info: LazyLoadReconciliationPath.PathInfo)
        =
        Map.tryFind (directoryFileRelativePath info) artifacts

    /// Prior artifact bytes for warm reconcile: export current graph when it
    /// already projects outline text. Empty/missing → cold None (first load).
    let previousArtifactText (graph: Graph) (nodeId: NodeId) (relativePath: string) =
        match DocumentFormat.writeArtifact graph nodeId relativePath None with
        | Ok prev when prev.Length > 0 -> Some prev
        | _ -> None

    /// Skip Added-path Directory File parse when stub already exists Current
    /// and ensureChild emitted no creates. New / Unparsed / PullingFile parse.
    let skipParseAddedDirInfo
        (graph: Graph)
        workspaceId
        (createOps: Op list)
        (info: LazyLoadReconciliationPath.PathInfo)
        =
        if not info.isDirInfo || not createOps.IsEmpty then
            false
        else
            match
                LazyLoadReconciliationPath.resolveInfo graph workspaceId info
            with
            | Ok(Some(nodeId, _)) ->
                Node.documentStateEnum graph.nodes.[nodeId] = Current
            | _ -> false

    let private noParsedDir graph =
        Ok(graph, [], None)

    /// `dirId` is the Directory or Workspace this parse applied.
    let private planParsedDir graph nodeId relativePath text docState =
        let previousText =
            match docState with
            | Unparsed
            | PullingFile -> None
            | Current ->
                previousArtifactText graph nodeId relativePath
        DocumentParseOps.planApplyArtifact
            graph
            nodeId
            relativePath
            text
            previousText
        |> Result.bind (fun parseOps ->
            applyOps graph parseOps
            |> Result.map (fun next -> next, parseOps, Some nodeId))

    let parseDirInfoIfPresent
        graph
        workspaceId
        (artifacts: Map<string, string>)
        (info: LazyLoadReconciliationPath.PathInfo)
        =
        if not info.isDirInfo then
            noParsedDir graph
        else
            match tryArtifactText artifacts info with
            | None -> noParsedDir graph
            | Some text ->
                LazyLoadReconciliationPath.resolveInfo graph workspaceId info
                |> Result.bind (function
                    | None -> noParsedDir graph
                    | Some(nodeId, _) ->
                        let relativePath = directoryFileRelativePath info
                        let docState =
                            Node.documentStateEnum graph.nodes.[nodeId]
                        planParsedDir
                            graph
                            nodeId
                            relativePath
                            text
                            docState)

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
                if skipParseAddedDirInfo current workspaceId [] info then
                    Ok(current, ops)
                else
                    parseDirInfoIfPresent current workspaceId artifacts info
                    |> Result.map (fun (next, parseOps, _) ->
                        next, ops @ parseOps))) (Ok(graph, []))
