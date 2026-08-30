namespace Gambol.Shared

/// C-style brace warm import via nested SpanNode + shared outline LCS.
[<RequireQualifiedAccess>]
module CStyleReconcile =

    let private toNodesRead (r: CStyleReadResult) =
        OutlineDocument.nodesRead r.documentRootId r.nodes

    let private toSpanTree text nodeIds =
        let _, flat = CStyleDocument.flattenText text

        OutlineDocument.nestOutlineRows
            (flat |> List.map (fun (depth, body, _) -> depth, body, None))
            nodeIds

    let private finishNodes text documentRootId contextGraph nodes =
        let indentStyle, _ = CStyleDocument.flattenText text

        toNodesRead (
            CStyleDocument.finishRead
                documentRootId
                contextGraph
                nodes
                indentStyle
        )

    let private readColdImpl text graph documentRootId =
        CStyleDocument.read text documentRootId graph
        |> Result.map toNodesRead

    let private hooks: OutlineDocumentWarm.OutlineWarmHooks = {
        OutlineDocumentWarm.OutlineWarmHooks.previousNodeIds =
            CStyleDocument.previousOutlineIds
        whenUnchanged =
            Some(fun previousText contextGraph documentRootId ->
                CStyleDocument.copyDocumentFromGraph
                    contextGraph
                    documentRootId
                |> finishNodes previousText documentRootId contextGraph
                |> Ok)
        fromAligned =
            fun editedText contextGraph documentRootId aligned ->
                CStyleDocument.rebuildFromAligned
                    editedText
                    documentRootId
                    contextGraph
                    aligned
                |> Result.map (
                    finishNodes editedText documentRootId contextGraph
                )
    }

    let handler (diffTexts: OutlineDiffTexts) : DocumentHandler =
        OutlineDocumentWarm.makeOutlineHandler
            diffTexts
            toSpanTree
            readColdImpl
            hooks
            (fun graph documentRootId previousText ->
                match previousText with
                | None ->
                    CStyleDocument.writeArtifact graph documentRootId None
                | Some prev ->
                    CStyleDocument.writeArtifactWarm
                        diffTexts
                        graph
                        documentRootId
                        prev)
