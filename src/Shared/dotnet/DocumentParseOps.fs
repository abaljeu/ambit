namespace Gambol.Shared

/// Turn a DocumentFormat artifact read into history Ops for one document root.
[<RequireQualifiedAccess>]
module DocumentParseOps =

    let private marksOwningSpecialUnpersisted =
        function
        | Op.SetText _
        | Op.SetClasses _
        | Op.SetName _
        | Op.Replace _ -> true
        | _ -> false

    /// Disk parse brings this content node in line with disk. Clear Unpersisted.
    /// Skip a no-op when the node is already Persisted and the parse did not edit it.
    let private finishDiskParse
        (graph: Graph)
        (documentRootId: NodeId)
        (ops: Op list)
        : Op list =
        match Map.tryFind documentRootId graph.nodes with
        | Some node when
            Node.carriesStateAxes node
            && (node.persistState = PersistState.Unpersisted
                || List.exists marksOwningSpecialUnpersisted ops) ->
            ops
            @ [ Op.SetPersistState(
                    documentRootId,
                    PersistState.Unpersisted,
                    PersistState.Persisted) ]
        | _ -> ops

    /// Parse one `.amb` (or plain) artifact into ops. Document stays Current.
    /// When `previousText` is present, warm-reconcile via OutlineLcs (DiffPlex).
    /// Cold (`None`) delegates to DocumentColdParse.planApplyCold.
    let planApplyArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (text: string)
        (previousText: string option)
        : Result<Op list, string> =
        let planned =
            match previousText with
            | None ->
                DocumentColdParse.planApplyCold
                    graph
                    documentRootId
                    relativePath
                    text
            | Some _ ->
                DocumentWarm.readArtifact
                    OutlineLcs.diffTexts
                    relativePath
                    text
                    documentRootId
                    graph
                    previousText
                |> Result.map (
                    DocumentColdParse.planOpsFromGraphs graph documentRootId
                )
        planned |> Result.map (finishDiskParse graph documentRootId)
