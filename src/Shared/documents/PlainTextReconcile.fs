namespace Gambol.Shared

/// Plain warm import via nested SpanNode + shared outline LCS.
/// Blank lines remain bound empty nodes (each blank is its own SpanNode).
[<RequireQualifiedAccess>]
module PlainTextReconcile =

    let private toNodesRead (r: PlainTextReadResult) =
        OutlineDocument.nodesRead r.documentRootId r.nodes

    let private toSpanTree text nodeIds =
        let _, flat = PlainTextDocument.flattenText text

        OutlineDocument.nestOutlineRows
            (flat |> List.map (fun (depth, body) -> depth, body, None))
            nodeIds

    let private finishNodes text documentRootId contextGraph nodes =
        let indentStyle, _ = PlainTextDocument.flattenText text

        toNodesRead (
            PlainTextDocument.finishRead
                documentRootId
                contextGraph
                nodes
                indentStyle
        )

    let private readColdImpl text graph documentRootId =
        PlainTextDocument.read text documentRootId graph
        |> Result.map toNodesRead

    let private hooks: OutlineDocument.OutlineWarmHooks = {
        OutlineDocument.OutlineWarmHooks.previousNodeIds =
            PlainTextDocument.previousOutlineIds
        whenUnchanged =
            Some(fun previousText contextGraph documentRootId ->
                PlainTextDocument.copyDocumentFromGraph
                    contextGraph
                    documentRootId
                |> finishNodes previousText documentRootId contextGraph
                |> Ok)
        fromAligned =
            fun editedText contextGraph documentRootId aligned ->
                PlainTextDocument.rebuildFromAligned
                    documentRootId
                    contextGraph
                    aligned
                |> Result.map (
                    finishNodes editedText documentRootId contextGraph
                )
    }

    let handler: DocumentHandler =
        OutlineDocument.makeOutlineHandler
            toSpanTree
            readColdImpl
            hooks
            PlainTextDocument.writeArtifact

    let reconcile
        (previousText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (editedText: string)
        : Result<PlainTextReadResult, string> =
        let indentStyle, _ =
            PlainTextDocument.flattenText (
                if editedText = previousText then
                    previousText
                else
                    editedText
            )

        handler.readWarm editedText contextGraph documentRootId previousText
        |> Result.map (fun r ->
            PlainTextDocument.finishRead
                documentRootId
                contextGraph
                r.nodes
                indentStyle)
